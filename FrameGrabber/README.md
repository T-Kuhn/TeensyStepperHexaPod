# UVC Camera High FPS DirectShow Application

A standalone C++ application that connects to a UVC-compatible USB camera and streams video at 1280x720 resolution and 120 FPS.

## Features

- Automatically detects and connects to the first available UVC camera
- Configures camera to 1280x720 resolution
- Sets frame rate to 120 FPS
- Displays video stream in a window

## Requirements

- Windows 10/11
- Microsoft Visual Studio Build Tools 2019 or later (with C++ build tools workload)
- CMake 3.10 or later
- UVC-compatible USB camera

## Building

### Using build.bat (Recommended)

Simply run the build script:
```
build.bat
```

Or press **Ctrl+Shift+B** in VS Code to run the build task.

The script will:
- Automatically detect and setup MSVC Build Tools environment
- Configure CMake with NMake Makefiles generator
- Build the project in Release mode

The executable will be in `build/bin/UVCCameraApp.exe`.

### Manual Build with CMake

1. Open a terminal in the project directory
2. Setup MSVC Build Tools environment:
   ```
   "C:\Program Files\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvarsall.bat" x64
   ```
3. Create a build directory:
   ```
   mkdir build
   cd build
   ```
4. Generate the project files:
   ```
   cmake .. -G "NMake Makefiles" -DCMAKE_BUILD_TYPE=Release
   ```
5. Build the project:
   ```
   cmake --build .
   ```

## Usage

1. Connect your UVC-compatible USB camera
2. Run the executable: `UVCCameraApp.exe`
3. The application will:
   - Find and connect to the first available camera
   - Configure it to 1280x720 @ 120 FPS
   - Display the video stream in a window
4. Press any key in the console to exit

## Notes

- If the camera doesn't support the exact resolution or frame rate, the application will attempt to set a custom format
- Some cameras may not support 120 FPS at 1280x720 - the application will try to set it but may fall back to the camera's maximum supported rate
- The video window can be resized and moved

## Troubleshooting

- **MSVC Build Tools not found**: Ensure Microsoft Visual Studio Build Tools is installed. The script looks for it in standard installation paths. If installed elsewhere, modify `build.bat` to point to your installation.
- **No camera found**: Ensure your camera is connected and recognized by Windows
- **Failed to set resolution/FPS**: Your camera may not support 1280x720 @ 120 FPS. Check your camera's specifications
- **Black screen**: The camera may be in use by another application - close other camera applications

