#pragma once

#ifdef __cplusplus
extern "C" {
#endif

    // Returns camera handle, or nullptr on failure
    __declspec(dllexport) void* getCamera(int id);

    // Returns -1.0 on error, property value otherwise
    __declspec(dllexport) double getCameraProperty(void* camera, int propertyID);

    // Returns 0 on failure, 1 on success
    __declspec(dllexport) int setCameraProperty(void* camera, int propertyID, double value);

    // Safely releases camera (handles nullptr)
    __declspec(dllexport) void releaseCamera(void* camera);

    // Returns 1 on success, negative values on failure:
    //   -1: null pointer(s)
    //   -2: invalid dimensions (width/height <= 0)
    //   -3: camera not opened
    //   -4: frame read failed
    //   -5: width/height mismatch
    //   -6: data size mismatch
    // data must be pre-allocated with size = width * height * 4 (RGBA)
    __declspec(dllexport) int getCameraTexture(void* camera, unsigned char* data, int width, int height);

    // Get camera frame dimensions (returns 1 on success, 0 on failure)
    __declspec(dllexport) int getCameraDimensions(void* camera, int* width, int* height);

#ifdef __cplusplus
}
#endif