#include <windows.h>
#include <dshow.h>
#include <strmif.h>
#include <uuids.h>
#include <iostream>
#include <string>
#include <conio.h>
#include <iomanip>
#include <vector>
#include <algorithm>
#include <cwctype>

// Sample Grabber interfaces (qedit.h is not available in standard Windows SDK)
// Forward declarations
struct ISampleGrabberCB;
struct ISampleGrabber;

// Define ISampleGrabberCB interface
struct ISampleGrabberCB : public IUnknown
{
    virtual STDMETHODIMP SampleCB(double SampleTime, IMediaSample* pSample) = 0;
    virtual STDMETHODIMP BufferCB(double SampleTime, BYTE* pBuffer, long BufferLen) = 0;
};

// Define ISampleGrabber interface
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

// CLSID for Sample Grabber filter
// Correct CLSID: {C1F400A0-3F08-11D3-9F0B-006008039E37}
static const GUID CLSID_SampleGrabber =
{ 0xC1F400A0, 0x3F08, 0x11D3, { 0x9F, 0x0B, 0x00, 0x60, 0x08, 0x03, 0x9E, 0x37 } };

// IID for ISampleGrabberCB
// {0579154A-2B53-4a10-B111-5F6C5E5E5E5E}
static const GUID IID_ISampleGrabberCB =
{ 0x0579154A, 0x2B53, 0x4A10, { 0xB1, 0x11, 0x5F, 0x6C, 0x5E, 0x5E, 0x5E, 0x5E } };

// IID for ISampleGrabber
// {6B652FFF-11FE-4fce-92AD-0266B5D7C78F}
static const GUID IID_ISampleGrabber =
{ 0x6B652FFF, 0x11FE, 0x4FCE, { 0x92, 0xAD, 0x02, 0x66, 0xB5, 0xD7, 0xC7, 0x8F } };

#pragma comment(lib, "strmiids.lib")
#pragma comment(lib, "ole32.lib")
#pragma comment(lib, "oleaut32.lib")
#pragma comment(lib, "quartz.lib")

// Helper function to delete media type (if not available from headers)
#ifndef DeleteMediaType
void DeleteMediaType(AM_MEDIA_TYPE* pmt)
{
    if (pmt != nullptr)
    {
        if (pmt->cbFormat != 0)
        {
            CoTaskMemFree((PVOID)pmt->pbFormat);
            pmt->cbFormat = 0;
            pmt->pbFormat = nullptr;
        }
        if (pmt->pUnk != nullptr)
        {
            pmt->pUnk->Release();
            pmt->pUnk = nullptr;
        }
        CoTaskMemFree(pmt);
    }
}
#endif

// Forward declarations
HRESULT EnumerateDevices(REFGUID category, IEnumMoniker** ppEnum);
HRESULT CreateCaptureGraph(IGraphBuilder** ppGraph, IBaseFilter** ppCaptureFilter, IBaseFilter** ppRendererFilter);
HRESULT SetCameraResolutionAndFPS(IBaseFilter* pCaptureFilter, int width, int height, int fps);
HRESULT SetCameraExposure(IBaseFilter* pCaptureFilter, long exposureValue);
HRESULT DisableAllAutomaticControls(IBaseFilter* pCaptureFilter);
HRESULT TryRegisterSampleGrabber();
void CleanupInterfaces();

// Frame timing callback class
class FrameTimingCallback : public ISampleGrabberCB
{
public:
    FrameTimingCallback() : m_cRef(1), m_slowFrameCount(0), m_totalFrameCount(0), m_lastFrameTime(0), m_lastStatsTime(0), m_frequency(0), m_statsReady(false), m_statsStartPosition({0, 0}), m_statsPositionSet(false), m_streamStartTime(0)
    {
        InitializeCriticalSection(&m_cs);
        LARGE_INTEGER freq;
        QueryPerformanceFrequency(&freq);
        m_frequency = freq.QuadPart;
        m_intervals.reserve(10000); // Reserve space for 10k measurements
        m_slowIntervals.reserve(1000); // Reserve space for last 1000 slow frames (accumulative)
        // Initialize last stats time to current time
        LARGE_INTEGER currentTime;
        QueryPerformanceCounter(&currentTime);
        m_lastStatsTime = currentTime.QuadPart;
    }

    ~FrameTimingCallback()
    {
        DeleteCriticalSection(&m_cs);
    }

    // IUnknown methods
    STDMETHODIMP QueryInterface(REFIID riid, void** ppv)
    {
        if (riid == IID_ISampleGrabberCB || riid == IID_IUnknown)
        {
            *ppv = (void*)static_cast<ISampleGrabberCB*>(this);
            AddRef();
            return S_OK;
        }
        return E_NOINTERFACE;
    }

    STDMETHODIMP_(ULONG) AddRef() { return InterlockedIncrement(&m_cRef); }
    STDMETHODIMP_(ULONG) Release() {
        LONG cRef = InterlockedDecrement(&m_cRef);
        if (cRef == 0) delete this;
        return cRef;
    }

    // ISampleGrabberCB methods
    STDMETHODIMP SampleCB(double SampleTime, IMediaSample* pSample) { return E_NOTIMPL; }

    STDMETHODIMP BufferCB(double SampleTime, BYTE* pBuffer, long BufferLen)
    {
        // This callback runs on DirectShow's capture thread - keep it lightweight!
        LARGE_INTEGER currentTime;
        QueryPerformanceCounter(&currentTime);

        EnterCriticalSection(&m_cs);

        if (m_lastFrameTime > 0)
        {
            // Calculate interval in milliseconds
            double intervalMs = ((double)(currentTime.QuadPart - m_lastFrameTime) * 1000.0) / m_frequency;
            m_intervals.push_back(intervalMs);
            m_totalFrameCount++; // Accumulative total

            // Track frames that take 30ms or more (accumulative)
            if (intervalMs >= 30.0)
            {
                m_slowFrameCount++;

                // Add to slow intervals list (keep last 1000, accumulative)
                m_slowIntervals.push_back(intervalMs);
                if (m_slowIntervals.size() > 1000)
                {
                    m_slowIntervals.erase(m_slowIntervals.begin());
                }

                // Signal that stats are ready to print whenever we detect a slow frame
                // Don't print here - let main thread do it to avoid blocking
                if (!m_intervals.empty())
                {
                    m_statsReady = true;
                }
            }
        }

        m_lastFrameTime = currentTime.QuadPart;
        LeaveCriticalSection(&m_cs);

        return S_OK;
    }

    // Check if stats are ready and return a copy of the data for printing
    // Returns true if stats should be printed, false otherwise
    // forceUpdate: if true, print stats even if no new frames detected (for time-based updates)
    bool GetStatsForPrinting(std::vector<double>& intervals, std::vector<double>& slowIntervals,
        size_t& slowFrameCount, size_t& totalFrameCount, bool forceUpdate = false)
    {
        EnterCriticalSection(&m_cs);
        bool shouldPrint = forceUpdate || (m_statsReady && !m_intervals.empty());

        if (shouldPrint)
        {
            // Copy data while holding the lock
            intervals = m_intervals;
            slowIntervals = m_slowIntervals; // Accumulative - don't clear
            slowFrameCount = m_slowFrameCount; // Accumulative - don't clear
            totalFrameCount = m_totalFrameCount; // Accumulative total

            // Clear only current period statistics (but keep accumulative slow frames and intervals)
            // Only clear if we had actual frame data, not for forced time-based updates
            if (!forceUpdate)
            {
                // m_intervals is no longer cleared - it accumulates all intervals
                m_statsReady = false;
            }
            // Note: m_intervals, m_slowIntervals, m_slowFrameCount, and m_totalFrameCount are NOT cleared
            
            // Update last stats time
            LARGE_INTEGER currentTime;
            QueryPerformanceCounter(&currentTime);
            m_lastStatsTime = currentTime.QuadPart;
        }

        LeaveCriticalSection(&m_cs);
        return shouldPrint;
    }

    void PrintStatistics(bool forceUpdate = false)
    {
        // Check stream state before starting
        if (!std::wcout.good())
        {
            std::wcout.clear();
        }

        // This method is now called from the main thread
        // Get a copy of the data first
        std::vector<double> intervals;
        std::vector<double> slowIntervals;
        size_t slowFrameCount = 0;
        size_t totalFrameCount = 0;

        if (!GetStatsForPrinting(intervals, slowIntervals, slowFrameCount, totalFrameCount, forceUpdate))
        {
            return; // No stats ready
        }

        // For time-based updates, allow printing even with empty intervals (to update Time log)
        if (intervals.empty() && !forceUpdate) return;

        // Get console handle and current cursor position
        HANDLE hConsole = GetStdHandle(STD_OUTPUT_HANDLE);
        CONSOLE_SCREEN_BUFFER_INFO csbi;
        COORD currentPos;
        
        if (GetConsoleScreenBufferInfo(hConsole, &csbi))
        {
            currentPos = csbi.dwCursorPosition;
            
            // If this is the first time printing stats, save the position
            // Otherwise, move cursor back to the saved position
            if (!m_statsPositionSet)
            {
                std::wcout << L"\n"; // Start on a new line for first print
                // Save position after the newline (one line down)
                if (GetConsoleScreenBufferInfo(hConsole, &csbi))
                {
                    m_statsStartPosition = csbi.dwCursorPosition;
                    m_statsPositionSet = true;
                }
            }
            else
            {
                // Move cursor back to the start of stats section
                SetConsoleCursorPosition(hConsole, m_statsStartPosition);
                
                // Calculate how many lines we need to clear (estimate based on slow intervals)
                // We'll clear enough lines to cover the previous output
                int linesToClear = 4; // Header + time + average + percentage
                if (!slowIntervals.empty())
                {
                    linesToClear += 1 + (slowIntervals.size() + 9) / 10; // Header + data lines
                }
                else
                {
                    linesToClear += 1; // "No slow frames" line
                }
                
                // Clear the lines by printing spaces
                for (int i = 0; i < linesToClear; ++i)
                {
                    std::wcout << std::wstring(csbi.dwSize.X, L' ') << std::endl;
                }
                
                // Move cursor back to start position
                SetConsoleCursorPosition(hConsole, m_statsStartPosition);
            }
        }
        else
        {
            // Fallback: if we can't get console info, just print normally
            if (!m_statsPositionSet)
            {
                std::wcout << L"\n";
                m_statsPositionSet = true;
            }
        }

        // Calculate elapsed time since stream started
        LARGE_INTEGER currentTime;
        QueryPerformanceCounter(&currentTime);
        double elapsedSeconds = 0.0;
        if (m_streamStartTime > 0)
        {
            elapsedSeconds = ((double)(currentTime.QuadPart - m_streamStartTime)) / m_frequency;
        }

        std::wcout << L"=== Frame Timing Statistics ===" << std::endl;
        std::wcout << L"Time: " << std::fixed << std::setprecision(2) << elapsedSeconds << L" s" << std::endl;
        
        // Calculate average interval for current period (only if we have intervals)
        double avgInterval = 0.0;
        if (!intervals.empty())
        {
            double sum = 0;
            for (double interval : intervals)
            {
                sum += interval;
            }
            avgInterval = sum / intervals.size();
            std::wcout << L"Average interval: " << std::fixed << std::setprecision(3) << avgInterval << L" ms" << std::endl;
        }
        else
        {
            std::wcout << L"Average interval: N/A (no frames in this period)" << std::endl;
        }
        
        std::wcout << L"Percentage of slow frames: " << std::fixed << std::setprecision(2)
            << (totalFrameCount > 0 ? (100.0 * slowFrameCount / totalFrameCount) : 0.0) << L"%" << std::endl;

        if (!slowIntervals.empty())
        {
            std::wcout << L"Last " << slowIntervals.size() << L" slow frame intervals:" << std::endl;

            // Print intervals in a compact format (10 per line)
            for (size_t i = 0; i < slowIntervals.size(); ++i)
            {
                if (i > 0 && i % 10 == 0)
                {
                    std::wcout << std::endl;
                }
                std::wcout << std::fixed << std::setprecision(2) << std::setw(7) << slowIntervals[i] << L"ms ";
            }
            std::wcout << std::endl;
        }
        else
        {
            std::wcout << L"No slow frames recorded yet." << std::endl;
        }
        std::wcout.flush();
    }

    bool AreStatsReady() const
    {
        EnterCriticalSection(&m_cs);
        bool ready = m_statsReady && !m_intervals.empty();
        LeaveCriticalSection(&m_cs);
        return ready;
    }

    // Check if 1 second has passed since last stats update
    bool ShouldUpdateStatsByTime() const
    {
        EnterCriticalSection(&m_cs);
        LARGE_INTEGER currentTime;
        QueryPerformanceCounter(&currentTime);
        double elapsedSeconds = ((double)(currentTime.QuadPart - m_lastStatsTime)) / m_frequency;
        bool shouldUpdate = elapsedSeconds >= 1.0;
        LeaveCriticalSection(&m_cs);
        return shouldUpdate;
    }

    void SetStreamStartTime()
    {
        EnterCriticalSection(&m_cs);
        LARGE_INTEGER currentTime;
        QueryPerformanceCounter(&currentTime);
        m_streamStartTime = currentTime.QuadPart;
        LeaveCriticalSection(&m_cs);
    }

private:
    mutable CRITICAL_SECTION m_cs; // Protects all member variables
    LONG m_cRef;
    size_t m_slowFrameCount; // Accumulative count of slow frames
    size_t m_totalFrameCount; // Accumulative total frame count
    LONGLONG m_lastFrameTime;
    LONGLONG m_lastStatsTime; // Time when statistics were last printed
    LONGLONG m_frequency;
    std::vector<double> m_intervals;
    std::vector<double> m_slowIntervals; // Accumulative list of slow frames (last 1000)
    bool m_statsReady; // Flag to indicate stats are ready to print (set by callback, checked by main thread)
    COORD m_statsStartPosition; // Cursor position where stats section starts
    bool m_statsPositionSet; // Whether the stats position has been set
    LONGLONG m_streamStartTime; // Time when the camera stream started
};

// Global COM interfaces
IGraphBuilder* g_pGraph = nullptr;
ICaptureGraphBuilder2* g_pCaptureGraphBuilder = nullptr;
IMediaControl* g_pMediaControl = nullptr;
IBaseFilter* g_pCaptureFilter = nullptr;
IBaseFilter* g_pRendererFilter = nullptr;
IBaseFilter* g_pSampleGrabberFilter = nullptr;
ISampleGrabber* g_pSampleGrabber = nullptr;
IVideoWindow* g_pVideoWindow = nullptr;
FrameTimingCallback* g_pFrameCallback = nullptr;

HRESULT EnumerateDevices(REFGUID category, IEnumMoniker** ppEnum)
{
    ICreateDevEnum* pDevEnum = nullptr;
    HRESULT hr = CoCreateInstance(CLSID_SystemDeviceEnum, nullptr, CLSCTX_INPROC_SERVER,
        IID_PPV_ARGS(&pDevEnum));

    if (SUCCEEDED(hr))
    {
        hr = pDevEnum->CreateClassEnumerator(category, ppEnum, 0);
        if (hr == S_FALSE)
        {
            hr = VFW_E_NOT_FOUND;
        }
        pDevEnum->Release();
    }
    return hr;
}

HRESULT CreateCaptureGraph(IGraphBuilder** ppGraph, IBaseFilter** ppCaptureFilter, IBaseFilter** ppRendererFilter)
{
    HRESULT hr = CoCreateInstance(CLSID_FilterGraph, nullptr, CLSCTX_INPROC_SERVER,
        IID_PPV_ARGS(ppGraph));

    if (FAILED(hr))
        return hr;

    // Create Capture Graph Builder
    hr = CoCreateInstance(CLSID_CaptureGraphBuilder2, nullptr, CLSCTX_INPROC_SERVER,
        IID_PPV_ARGS(&g_pCaptureGraphBuilder));

    if (FAILED(hr))
    {
        (*ppGraph)->Release();
        return hr;
    }

    g_pCaptureGraphBuilder->SetFiltergraph(*ppGraph);

    // Enumerate video capture devices
    IEnumMoniker* pEnum = nullptr;
    hr = EnumerateDevices(CLSID_VideoInputDeviceCategory, &pEnum);

    if (FAILED(hr))
    {
        std::wcout << L"Failed to enumerate video devices" << std::endl;
        return hr;
    }

    // Find the first device with "See3CAM" in its name
    IMoniker* pMoniker = nullptr;
    IMoniker* pSelectedMoniker = nullptr;
    bool foundSee3CAM = false;

    // Reset enumeration to start from beginning
    pEnum->Reset();

    // Loop through all devices to find one with "See3CAM" in the name
    while (pEnum->Next(1, &pMoniker, nullptr) == S_OK)
    {
        IPropertyBag* pPropBag = nullptr;
        hr = pMoniker->BindToStorage(nullptr, nullptr, IID_PPV_ARGS(&pPropBag));

        if (SUCCEEDED(hr))
        {
            VARIANT var;
            VariantInit(&var);
            hr = pPropBag->Read(L"FriendlyName", &var, nullptr);
            if (SUCCEEDED(hr))
            {
                // Check if "See3CAM" is in the camera name (case-insensitive)
                std::wstring cameraName(var.bstrVal);
                std::wstring searchTerm(L"See3CAM");
                
                // Convert to uppercase for case-insensitive comparison
                std::wstring upperCameraName = cameraName;
                std::wstring upperSearchTerm = searchTerm;
                std::transform(upperCameraName.begin(), upperCameraName.end(), upperCameraName.begin(), ::towupper);
                std::transform(upperSearchTerm.begin(), upperSearchTerm.end(), upperSearchTerm.begin(), ::towupper);

                if (upperCameraName.find(upperSearchTerm) != std::wstring::npos)
                {
                    std::wcout << L"Found See3CAM camera: " << var.bstrVal << std::endl;
                    pSelectedMoniker = pMoniker;
                    pSelectedMoniker->AddRef();
                    foundSee3CAM = true;
                    VariantClear(&var);
                    pPropBag->Release();
                    pMoniker->Release();
                    break;
                }
                else
                {
                    std::wcout << L"Skipping camera: " << var.bstrVal << std::endl;
                }
            }
            VariantClear(&var);
            pPropBag->Release();
        }
        pMoniker->Release();
    }

    if (foundSee3CAM && pSelectedMoniker)
    {
        // Create the capture filter from the selected See3CAM camera
        hr = pSelectedMoniker->BindToObject(nullptr, nullptr, IID_PPV_ARGS(ppCaptureFilter));
        pSelectedMoniker->Release();

        if (SUCCEEDED(hr))
        {
            // Add capture filter to graph
            hr = (*ppGraph)->AddFilter(*ppCaptureFilter, L"Video Capture");
        }
    }
    else
    {
        hr = VFW_E_NOT_FOUND;
        std::wcout << L"No See3CAM camera found" << std::endl;
    }

    pEnum->Release();

    if (FAILED(hr))
        return hr;

    // Create video renderer
    hr = CoCreateInstance(CLSID_VideoRenderer, nullptr, CLSCTX_INPROC_SERVER,
        IID_PPV_ARGS(ppRendererFilter));

    if (SUCCEEDED(hr))
    {
        hr = (*ppGraph)->AddFilter(*ppRendererFilter, L"Video Renderer");
    }

    return hr;
}

HRESULT SetCameraResolutionAndFPS(IBaseFilter* pCaptureFilter, int width, int height, int fps)
{
    if (!pCaptureFilter)
        return E_POINTER;

    IAMStreamConfig* pStreamConfig = nullptr;
    HRESULT hr = g_pCaptureGraphBuilder->FindInterface(
        &PIN_CATEGORY_CAPTURE,
        &MEDIATYPE_Video,
        pCaptureFilter,
        IID_IAMStreamConfig,
        (void**)&pStreamConfig);

    if (FAILED(hr))
    {
        std::wcout << L"Failed to get IAMStreamConfig interface" << std::endl;
        return hr;
    }

    // Get number of capabilities
    int iCount = 0, iSize = 0;
    hr = pStreamConfig->GetNumberOfCapabilities(&iCount, &iSize);

    if (FAILED(hr))
    {
        pStreamConfig->Release();
        return hr;
    }

    // Allocate memory for the capabilities
    BYTE* pSCC = new BYTE[iSize];
    VIDEO_STREAM_CONFIG_CAPS* pVSCC = (VIDEO_STREAM_CONFIG_CAPS*)pSCC;
    AM_MEDIA_TYPE* pmt = nullptr;

    bool found = false;
    for (int i = 0; i < iCount; i++)
    {
        hr = pStreamConfig->GetStreamCaps(i, &pmt, pSCC);
        if (SUCCEEDED(hr))
        {
            if (pmt->formattype == FORMAT_VideoInfo)
            {
                VIDEOINFOHEADER* pVih = (VIDEOINFOHEADER*)pmt->pbFormat;
                int capWidth = pVih->bmiHeader.biWidth;
                int capHeight = abs(pVih->bmiHeader.biHeight);
                REFERENCE_TIME rtAvgTimePerFrame = pVih->AvgTimePerFrame;
                int capFPS = (int)(10000000.0 / rtAvgTimePerFrame);

                // Check if this matches our desired resolution and FPS
                if (capWidth == width && capHeight == height && capFPS == fps)
                {
                    hr = pStreamConfig->SetFormat(pmt);
                    if (SUCCEEDED(hr))
                    {
                        std::wcout << L"Set resolution to " << width << L"x" << height
                            << L" at " << fps << L" FPS" << std::endl;
                        found = true;
                    }
                    DeleteMediaType(pmt);
                    break;
                }
            }
            DeleteMediaType(pmt);
        }
    }

    if (!found)
    {
        // Try to set closest match or create custom format
        std::wcout << L"Exact match not found, trying to set custom format..." << std::endl;

        // Get the first capability to use as template
        hr = pStreamConfig->GetStreamCaps(0, &pmt, pSCC);
        if (SUCCEEDED(hr))
        {
            if (pmt->formattype == FORMAT_VideoInfo)
            {
                VIDEOINFOHEADER* pVih = (VIDEOINFOHEADER*)pmt->pbFormat;

                // Set desired resolution
                pVih->bmiHeader.biWidth = width;
                pVih->bmiHeader.biHeight = height;
                pVih->bmiHeader.biSizeImage = width * height * 3; // RGB24

                // Set desired FPS
                pVih->AvgTimePerFrame = (REFERENCE_TIME)(10000000.0 / fps);

                // Try to set the format
                hr = pStreamConfig->SetFormat(pmt);
                if (SUCCEEDED(hr))
                {
                    std::wcout << L"Set custom format: " << width << L"x" << height
                        << L" at " << fps << L" FPS" << std::endl;
                }
                else
                {
                    std::wcout << L"Failed to set custom format. Error: 0x" << std::hex << hr << std::endl;
                }
            }
            DeleteMediaType(pmt);
        }
    }

    delete[] pSCC;
    pStreamConfig->Release();
    return hr;
}

HRESULT SetCameraExposure(IBaseFilter* pCaptureFilter, long exposureValue)
{
    if (!pCaptureFilter)
        return E_POINTER;

    IAMCameraControl* pCameraControl = nullptr;
    HRESULT hr = g_pCaptureGraphBuilder->FindInterface(
        &PIN_CATEGORY_CAPTURE,
        &MEDIATYPE_Video,
        pCaptureFilter,
        IID_IAMCameraControl,
        (void**)&pCameraControl);

    if (FAILED(hr))
    {
        std::wcout << L"Failed to get IAMCameraControl interface" << std::endl;
        return hr;
    }

    // Set exposure to manual mode and set the value
    // CameraControl_Flags_Manual = 0x0001 means manual control
    hr = pCameraControl->Set(CameraControl_Exposure, exposureValue, CameraControl_Flags_Manual);

    if (SUCCEEDED(hr))
    {
        std::wcout << L"Set camera exposure to " << exposureValue << std::endl;
    }
    else
    {
        std::wcout << L"Failed to set camera exposure. Error: 0x" << std::hex << hr << std::endl;
    }

    pCameraControl->Release();
    return hr;
}

HRESULT DisableAllAutomaticControls(IBaseFilter* pCaptureFilter)
{
    if (!pCaptureFilter)
        return E_POINTER;

    HRESULT hrTemp = S_OK;

    // Disable automatic camera controls (IAMCameraControl)
    IAMCameraControl* pCameraControl = nullptr;
    hrTemp = g_pCaptureGraphBuilder->FindInterface(
        &PIN_CATEGORY_CAPTURE,
        &MEDIATYPE_Video,
        pCaptureFilter,
        IID_IAMCameraControl,
        (void**)&pCameraControl);

    if (SUCCEEDED(hrTemp))
    {
        long minVal, maxVal, stepVal, defaultVal, flags;

        // Disable Auto Exposure (set to manual)
        if (SUCCEEDED(pCameraControl->GetRange(CameraControl_Exposure, &minVal, &maxVal, &stepVal, &defaultVal, &flags)))
        {
            // Get current value first
            long currentVal;
            if (SUCCEEDED(pCameraControl->Get(CameraControl_Exposure, &currentVal, &flags)))
            {
                // Set to manual mode (keep current value)
                hrTemp = pCameraControl->Set(CameraControl_Exposure, currentVal, CameraControl_Flags_Manual);
                if (SUCCEEDED(hrTemp))
                {
                    std::wcout << L"Disabled auto exposure (set to manual)" << std::endl;
                }
            }
        }

        // Disable Auto Focus (set to manual)
        if (SUCCEEDED(pCameraControl->GetRange(CameraControl_Focus, &minVal, &maxVal, &stepVal, &defaultVal, &flags)))
        {
            long currentVal;
            if (SUCCEEDED(pCameraControl->Get(CameraControl_Focus, &currentVal, &flags)))
            {
                hrTemp = pCameraControl->Set(CameraControl_Focus, currentVal, CameraControl_Flags_Manual);
                if (SUCCEEDED(hrTemp))
                {
                    std::wcout << L"Disabled auto focus (set to manual)" << std::endl;
                }
            }
        }

        // Disable Auto Iris (set to manual)
        if (SUCCEEDED(pCameraControl->GetRange(CameraControl_Iris, &minVal, &maxVal, &stepVal, &defaultVal, &flags)))
        {
            long currentVal;
            if (SUCCEEDED(pCameraControl->Get(CameraControl_Iris, &currentVal, &flags)))
            {
                hrTemp = pCameraControl->Set(CameraControl_Iris, currentVal, CameraControl_Flags_Manual);
                if (SUCCEEDED(hrTemp))
                {
                    std::wcout << L"Disabled auto iris (set to manual)" << std::endl;
                }
            }
        }

        // Disable Auto Zoom (set to manual)
        if (SUCCEEDED(pCameraControl->GetRange(CameraControl_Zoom, &minVal, &maxVal, &stepVal, &defaultVal, &flags)))
        {
            long currentVal;
            if (SUCCEEDED(pCameraControl->Get(CameraControl_Zoom, &currentVal, &flags)))
            {
                hrTemp = pCameraControl->Set(CameraControl_Zoom, currentVal, CameraControl_Flags_Manual);
                if (SUCCEEDED(hrTemp))
                {
                    std::wcout << L"Disabled auto zoom (set to manual)" << std::endl;
                }
            }
        }

        pCameraControl->Release();
    }

    // Disable automatic video processing controls (IAMVideoProcAmp)
    IAMVideoProcAmp* pVideoProcAmp = nullptr;
    hrTemp = g_pCaptureGraphBuilder->FindInterface(
        &PIN_CATEGORY_CAPTURE,
        &MEDIATYPE_Video,
        pCaptureFilter,
        IID_IAMVideoProcAmp,
        (void**)&pVideoProcAmp);

    if (SUCCEEDED(hrTemp))
    {
        long minVal, maxVal, stepVal, defaultVal, flags;

        // Disable Auto White Balance (set to manual)
        if (SUCCEEDED(pVideoProcAmp->GetRange(VideoProcAmp_WhiteBalance, &minVal, &maxVal, &stepVal, &defaultVal, &flags)))
        {
            long currentVal;
            if (SUCCEEDED(pVideoProcAmp->Get(VideoProcAmp_WhiteBalance, &currentVal, &flags)))
            {
                hrTemp = pVideoProcAmp->Set(VideoProcAmp_WhiteBalance, currentVal, VideoProcAmp_Flags_Manual);
                if (SUCCEEDED(hrTemp))
                {
                    std::wcout << L"Disabled auto white balance (set to manual)" << std::endl;
                }
            }
        }

        // Disable Auto Gain (set to manual)
        if (SUCCEEDED(pVideoProcAmp->GetRange(VideoProcAmp_Gain, &minVal, &maxVal, &stepVal, &defaultVal, &flags)))
        {
            long currentVal;
            if (SUCCEEDED(pVideoProcAmp->Get(VideoProcAmp_Gain, &currentVal, &flags)))
            {
                hrTemp = pVideoProcAmp->Set(VideoProcAmp_Gain, currentVal, VideoProcAmp_Flags_Manual);
                if (SUCCEEDED(hrTemp))
                {
                    std::wcout << L"Disabled auto gain (set to manual)" << std::endl;
                }
            }
        }

        // Disable Auto Backlight Compensation (set to manual)
        if (SUCCEEDED(pVideoProcAmp->GetRange(VideoProcAmp_BacklightCompensation, &minVal, &maxVal, &stepVal, &defaultVal, &flags)))
        {
            long currentVal;
            if (SUCCEEDED(pVideoProcAmp->Get(VideoProcAmp_BacklightCompensation, &currentVal, &flags)))
            {
                hrTemp = pVideoProcAmp->Set(VideoProcAmp_BacklightCompensation, currentVal, VideoProcAmp_Flags_Manual);
                if (SUCCEEDED(hrTemp))
                {
                    std::wcout << L"Disabled auto backlight compensation (set to manual)" << std::endl;
                }
            }
        }

        // Disable Auto Brightness (set to manual) - if supported
        if (SUCCEEDED(pVideoProcAmp->GetRange(VideoProcAmp_Brightness, &minVal, &maxVal, &stepVal, &defaultVal, &flags)))
        {
            long currentVal;
            if (SUCCEEDED(pVideoProcAmp->Get(VideoProcAmp_Brightness, &currentVal, &flags)))
            {
                // Only set to manual if it's currently in auto mode
                if (flags & VideoProcAmp_Flags_Auto)
                {
                    hrTemp = pVideoProcAmp->Set(VideoProcAmp_Brightness, currentVal, VideoProcAmp_Flags_Manual);
                    if (SUCCEEDED(hrTemp))
                    {
                        std::wcout << L"Disabled auto brightness (set to manual)" << std::endl;
                    }
                }
            }
        }

        // Disable Auto Contrast (set to manual) - if supported
        if (SUCCEEDED(pVideoProcAmp->GetRange(VideoProcAmp_Contrast, &minVal, &maxVal, &stepVal, &defaultVal, &flags)))
        {
            long currentVal;
            if (SUCCEEDED(pVideoProcAmp->Get(VideoProcAmp_Contrast, &currentVal, &flags)))
            {
                if (flags & VideoProcAmp_Flags_Auto)
                {
                    hrTemp = pVideoProcAmp->Set(VideoProcAmp_Contrast, currentVal, VideoProcAmp_Flags_Manual);
                    if (SUCCEEDED(hrTemp))
                    {
                        std::wcout << L"Disabled auto contrast (set to manual)" << std::endl;
                    }
                }
            }
        }

        // Disable Auto Hue (set to manual) - if supported
        if (SUCCEEDED(pVideoProcAmp->GetRange(VideoProcAmp_Hue, &minVal, &maxVal, &stepVal, &defaultVal, &flags)))
        {
            long currentVal;
            if (SUCCEEDED(pVideoProcAmp->Get(VideoProcAmp_Hue, &currentVal, &flags)))
            {
                if (flags & VideoProcAmp_Flags_Auto)
                {
                    hrTemp = pVideoProcAmp->Set(VideoProcAmp_Hue, currentVal, VideoProcAmp_Flags_Manual);
                    if (SUCCEEDED(hrTemp))
                    {
                        std::wcout << L"Disabled auto hue (set to manual)" << std::endl;
                    }
                }
            }
        }

        // Disable Auto Saturation (set to manual) - if supported
        if (SUCCEEDED(pVideoProcAmp->GetRange(VideoProcAmp_Saturation, &minVal, &maxVal, &stepVal, &defaultVal, &flags)))
        {
            long currentVal;
            if (SUCCEEDED(pVideoProcAmp->Get(VideoProcAmp_Saturation, &currentVal, &flags)))
            {
                if (flags & VideoProcAmp_Flags_Auto)
                {
                    hrTemp = pVideoProcAmp->Set(VideoProcAmp_Saturation, currentVal, VideoProcAmp_Flags_Manual);
                    if (SUCCEEDED(hrTemp))
                    {
                        std::wcout << L"Disabled auto saturation (set to manual)" << std::endl;
                    }
                }
            }
        }

        // Disable Auto Sharpness (set to manual) - if supported
        if (SUCCEEDED(pVideoProcAmp->GetRange(VideoProcAmp_Sharpness, &minVal, &maxVal, &stepVal, &defaultVal, &flags)))
        {
            long currentVal;
            if (SUCCEEDED(pVideoProcAmp->Get(VideoProcAmp_Sharpness, &currentVal, &flags)))
            {
                if (flags & VideoProcAmp_Flags_Auto)
                {
                    hrTemp = pVideoProcAmp->Set(VideoProcAmp_Sharpness, currentVal, VideoProcAmp_Flags_Manual);
                    if (SUCCEEDED(hrTemp))
                    {
                        std::wcout << L"Disabled auto sharpness (set to manual)" << std::endl;
                    }
                }
            }
        }

        // Disable Auto Gamma (set to manual) - if supported
        if (SUCCEEDED(pVideoProcAmp->GetRange(VideoProcAmp_Gamma, &minVal, &maxVal, &stepVal, &defaultVal, &flags)))
        {
            long currentVal;
            if (SUCCEEDED(pVideoProcAmp->Get(VideoProcAmp_Gamma, &currentVal, &flags)))
            {
                if (flags & VideoProcAmp_Flags_Auto)
                {
                    hrTemp = pVideoProcAmp->Set(VideoProcAmp_Gamma, currentVal, VideoProcAmp_Flags_Manual);
                    if (SUCCEEDED(hrTemp))
                    {
                        std::wcout << L"Disabled auto gamma (set to manual)" << std::endl;
                    }
                }
            }
        }

        pVideoProcAmp->Release();
    }
    else
    {
        std::wcout << L"Warning: IAMVideoProcAmp interface not available (some cameras may not support this)" << std::endl;
    }

    std::wcout << L"Finished disabling all automatic camera controls" << std::endl;
    return S_OK;
}

HRESULT TryRegisterSampleGrabber()
{
    // Try to find and register qedit.dll
    // First try system32 (64-bit) or SysWOW64 (32-bit on 64-bit system)
    const wchar_t* paths[] = {
        L"C:\\Windows\\System32\\qedit.dll",
        L"C:\\Windows\\SysWOW64\\qedit.dll",
        L"qedit.dll"  // Try current directory or PATH
    };

    for (int i = 0; i < 3; i++)
    {
        HMODULE hModule = LoadLibraryW(paths[i]);
        if (hModule)
        {
            typedef HRESULT(WINAPI* DllRegisterServerProc)();
            DllRegisterServerProc pDllRegisterServer = (DllRegisterServerProc)GetProcAddress(hModule, "DllRegisterServer");
            if (pDllRegisterServer)
            {
                HRESULT hr = pDllRegisterServer();
                FreeLibrary(hModule);
                if (SUCCEEDED(hr))
                {
                    return hr;
                }
            }
            else
            {
                FreeLibrary(hModule);
            }
        }
    }
    return E_FAIL;
}

void CleanupInterfaces()
{
    if (g_pMediaControl)
    {
        g_pMediaControl->Stop();
        g_pMediaControl->Release();
        g_pMediaControl = nullptr;
    }

    if (g_pVideoWindow)
    {
        g_pVideoWindow->put_Visible(OAFALSE);
        g_pVideoWindow->put_Owner((OAHWND)nullptr);
        g_pVideoWindow->Release();
        g_pVideoWindow = nullptr;
    }

    if (g_pSampleGrabber)
    {
        g_pSampleGrabber->Release();
        g_pSampleGrabber = nullptr;
    }

    if (g_pSampleGrabberFilter)
    {
        g_pSampleGrabberFilter->Release();
        g_pSampleGrabberFilter = nullptr;
    }

    if (g_pFrameCallback)
    {
        g_pFrameCallback->Release();
        g_pFrameCallback = nullptr;
    }

    if (g_pRendererFilter)
    {
        g_pRendererFilter->Release();
        g_pRendererFilter = nullptr;
    }

    if (g_pCaptureFilter)
    {
        g_pCaptureFilter->Release();
        g_pCaptureFilter = nullptr;
    }

    if (g_pCaptureGraphBuilder)
    {
        g_pCaptureGraphBuilder->Release();
        g_pCaptureGraphBuilder = nullptr;
    }

    if (g_pGraph)
    {
        g_pGraph->Release();
        g_pGraph = nullptr;
    }
}

int main()
{
    // Initialize COM
    HRESULT hr = CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED);
    if (FAILED(hr))
    {
        std::wcout << L"Failed to initialize COM" << std::endl;
        return 1;
    }

    std::wcout << L"Initializing UVC Camera Application..." << std::endl;

    // Create the capture graph
    hr = CreateCaptureGraph(&g_pGraph, &g_pCaptureFilter, &g_pRendererFilter);
    if (FAILED(hr))
    {
        std::wcout << L"Failed to create capture graph" << std::endl;
        CoUninitialize();
        return 1;
    }

    // Set camera resolution and FPS
    hr = SetCameraResolutionAndFPS(g_pCaptureFilter, 1280, 720, 120);
    if (FAILED(hr))
    {
        std::wcout << L"Warning: Failed to set camera resolution/FPS. Continuing with default settings..." << std::endl;
    }

    // Disable all automatic camera controls to prevent frame stuttering
    hr = DisableAllAutomaticControls(g_pCaptureFilter);
    if (FAILED(hr))
    {
        std::wcout << L"Warning: Failed to disable some automatic controls. Continuing..." << std::endl;
    }

    // Create Sample Grabber filter for frame timing measurement
    std::wcout << L"Creating Sample Grabber filter for frame timing..." << std::endl;

    // First, try to register qedit.dll if it exists
    if (SUCCEEDED(TryRegisterSampleGrabber()))
    {
        std::wcout << L"Successfully registered Sample Grabber filter" << std::endl;
    }

    hr = CoCreateInstance(CLSID_SampleGrabber, nullptr, CLSCTX_INPROC_SERVER,
        IID_PPV_ARGS(&g_pSampleGrabberFilter));

    if (FAILED(hr))
    {
        std::wcout << L"\nERROR: Failed to create Sample Grabber filter. Error: 0x" << std::hex << hr << std::endl;
        std::wcout << L"\nFrame timing measurement is REQUIRED for this application." << std::endl;
        std::wcout << L"\nTo fix this issue, please register qedit.dll:" << std::endl;
        std::wcout << L"1. Open Command Prompt as Administrator" << std::endl;
        std::wcout << L"2. Run: regsvr32 C:\\Windows\\System32\\qedit.dll" << std::endl;
        std::wcout << L"   (or regsvr32 C:\\Windows\\SysWOW64\\qedit.dll for 32-bit applications)" << std::endl;
        std::wcout << L"3. If qedit.dll is not found, you may need to copy it from another Windows system" << std::endl;
        std::wcout << L"   or install DirectShow components.\n" << std::endl;
        CleanupInterfaces();
        CoUninitialize();
        return 1;
    }
    // Add Sample Grabber to graph
    hr = g_pGraph->AddFilter(g_pSampleGrabberFilter, L"Sample Grabber");
    if (FAILED(hr))
    {
        std::wcout << L"ERROR: Failed to add Sample Grabber to graph. Error: 0x" << std::hex << hr << std::endl;
        std::wcout << L"Frame timing is REQUIRED. Cannot continue without it." << std::endl;
        g_pSampleGrabberFilter->Release();
        g_pSampleGrabberFilter = nullptr;
        CleanupInterfaces();
        CoUninitialize();
        return 1;
    }

    // Get ISampleGrabber interface
    hr = g_pSampleGrabberFilter->QueryInterface(IID_ISampleGrabber, (void**)&g_pSampleGrabber);
    if (FAILED(hr))
    {
        std::wcout << L"ERROR: Failed to get ISampleGrabber interface. Error: 0x" << std::hex << hr << std::endl;
        std::wcout << L"Frame timing is REQUIRED. Cannot continue without it." << std::endl;
        CleanupInterfaces();
        CoUninitialize();
        return 1;
    }

    // Create and configure frame timing callback
    g_pFrameCallback = new FrameTimingCallback();
    g_pFrameCallback->AddRef();

    // Don't set a specific media type - let it auto-negotiate from the capture filter
    // This ensures compatibility with whatever format the camera provides
    hr = g_pSampleGrabber->SetMediaType(nullptr);
    if (FAILED(hr))
    {
        std::wcout << L"Warning: Failed to set Sample Grabber media type. Error: 0x" << std::hex << hr << std::endl;
    }

    // Set callback mode (one-shot = false means continuous)
    hr = g_pSampleGrabber->SetOneShot(FALSE);
    if (FAILED(hr))
    {
        std::wcout << L"Warning: Failed to set Sample Grabber one-shot mode. Error: 0x" << std::hex << hr << std::endl;
    }

    hr = g_pSampleGrabber->SetBufferSamples(TRUE);
    if (FAILED(hr))
    {
        std::wcout << L"Warning: Failed to enable buffer samples. Error: 0x" << std::hex << hr << std::endl;
    }

    hr = g_pSampleGrabber->SetCallback(g_pFrameCallback, 1); // 1 = BufferCB callback
    if (FAILED(hr))
    {
        std::wcout << L"ERROR: Failed to set Sample Grabber callback. Error: 0x" << std::hex << hr << std::endl;
        std::wcout << L"Frame timing is REQUIRED. Cannot continue without it." << std::endl;
        CleanupInterfaces();
        CoUninitialize();
        return 1;
    }

    std::wcout << L"Frame timing callback configured successfully" << std::endl;

    // Render the video stream
    // Render: Capture -> Sample Grabber -> Renderer
    std::wcout << L"Connecting filters: Capture -> Sample Grabber -> Renderer..." << std::endl;

        // First render from capture to sample grabber
        hr = g_pCaptureGraphBuilder->RenderStream(
            &PIN_CATEGORY_CAPTURE,
            &MEDIATYPE_Video,
            g_pCaptureFilter,
            nullptr,
            g_pSampleGrabberFilter);

        if (FAILED(hr))
        {
            std::wcout << L"ERROR: Failed to connect capture to sample grabber. Error: 0x" << std::hex << hr << std::endl;
            std::wcout << L"Frame timing is REQUIRED. Cannot continue without it." << std::endl;
            CleanupInterfaces();
            CoUninitialize();
            return 1;
        }
        else
        {
            // Then render from sample grabber to renderer
            IPin* pSampleGrabberOutPin = nullptr;
            IPin* pRendererInPin = nullptr;

            // Find sample grabber output pin
            IEnumPins* pEnumPins = nullptr;
            hr = g_pSampleGrabberFilter->EnumPins(&pEnumPins);
            if (SUCCEEDED(hr))
            {
                IPin* pPin = nullptr;
                while (pEnumPins->Next(1, &pPin, nullptr) == S_OK)
                {
                    PIN_INFO pinInfo;
                    if (SUCCEEDED(pPin->QueryPinInfo(&pinInfo)))
                    {
                        if (pinInfo.dir == PINDIR_OUTPUT)
                        {
                            pSampleGrabberOutPin = pPin;
                            pSampleGrabberOutPin->AddRef();
                        }
                        if (pinInfo.pFilter)
                            pinInfo.pFilter->Release();
                    }
                    pPin->Release();
                }
                pEnumPins->Release();
            }

            // Find renderer input pin
            pEnumPins = nullptr;
            hr = g_pRendererFilter->EnumPins(&pEnumPins);
            if (SUCCEEDED(hr))
            {
                IPin* pPin = nullptr;
                while (pEnumPins->Next(1, &pPin, nullptr) == S_OK)
                {
                    PIN_INFO pinInfo;
                    if (SUCCEEDED(pPin->QueryPinInfo(&pinInfo)))
                    {
                        if (pinInfo.dir == PINDIR_INPUT)
                        {
                            pRendererInPin = pPin;
                            pRendererInPin->AddRef();
                        }
                        if (pinInfo.pFilter)
                            pinInfo.pFilter->Release();
                    }
                    pPin->Release();
                }
                pEnumPins->Release();
            }

            // Connect sample grabber to renderer
            if (pSampleGrabberOutPin && pRendererInPin)
            {
                hr = g_pGraph->Connect(pSampleGrabberOutPin, pRendererInPin);
                if (FAILED(hr))
                {
                    std::wcout << L"ERROR: Failed to connect sample grabber to renderer. Error: 0x" << std::hex << hr << std::endl;
                    std::wcout << L"Frame timing is REQUIRED. Cannot continue without it." << std::endl;
                    if (pSampleGrabberOutPin) pSampleGrabberOutPin->Release();
                    if (pRendererInPin) pRendererInPin->Release();
                    CleanupInterfaces();
                    CoUninitialize();
                    return 1;
                }
                else
                {
                    std::wcout << L"Successfully connected Sample Grabber to Renderer" << std::endl;
                    std::wcout << L"Frame timing measurement enabled." << std::endl;
                }

                if (pSampleGrabberOutPin) pSampleGrabberOutPin->Release();
                if (pRendererInPin) pRendererInPin->Release();
            }
            else
            {
                std::wcout << L"ERROR: Failed to find pins for connection" << std::endl;
                std::wcout << L"Frame timing is REQUIRED. Cannot continue without it." << std::endl;
                if (pSampleGrabberOutPin) pSampleGrabberOutPin->Release();
                if (pRendererInPin) pRendererInPin->Release();
                CleanupInterfaces();
                CoUninitialize();
                return 1;
            }
        }

    // Get video window interface
    hr = g_pGraph->QueryInterface(IID_PPV_ARGS(&g_pVideoWindow));
    if (SUCCEEDED(hr))
    {
        // Set window properties - DirectShow will create its own window
        g_pVideoWindow->put_AutoShow(OATRUE);
        g_pVideoWindow->put_WindowStyle(WS_OVERLAPPEDWINDOW);

        // Set window position and size
        int width = 1280;
        int height = 720;

        // Center the window on screen
        int screenWidth = GetSystemMetrics(SM_CXSCREEN);
        int screenHeight = GetSystemMetrics(SM_CYSCREEN);
        int x = (screenWidth - width) / 2;
        int y = (screenHeight - height) / 2;

        g_pVideoWindow->SetWindowPosition(x, y, width, height);
        BSTR caption = SysAllocString(L"UVC Camera - 1280x720 @ 120 FPS");
        g_pVideoWindow->put_Caption(caption);
        SysFreeString(caption);
    }

    // Get media control interface
    hr = g_pGraph->QueryInterface(IID_PPV_ARGS(&g_pMediaControl));
    if (FAILED(hr))
    {
        std::wcout << L"Failed to get media control interface" << std::endl;
        CleanupInterfaces();
        CoUninitialize();
        return 1;
    }

    // Configure graph to reduce buffering and latency
    // Set reference clock to NULL to disable synchronization and reduce buffering
    IMediaFilter* pMediaFilter = nullptr;
    hr = g_pGraph->QueryInterface(IID_PPV_ARGS(&pMediaFilter));
    if (SUCCEEDED(hr))
    {
        pMediaFilter->SetSyncSource(nullptr);
        pMediaFilter->Release();
    }

    // Run the graph
    hr = g_pMediaControl->Run();
    if (FAILED(hr))
    {
        std::wcout << L"Failed to start video capture" << std::endl;
        CleanupInterfaces();
        CoUninitialize();
        return 1;
    }

    // Record the stream start time
    if (g_pFrameCallback)
    {
        g_pFrameCallback->SetStreamStartTime();
    }

    // Set camera exposure to -7
    hr = SetCameraExposure(g_pCaptureFilter, -7);
    if (FAILED(hr))
    {
        std::wcout << L"Warning: Failed to set camera exposure. Continuing..." << std::endl;
    }

    std::wcout << L"Camera streaming started. Press any key in console or close the window to exit..." << std::endl;

    // Get the window handle for message processing
    // IVideoWindow doesn't have get_Handle(), so we find the window by its caption
    HWND hwnd = nullptr;
    if (g_pVideoWindow)
    {
        // Find the video window by its caption
        hwnd = FindWindowW(nullptr, L"UVC Camera - 1280x720 @ 120 FPS");
    }

    // Message loop to handle window messages (prevents window from becoming unresponsive)
    // Optimized for high-frequency frame updates - process messages as fast as possible
    bool running = true;
    MSG msg;

    // High-priority message loop for smooth frame updates
    // Process messages immediately without waiting to minimize frame delays
    while (running)
    {
        // Process all pending Windows messages first (no timeout, no wait)
        // This ensures window updates happen immediately
        while (PeekMessage(&msg, nullptr, 0, 0, PM_REMOVE))
        {
            if (msg.message == WM_QUIT)
            {
                running = false;
                break;
            }

            // Handle window close
            if (msg.message == WM_CLOSE && msg.hwnd == hwnd)
            {
                running = false;
                break;
            }

            TranslateMessage(&msg);
            DispatchMessage(&msg);
        }

        // Check for console input (non-blocking)
        if (_kbhit())
        {
            _getwch();
            running = false;
            break;
        }

        // Check if statistics are ready to print (called from main thread to avoid blocking callback)
        // Update stats when a new frame is detected OR if 1 second has passed (mainly to update the "Time" log)
        if (g_pFrameCallback)
        {
            bool statsReady = g_pFrameCallback->AreStatsReady();
            bool timeUpdate = g_pFrameCallback->ShouldUpdateStatsByTime();
            
            if (statsReady || timeUpdate)
            {
                g_pFrameCallback->PrintStatistics(timeUpdate && !statsReady);
            }
        }

        // For high-frequency updates, use a very short timeout (0ms) to check for messages
        // This ensures we don't block and can process frames immediately
        // Only sleep if there are no messages to process
        DWORD result = MsgWaitForMultipleObjects(0, nullptr, FALSE, 0, QS_ALLINPUT);
        if (result == WAIT_TIMEOUT)
        {
            // No messages available, yield CPU briefly to avoid 100% CPU usage
            // But keep it very short (0ms) to minimize latency
            Sleep(0);
        }
        // If WAIT_OBJECT_0, messages are available, continue loop to process them
    }

    // Print final statistics before cleanup
    if (g_pFrameCallback)
    {
        std::wcout << L"\n=== Final Frame Timing Statistics ===" << std::endl;
        g_pFrameCallback->PrintStatistics();
    }

    // Cleanup
    CleanupInterfaces();
    CoUninitialize();

    std::wcout << L"Application terminated." << std::endl;
    return 0;
}

