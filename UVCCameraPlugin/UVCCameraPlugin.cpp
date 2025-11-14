#include "UVCCameraPlugin.h"
#include <opencv2/opencv.hpp>
#include <cstdio>
#include "opencv2/imgcodecs.hpp"
#include "opencv2/highgui.hpp"
#include "opencv2/imgproc.hpp"

using namespace cv;
using namespace std;

void* getCamera()
{
    auto cap = new cv::VideoCapture(0);

    return static_cast<void*>(cap);
}

double getCameraProperty(void* camera, int propertyID) 
{
    auto cap = static_cast<cv::VideoCapture*>(camera);
    return cap->get(propertyID);
}

double setCameraProperty(void* camera, int propertyID, double value) 
{
    auto cap = static_cast<cv::VideoCapture*>(camera);
    return cap->set(propertyID, value);
}

void releaseCamera(void* camera)
{
    auto cap = static_cast<cv::VideoCapture*>(camera);
    delete cap;
}

void getCameraTexture(
    void* camera,
    unsigned char* data,
    bool executeHT21,
    bool executeMedianBlur,
    double dp,
    double minDist,
    double param1,
    double param2,
    int minRadius,
    int maxRadius)
{
    auto cap = static_cast<cv::VideoCapture*>(camera);

    cv::Mat img;
    *cap >> img;
    Mat src = img;

    cv::Mat rgba;
    cv::cvtColor(src, rgba, cv::COLOR_BGR2RGBA);
    std::memcpy(data, rgba.data, rgba.total() * rgba.elemSize());
}
