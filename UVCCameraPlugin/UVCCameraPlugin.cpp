#include "UVCCameraPlugin.h"
#include <opencv2/opencv.hpp>
#include <cstring>

void* getCamera(int id)
{
    // Try DirectShow backend first (better for UVC cameras on Windows)
    cv::VideoCapture* cap = new cv::VideoCapture(id, cv::CAP_DSHOW);
    
    // If DirectShow fails, try default backend
    if (!cap->isOpened()) {
        delete cap;
        cap = new cv::VideoCapture(id);
    }

    // Verify camera opened successfully
    if (!cap->isOpened()) {
        delete cap;
        return nullptr;
    }

    return static_cast<void*>(cap);
}

double getCameraProperty(void* camera, int propertyID)
{
    if (camera == nullptr) {
        return -1.0;
    }

    cv::VideoCapture* cap = static_cast<cv::VideoCapture*>(camera);

    if (!cap->isOpened()) {
        return -1.0;
    }

    return cap->get(propertyID);
}

int setCameraProperty(void* camera, int propertyID, double value)
{
    if (camera == nullptr) {
        return 0;
    }

    cv::VideoCapture* cap = static_cast<cv::VideoCapture*>(camera);

    if (!cap->isOpened()) {
        return 0;
    }

    return cap->set(propertyID, value) ? 1 : 0;
}

void releaseCamera(void* camera)
{
    if (camera == nullptr) {
        return;
    }

    cv::VideoCapture* cap = static_cast<cv::VideoCapture*>(camera);
    cap->release();
    delete cap;
}

int getCameraDimensions(void* camera, int* width, int* height)
{
    if (camera == nullptr || width == nullptr || height == nullptr) {
        return 0;
    }

    cv::VideoCapture* cap = static_cast<cv::VideoCapture*>(camera);

    if (!cap->isOpened()) {
        return 0;
    }

    *width = static_cast<int>(cap->get(cv::CAP_PROP_FRAME_WIDTH));
    *height = static_cast<int>(cap->get(cv::CAP_PROP_FRAME_HEIGHT));

    return (*width > 0 && *height > 0) ? 1 : 0;
}

int getCameraTexture(void* camera, unsigned char* data, int width, int height)
{
    if (camera == nullptr || data == nullptr) {
        return -1; // Null pointer(s)
    }

    if (width <= 0 || height <= 0) {
        return -2; // Invalid dimensions
    }

    cv::VideoCapture* cap = static_cast<cv::VideoCapture*>(camera);

    if (!cap->isOpened()) {
        return -3; // Camera not opened
    }

    // Read a frame
    cv::Mat img;
    bool success = cap->read(img);
    if (!success || img.empty()) {
        return -4; // Frame read failed
    }

    if (img.cols != width || img.rows != height) {
        return -5; // width/height mismatch
    }

    cv::Mat rgba;
    cv::cvtColor(img, rgba, cv::COLOR_BGR2RGBA);

    // Validate buffer size before copying
    size_t expectedSize = width * height * 4; // RGBA = 4 bytes per pixel
    size_t actualSize = rgba.total() * rgba.elemSize();

    if (actualSize != expectedSize) {
        return -6; // data size mismatch
    }

    std::memcpy(data, rgba.data, actualSize);
    return 1; // Success
}
