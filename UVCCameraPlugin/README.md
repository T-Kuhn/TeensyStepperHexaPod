## Build Requirements

To build this plugin, you need the following:

### Required Software

> **Note:** We are using Visual Studio 2022 (MSVC Tools v14.37) and OpenCV 4.12.0 (opencv_world4120.lib). While other versions may work, these specific versions are tested and verified.

1. **Microsoft Visual Studio Build Tools 2022** (or Visual Studio 2022)
   - The build script looks for Visual Studio 2022 in the following locations:
     - Community edition: `C:\Program Files\Microsoft Visual Studio\2022\Community\`
     - Professional edition: `C:\Program Files\Microsoft Visual Studio\2022\Professional\`
     - Build Tools: `C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\`
   - The script requires `vcvars64.bat` to initialize the MSVC environment

2. **OpenCV 4.x**
   - Download from: https://opencv.org/releases/
   - The build script expects OpenCV to be installed at: `C:\Work\OpenCV\opencv`
   - Required structure:
     - Include files: `opencv\build\include\`
     - Libraries: `opencv\build\x64\vc16\lib\`
     - Binaries: `opencv\build\x64\vc16\bin\`
   - **Note:** If your OpenCV is installed in a different location, update the `OPENCV_DIR` variable in `build.bat`

### Build Configuration

- **Architecture:** x64
- **Configuration:** Release
- **C++ Standard:** C++17
- **Target:** Unity 2021 (x64)

## How To Build Plugin DLL
1. cd into UVCCameraPlugin directory
2. ctrl + shift + B
3. build.bat should execute with below logs
```
Starting build script...
Current directory: X:\git\TeensyStepperHexaPod\UVCCameraPlugin
Checking for MSVC Build Tools...
Checking Community edition...
Found: Community edition
========================================
Building UVCCameraPlugin for Unity 2021
Architecture: x64
Configuration: Release
========================================

Initializing MSVC Build Tools environment...
**********************************************************************
** Visual Studio 2022 Developer Command Prompt v17.9.6
** Copyright (c) 2022 Microsoft Corporation
**********************************************************************
[vcvarsall.bat] Environment initialized for: 'x64'
Using opencv_world library: opencv_world4120.lib
Compiling UVCCameraPlugin.cpp...
UVCCameraPlugin.cpp
C:\Work\OpenCV\opencv\build\include\opencv2/calib3d.hpp(678): warning C4819: ファイルは、現在のコード ページ (932) で表示できない文字を含んでいます。データの損失を防ぐために、ファイルを Unicode 形式で保存してください。
C:\Work\OpenCV\opencv\build\include\opencv2/imgcodecs.hpp(1): warning C4819: ファイルは、現在のコード ページ (932) で表示できない文字を含んでいます。データの損失を防ぐために、ファイルを Unicode 形式で保存してください。
Linking UVCCameraPlugin.dll...
   ライブラリ bin\UVCCameraPlugin.lib とオブジェクト bin\UVCCameraPlugin.exp を作成中
Copying OpenCV runtime DLLs...

========================================
Build completed successfully
========================================
Output: bin\UVCCameraPlugin.dll
```