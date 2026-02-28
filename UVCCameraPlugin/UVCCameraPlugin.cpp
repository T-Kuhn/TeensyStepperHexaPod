#include "UVCCameraPlugin.h"

#include <windows.h>
#include <dshow.h>
#include <strmif.h>
#include <uuids.h>
#include <vector>
#include <cstring>
#include <algorithm>
#include <cwctype>
#include <string>

#pragma comment(lib, "strmiids.lib")
#pragma comment(lib, "ole32.lib")
#pragma comment(lib, "oleaut32.lib")
#pragma comment(lib, "quartz.lib")

// ---------------------------------------------------------------------------
// ISampleGrabberCB / ISampleGrabber -- forward-declared inline (qedit.h is
// not available in the standard Windows SDK).
// ---------------------------------------------------------------------------

struct ISampleGrabberCB : public IUnknown
{
    virtual STDMETHODIMP SampleCB(double SampleTime, IMediaSample* pSample) = 0;
    virtual STDMETHODIMP BufferCB(double SampleTime, BYTE* pBuffer, long BufferLen) = 0;
};

struct ISampleGrabber : public IUnknown
{
    virtual HRESULT STDMETHODCALLTYPE SetOneShot(BOOL OneShot) = 0;
    virtual HRESULT STDMETHODCALLTYPE SetMediaType(const AM_MEDIA_TYPE* pmt) = 0;
    virtual HRESULT STDMETHODCALLTYPE GetConnectedMediaType(AM_MEDIA_TYPE* pmt) = 0;
    virtual HRESULT STDMETHODCALLTYPE SetBufferSamples(BOOL BufferThem) = 0;
    virtual HRESULT STDMETHODCALLTYPE GetCurrentBuffer(long* pBufferSize, long* pBuffer) = 0;
    virtual HRESULT STDMETHODCALLTYPE GetCurrentSample(IMediaSample** ppSample) = 0;
    virtual HRESULT STDMETHODCALLTYPE SetCallback(ISampleGrabberCB* pCallback, long WhichMethodToCallback) = 0;
};

static const GUID CLSID_SampleGrabber =
{ 0xC1F400A0, 0x3F08, 0x11D3, { 0x9F, 0x0B, 0x00, 0x60, 0x08, 0x03, 0x9E, 0x37 } };

static const GUID IID_ISampleGrabberCB =
{ 0x0579154A, 0x2B53, 0x4A10, { 0xB1, 0x11, 0x5F, 0x6C, 0x5E, 0x5E, 0x5E, 0x5E } };

static const GUID IID_ISampleGrabber =
{ 0x6B652FFF, 0x11FE, 0x4FCE, { 0x92, 0xAD, 0x02, 0x66, 0xB5, 0xD7, 0xC7, 0x8F } };

// CLSID_NullRenderer  {C1F400A4-3F08-11D3-9F0B-006008039E37}
static const GUID CLSID_NullRenderer =
{ 0xC1F400A4, 0x3F08, 0x11D3, { 0x9F, 0x0B, 0x00, 0x60, 0x08, 0x03, 0x9E, 0x37 } };

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

static void SafeDeleteMediaType(AM_MEDIA_TYPE* pmt)
{
    if (!pmt) return;
    if (pmt->cbFormat != 0)
    {
        CoTaskMemFree((PVOID)pmt->pbFormat);
        pmt->cbFormat = 0;
        pmt->pbFormat = nullptr;
    }
    if (pmt->pUnk)
    {
        pmt->pUnk->Release();
        pmt->pUnk = nullptr;
    }
    CoTaskMemFree(pmt);
}

template<typename T>
static void SafeRelease(T*& p)
{
    if (p) { p->Release(); p = nullptr; }
}

// ---------------------------------------------------------------------------
// CameraState
// ---------------------------------------------------------------------------

struct CameraState;

// Forward-declared so FrameCallback can reference it.
static void CopyFlippedBGR(BYTE* dst, const BYTE* src, int width, int height);

class FrameCallback : public ISampleGrabberCB
{
public:
    CameraState* pState = nullptr;

    // IUnknown -- minimal implementation (lifetime managed by CameraState)
    STDMETHODIMP QueryInterface(REFIID riid, void** ppv) override
    {
        if (riid == IID_IUnknown || riid == IID_ISampleGrabberCB)
        {
            *ppv = static_cast<ISampleGrabberCB*>(this);
            AddRef();
            return S_OK;
        }
        *ppv = nullptr;
        return E_NOINTERFACE;
    }
    STDMETHODIMP_(ULONG) AddRef() override  { return InterlockedIncrement(&m_ref); }
    STDMETHODIMP_(ULONG) Release() override { return InterlockedDecrement(&m_ref); }

    STDMETHODIMP SampleCB(double, IMediaSample*) override { return E_NOTIMPL; }
    STDMETHODIMP BufferCB(double, BYTE* pBuffer, long BufferLen) override;

private:
    volatile LONG m_ref = 1;
};

struct CameraState
{
    IGraphBuilder*         pGraph             = nullptr;
    ICaptureGraphBuilder2* pCaptureBuilder    = nullptr;
    IBaseFilter*           pCaptureFilter     = nullptr;
    IBaseFilter*           pSampleGrabFilter  = nullptr;
    IBaseFilter*           pNullRenderer      = nullptr;
    ISampleGrabber*        pSampleGrabber     = nullptr;
    FrameCallback*         pCallback          = nullptr;
    IMediaControl*         pMediaControl      = nullptr;
    IAMCameraControl*      pCameraControl     = nullptr;
    IAMVideoProcAmp*       pVideoProcAmp      = nullptr;

    std::vector<BYTE>      frameBuffer;
    int                    width  = 0;
    int                    height = 0;
    bool                   flipRows = true; // DirectShow RGB24 is bottom-up by default

    CRITICAL_SECTION       cs;
    HANDLE                 frameEvent = nullptr; // auto-reset event
    bool                   csInitialized = false;
    bool                   running = false;
};

// FrameCallback::BufferCB -- copies the latest frame into the shared buffer,
// flipping rows to convert DirectShow's bottom-up BGR to top-down BGR.
STDMETHODIMP FrameCallback::BufferCB(double, BYTE* pBuffer, long BufferLen)
{
    if (!pState || !pBuffer || BufferLen <= 0)
        return S_OK;

    EnterCriticalSection(&pState->cs);

    if ((long)pState->frameBuffer.size() == BufferLen)
    {
        if (pState->flipRows)
            CopyFlippedBGR(pState->frameBuffer.data(), pBuffer, pState->width, pState->height);
        else
            memcpy(pState->frameBuffer.data(), pBuffer, BufferLen);
    }

    LeaveCriticalSection(&pState->cs);
    SetEvent(pState->frameEvent);
    return S_OK;
}

// Copy src (bottom-up) into dst (top-down), row by row.
static void CopyFlippedBGR(BYTE* dst, const BYTE* src, int width, int height)
{
    const int stride = width * 3;
    const BYTE* srcRow = src + (long long)(height - 1) * stride;
    BYTE* dstRow = dst;
    for (int y = 0; y < height; ++y, srcRow -= stride, dstRow += stride)
        memcpy(dstRow, srcRow, stride);
}

// ---------------------------------------------------------------------------
// Internal helper: SetCameraResolutionAndFPS
// Tries an exact match in IAMStreamConfig capabilities; falls back to a custom
// format derived from the first capability entry.
// ---------------------------------------------------------------------------
static HRESULT SetCameraResolutionAndFPS(ICaptureGraphBuilder2* pBuilder,
                                          IBaseFilter* pCaptureFilter,
                                          int width, int height, int fps)
{
    IAMStreamConfig* pStreamConfig = nullptr;
    HRESULT hr = pBuilder->FindInterface(
        &PIN_CATEGORY_CAPTURE, &MEDIATYPE_Video,
        pCaptureFilter, IID_IAMStreamConfig, (void**)&pStreamConfig);
    if (FAILED(hr)) return hr;

    int iCount = 0, iSize = 0;
    hr = pStreamConfig->GetNumberOfCapabilities(&iCount, &iSize);
    if (FAILED(hr)) { pStreamConfig->Release(); return hr; }

    BYTE* pSCC = new BYTE[iSize];
    AM_MEDIA_TYPE* pmt = nullptr;
    AM_MEDIA_TYPE* pFirstMT = nullptr;
    bool found = false;

    for (int i = 0; i < iCount && !found; ++i)
    {
        hr = pStreamConfig->GetStreamCaps(i, &pmt, pSCC);
        if (SUCCEEDED(hr) && pmt->formattype == FORMAT_VideoInfo)
        {
            VIDEOINFOHEADER* pVih = (VIDEOINFOHEADER*)pmt->pbFormat;
            int capW = pVih->bmiHeader.biWidth;
            int capH = abs(pVih->bmiHeader.biHeight);
            int capFPS = (pVih->AvgTimePerFrame > 0)
                ? (int)(10000000.0 / pVih->AvgTimePerFrame) : 0;

            if (capW == width && capH == height && capFPS == fps)
            {
                hr = pStreamConfig->SetFormat(pmt);
                found = SUCCEEDED(hr);
                SafeDeleteMediaType(pmt);
                pmt = nullptr;
            }
            else
            {
                if (!pFirstMT)
                    pFirstMT = pmt; // keep first as fallback template
                else
                    SafeDeleteMediaType(pmt);
                pmt = nullptr;
            }
        }
        else if (pmt)
        {
            SafeDeleteMediaType(pmt);
            pmt = nullptr;
        }
    }

    if (!found && pFirstMT && pFirstMT->formattype == FORMAT_VideoInfo)
    {
        // Custom format: patch the first capability as a template.
        VIDEOINFOHEADER* pVih = (VIDEOINFOHEADER*)pFirstMT->pbFormat;
        pVih->bmiHeader.biWidth     = width;
        pVih->bmiHeader.biHeight    = height;
        pVih->bmiHeader.biSizeImage = width * height * 3;
        pVih->AvgTimePerFrame       = (fps > 0) ? (REFERENCE_TIME)(10000000.0 / fps) : 333333;
        hr = pStreamConfig->SetFormat(pFirstMT);
        found = SUCCEEDED(hr);
    }

    if (pFirstMT) SafeDeleteMediaType(pFirstMT);
    delete[] pSCC;
    pStreamConfig->Release();
    return found ? S_OK : E_FAIL;
}

// ---------------------------------------------------------------------------
// Internal helper: DisableAllAutomaticControls
// Mirrors the FrameGrabber's approach: set every auto-mode property to manual
// (keeping current value). Failures for individual properties are tolerated.
// Returns pCameraControl and pVideoProcAmp (caller must Release when done).
// ---------------------------------------------------------------------------
static void DisableAllAutomaticControls(ICaptureGraphBuilder2* pBuilder,
                                         IBaseFilter* pCaptureFilter,
                                         IAMCameraControl** ppCC,
                                         IAMVideoProcAmp**  ppVPA)
{
    *ppCC  = nullptr;
    *ppVPA = nullptr;

    // --- IAMCameraControl ---
    IAMCameraControl* pCC = nullptr;
    HRESULT hr = pBuilder->FindInterface(
        &PIN_CATEGORY_CAPTURE, &MEDIATYPE_Video,
        pCaptureFilter, IID_IAMCameraControl, (void**)&pCC);

    if (SUCCEEDED(hr) && pCC)
    {
        static const long ccProps[] = {
            CameraControl_Exposure,
            CameraControl_Focus,
            CameraControl_Iris,
            CameraControl_Zoom,
        };
        for (long prop : ccProps)
        {
            long minV, maxV, step, defV, flags;
            if (SUCCEEDED(pCC->GetRange(prop, &minV, &maxV, &step, &defV, &flags)))
            {
                long cur;
                if (SUCCEEDED(pCC->Get(prop, &cur, &flags)))
                    pCC->Set(prop, cur, CameraControl_Flags_Manual);
            }
        }
        *ppCC = pCC; // caller keeps it
    }

    // --- IAMVideoProcAmp ---
    IAMVideoProcAmp* pVPA = nullptr;
    hr = pBuilder->FindInterface(
        &PIN_CATEGORY_CAPTURE, &MEDIATYPE_Video,
        pCaptureFilter, IID_IAMVideoProcAmp, (void**)&pVPA);

    if (SUCCEEDED(hr) && pVPA)
    {
        static const long vpaProps[] = {
            VideoProcAmp_WhiteBalance,
            VideoProcAmp_Gain,
            VideoProcAmp_BacklightCompensation,
            VideoProcAmp_Brightness,
            VideoProcAmp_Contrast,
            VideoProcAmp_Hue,
            VideoProcAmp_Saturation,
            VideoProcAmp_Sharpness,
            VideoProcAmp_Gamma,
        };
        for (long prop : vpaProps)
        {
            long minV, maxV, step, defV, flags;
            if (SUCCEEDED(pVPA->GetRange(prop, &minV, &maxV, &step, &defV, &flags)))
            {
                long cur;
                if (SUCCEEDED(pVPA->Get(prop, &cur, &flags)))
                    pVPA->Set(prop, cur, VideoProcAmp_Flags_Manual);
            }
        }
        *ppVPA = pVPA; // caller keeps it
    }
}

// ---------------------------------------------------------------------------
// Internal helper: find first output pin of a filter
// ---------------------------------------------------------------------------
static IPin* FindOutputPin(IBaseFilter* pFilter)
{
    IEnumPins* pEnum = nullptr;
    if (FAILED(pFilter->EnumPins(&pEnum))) return nullptr;

    IPin* pPin = nullptr;
    IPin* pFound = nullptr;
    while (pEnum->Next(1, &pPin, nullptr) == S_OK)
    {
        PIN_INFO info;
        if (SUCCEEDED(pPin->QueryPinInfo(&info)))
        {
            if (info.pFilter) info.pFilter->Release();
            if (info.dir == PINDIR_OUTPUT && !pFound)
            {
                pFound = pPin;
                pFound->AddRef();
            }
        }
        pPin->Release();
    }
    pEnum->Release();
    return pFound;
}

// ---------------------------------------------------------------------------
// Internal helper: find first input pin of a filter
// ---------------------------------------------------------------------------
static IPin* FindInputPin(IBaseFilter* pFilter)
{
    IEnumPins* pEnum = nullptr;
    if (FAILED(pFilter->EnumPins(&pEnum))) return nullptr;

    IPin* pPin = nullptr;
    IPin* pFound = nullptr;
    while (pEnum->Next(1, &pPin, nullptr) == S_OK)
    {
        PIN_INFO info;
        if (SUCCEEDED(pPin->QueryPinInfo(&info)))
        {
            if (info.pFilter) info.pFilter->Release();
            if (info.dir == PINDIR_INPUT && !pFound)
            {
                pFound = pPin;
                pFound->AddRef();
            }
        }
        pPin->Release();
    }
    pEnum->Release();
    return pFound;
}

// ---------------------------------------------------------------------------
// Cleanup helper
// ---------------------------------------------------------------------------
static void CleanupState(CameraState* state)
{
    if (!state) return;

    if (state->pMediaControl)
        state->pMediaControl->Stop();

    SafeRelease(state->pMediaControl);
    SafeRelease(state->pCameraControl);
    SafeRelease(state->pVideoProcAmp);
    SafeRelease(state->pSampleGrabber);
    SafeRelease(state->pSampleGrabFilter);
    SafeRelease(state->pNullRenderer);

    if (state->pCallback)
    {
        state->pCallback->Release();
        state->pCallback = nullptr;
    }

    SafeRelease(state->pCaptureFilter);
    SafeRelease(state->pCaptureBuilder);
    SafeRelease(state->pGraph);

    if (state->frameEvent)
    {
        CloseHandle(state->frameEvent);
        state->frameEvent = nullptr;
    }

    if (state->csInitialized)
    {
        DeleteCriticalSection(&state->cs);
        state->csInitialized = false;
    }
}

// ===========================================================================
// Exported API
// ===========================================================================

void* openCamera(int deviceIndex, int width, int height, int fps)
{
    if (width <= 0 || height <= 0 || fps <= 0 || deviceIndex < 0)
        return nullptr;

    CameraState* state = new CameraState();

    InitializeCriticalSection(&state->cs);
    state->csInitialized = true;

    state->frameEvent = CreateEvent(nullptr, FALSE, FALSE, nullptr); // auto-reset
    if (!state->frameEvent)
    {
        CleanupState(state);
        delete state;
        return nullptr;
    }

    state->width  = width;
    state->height = height;
    state->frameBuffer.resize((size_t)width * height * 3, 0);

    // COM init -- tolerate RPC_E_CHANGED_MODE (Unity may have already initialised COM)
    HRESULT hr = CoInitializeEx(nullptr, COINIT_MULTITHREADED);
    if (FAILED(hr) && hr != RPC_E_CHANGED_MODE)
    {
        CleanupState(state);
        delete state;
        return nullptr;
    }

    // Filter graph
    hr = CoCreateInstance(CLSID_FilterGraph, nullptr, CLSCTX_INPROC_SERVER,
                          IID_PPV_ARGS(&state->pGraph));
    if (FAILED(hr)) goto fail;

    // Capture graph builder
    hr = CoCreateInstance(CLSID_CaptureGraphBuilder2, nullptr, CLSCTX_INPROC_SERVER,
                          IID_PPV_ARGS(&state->pCaptureBuilder));
    if (FAILED(hr)) goto fail;

    state->pCaptureBuilder->SetFiltergraph(state->pGraph);

    // -----------------------------------------------------------------------
    // Enumerate video devices and select by index
    // -----------------------------------------------------------------------
    {
        ICreateDevEnum* pDevEnum = nullptr;
        hr = CoCreateInstance(CLSID_SystemDeviceEnum, nullptr, CLSCTX_INPROC_SERVER,
                              IID_PPV_ARGS(&pDevEnum));
        if (FAILED(hr)) goto fail;

        IEnumMoniker* pEnum = nullptr;
        hr = pDevEnum->CreateClassEnumerator(CLSID_VideoInputDeviceCategory, &pEnum, 0);
        pDevEnum->Release();
        if (FAILED(hr) || hr == S_FALSE || !pEnum) goto fail;

        IMoniker* pMoniker = nullptr;
        int idx = 0;
        bool found = false;
        while (pEnum->Next(1, &pMoniker, nullptr) == S_OK)
        {
            if (idx == deviceIndex)
            {
                hr = pMoniker->BindToObject(nullptr, nullptr, IID_PPV_ARGS(&state->pCaptureFilter));
                pMoniker->Release();
                found = SUCCEEDED(hr);
                break;
            }
            ++idx;
            pMoniker->Release();
        }
        pEnum->Release();

        if (!found || !state->pCaptureFilter) goto fail;
    }

    hr = state->pGraph->AddFilter(state->pCaptureFilter, L"Video Capture");
    if (FAILED(hr)) goto fail;

    // -----------------------------------------------------------------------
    // Configure resolution / FPS
    // -----------------------------------------------------------------------
    SetCameraResolutionAndFPS(state->pCaptureBuilder, state->pCaptureFilter, width, height, fps);

    // -----------------------------------------------------------------------
    // Disable all automatic controls; keep IAMCameraControl & IAMVideoProcAmp
    // -----------------------------------------------------------------------
    DisableAllAutomaticControls(state->pCaptureBuilder, state->pCaptureFilter,
                                 &state->pCameraControl, &state->pVideoProcAmp);

    // -----------------------------------------------------------------------
    // Sample Grabber
    // -----------------------------------------------------------------------
    hr = CoCreateInstance(CLSID_SampleGrabber, nullptr, CLSCTX_INPROC_SERVER,
                          IID_IBaseFilter, (void**)&state->pSampleGrabFilter);
    if (FAILED(hr)) goto fail;

    hr = state->pGraph->AddFilter(state->pSampleGrabFilter, L"Sample Grabber");
    if (FAILED(hr)) goto fail;

    hr = state->pSampleGrabFilter->QueryInterface(IID_ISampleGrabber,
                                                   (void**)&state->pSampleGrabber);
    if (FAILED(hr)) goto fail;

    // Request RGB24 -- DirectShow inserts a colour-space converter as needed.
    {
        AM_MEDIA_TYPE mt = {};
        mt.majortype  = MEDIATYPE_Video;
        mt.subtype    = MEDIASUBTYPE_RGB24;
        mt.formattype = FORMAT_VideoInfo;
        state->pSampleGrabber->SetMediaType(&mt);
    }
    state->pSampleGrabber->SetOneShot(FALSE);
    state->pSampleGrabber->SetBufferSamples(FALSE);

    // Attach callback
    state->pCallback = new FrameCallback();
    state->pCallback->pState = state;
    hr = state->pSampleGrabber->SetCallback(state->pCallback, 1); // 1 = BufferCB
    if (FAILED(hr)) goto fail;

    // -----------------------------------------------------------------------
    // Null Renderer (terminates the graph without displaying a window)
    // -----------------------------------------------------------------------
    hr = CoCreateInstance(CLSID_NullRenderer, nullptr, CLSCTX_INPROC_SERVER,
                          IID_PPV_ARGS(&state->pNullRenderer));
    if (FAILED(hr)) goto fail;

    hr = state->pGraph->AddFilter(state->pNullRenderer, L"Null Renderer");
    if (FAILED(hr)) goto fail;

    // -----------------------------------------------------------------------
    // Connect: Capture --> SampleGrabber --> NullRenderer
    // -----------------------------------------------------------------------
    // First leg: Capture → SampleGrabber (RenderStream handles format negotiation)
    hr = state->pCaptureBuilder->RenderStream(
        &PIN_CATEGORY_CAPTURE, &MEDIATYPE_Video,
        state->pCaptureFilter,
        nullptr,
        state->pSampleGrabFilter);
    if (FAILED(hr)) goto fail;

    // Second leg: SampleGrabber output → NullRenderer input
    {
        IPin* pSGOut   = FindOutputPin(state->pSampleGrabFilter);
        IPin* pNullIn  = FindInputPin(state->pNullRenderer);
        if (pSGOut && pNullIn)
            state->pGraph->Connect(pSGOut, pNullIn);
        if (pSGOut)  pSGOut->Release();
        if (pNullIn) pNullIn->Release();
    }

    // -----------------------------------------------------------------------
    // Check actual pixel format orientation after connection
    // -----------------------------------------------------------------------
    {
        AM_MEDIA_TYPE connectedMT = {};
        if (SUCCEEDED(state->pSampleGrabber->GetConnectedMediaType(&connectedMT)))
        {
            if (connectedMT.formattype == FORMAT_VideoInfo && connectedMT.pbFormat)
            {
                VIDEOINFOHEADER* pVih = (VIDEOINFOHEADER*)connectedMT.pbFormat;
                // Negative height = top-down (no flip needed)
                state->flipRows = (pVih->bmiHeader.biHeight > 0);
                // Update stored dimensions from negotiated format
                state->width  = pVih->bmiHeader.biWidth;
                state->height = abs(pVih->bmiHeader.biHeight);
                state->frameBuffer.resize((size_t)state->width * state->height * 3, 0);
            }
            if (connectedMT.cbFormat)
                CoTaskMemFree(connectedMT.pbFormat);
            if (connectedMT.pUnk)
                connectedMT.pUnk->Release();
        }
    }

    // -----------------------------------------------------------------------
    // Disable reference clock for lower latency
    // -----------------------------------------------------------------------
    {
        IMediaFilter* pMediaFilter = nullptr;
        if (SUCCEEDED(state->pGraph->QueryInterface(IID_PPV_ARGS(&pMediaFilter))))
        {
            pMediaFilter->SetSyncSource(nullptr);
            pMediaFilter->Release();
        }
    }

    // -----------------------------------------------------------------------
    // Get IMediaControl and start the graph
    // -----------------------------------------------------------------------
    hr = state->pGraph->QueryInterface(IID_PPV_ARGS(&state->pMediaControl));
    if (FAILED(hr)) goto fail;

    hr = state->pMediaControl->Run();
    if (FAILED(hr)) goto fail;

    state->running = true;
    return static_cast<void*>(state);

fail:
    CleanupState(state);
    delete state;
    return nullptr;
}

// ---------------------------------------------------------------------------

void releaseCamera(void* camera)
{
    if (!camera) return;
    CameraState* state = static_cast<CameraState*>(camera);
    state->running = false;
    // Unblock any waiting getCameraTexture call
    if (state->frameEvent)
        SetEvent(state->frameEvent);
    CleanupState(state);
    CoUninitialize();
    delete state;
}

// ---------------------------------------------------------------------------

int getCameraTexture(void* camera, unsigned char* data, int width, int height)
{
    if (!camera || !data)           return -1;
    if (width <= 0 || height <= 0)  return -2;

    CameraState* state = static_cast<CameraState*>(camera);

    if (!state->running)            return -3;
    if (width != state->width || height != state->height) return -5;

    DWORD result = WaitForSingleObject(state->frameEvent, 5000);
    if (result != WAIT_OBJECT_0)    return -4;

    if (!state->running)            return -3;

    EnterCriticalSection(&state->cs);
    memcpy(data, state->frameBuffer.data(), (size_t)width * height * 3);
    LeaveCriticalSection(&state->cs);

    return 1;
}

// ---------------------------------------------------------------------------

int getCameraDimensions(void* camera, int* width, int* height)
{
    if (!camera || !width || !height) return 0;
    CameraState* state = static_cast<CameraState*>(camera);
    *width  = state->width;
    *height = state->height;
    return 1;
}

// ---------------------------------------------------------------------------

int setCameraExposure(void* camera, long value)
{
    if (!camera) return 0;
    CameraState* state = static_cast<CameraState*>(camera);
    if (!state->pCameraControl) return 0;
    HRESULT hr = state->pCameraControl->Set(
        CameraControl_Exposure, value, CameraControl_Flags_Manual);
    return SUCCEEDED(hr) ? 1 : 0;
}

int setCameraGain(void* camera, long value)
{
    if (!camera) return 0;
    CameraState* state = static_cast<CameraState*>(camera);
    if (!state->pVideoProcAmp) return 0;
    HRESULT hr = state->pVideoProcAmp->Set(
        VideoProcAmp_Gain, value, VideoProcAmp_Flags_Manual);
    return SUCCEEDED(hr) ? 1 : 0;
}

int setCameraContrast(void* camera, long value)
{
    if (!camera) return 0;
    CameraState* state = static_cast<CameraState*>(camera);
    if (!state->pVideoProcAmp) return 0;
    HRESULT hr = state->pVideoProcAmp->Set(
        VideoProcAmp_Contrast, value, VideoProcAmp_Flags_Manual);
    return SUCCEEDED(hr) ? 1 : 0;
}
