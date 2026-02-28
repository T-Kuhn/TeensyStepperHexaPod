#pragma once

#ifdef __cplusplus
extern "C" {
#endif

    // Open camera by device index (0 = first camera), configure resolution and FPS.
    // All automatic camera controls (auto-exposure, auto-white-balance, etc.) are
    // disabled internally. Returns an opaque handle, or nullptr on failure.
    __declspec(dllexport) void* openCamera(int deviceIndex, int width, int height, int fps);

    // Stop streaming, release all DirectShow resources, and free the handle.
    __declspec(dllexport) void releaseCamera(void* camera);

    // Fill caller-supplied buffer with the latest BGR frame (width * height * 3 bytes).
    // Blocks until a new frame arrives from the camera (5-second timeout).
    // Returns:
    //   1  = success
    //  -1  = null pointer(s)
    //  -2  = invalid dimensions (width or height <= 0)
    //  -3  = camera graph not running
    //  -4  = timeout -- no frame delivered within 5 seconds
    //  -5  = dimensions mismatch with opened resolution
    __declspec(dllexport) int getCameraTexture(void* camera, unsigned char* data, int width, int height);

    // Write the camera's opened width/height into *width and *height.
    // Returns 1 on success, 0 on failure.
    __declspec(dllexport) int getCameraDimensions(void* camera, int* width, int* height);

    // Set exposure time manually via IAMCameraControl (CameraControl_Flags_Manual).
    // Returns 1 on success, 0 on failure.
    __declspec(dllexport) int setCameraExposure(void* camera, long value);

    // Set analog gain manually via IAMVideoProcAmp (VideoProcAmp_Flags_Manual).
    // Returns 1 on success, 0 on failure.
    __declspec(dllexport) int setCameraGain(void* camera, long value);

    // Set contrast manually via IAMVideoProcAmp (VideoProcAmp_Flags_Manual).
    // Returns 1 on success, 0 on failure.
    __declspec(dllexport) int setCameraContrast(void* camera, long value);

#ifdef __cplusplus
}
#endif
