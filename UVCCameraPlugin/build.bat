@echo off
REM Build script for UVCCameraPlugin DLL for Unity 2021 (x64)
REM Requires MSVC Build Tools and OpenCV

echo Starting build script...
echo Current directory: %CD%

setlocal enabledelayedexpansion

REM Configuration
set PLUGIN_NAME=UVCCameraPlugin
set OUTPUT_DIR=bin
set ARCH=x64
set CONFIG=Release

REM OpenCV Configuration - Update these paths to match your OpenCV installation
set OPENCV_DIR=C:\Work\OpenCV\opencv
set OPENCV_VERSION=4.8.0
set OPENCV_BUILD=x64\vc16

REM MSVC Configuration
set MSVC_VERSION=2022
set MSVC_TOOLS_VERSION=14.37

echo Checking for MSVC Build Tools...
REM Try to find MSVC Build Tools
set VCVARS_PATH=
echo Checking Community edition...
if exist "C:\Program Files\Microsoft Visual Studio\%MSVC_VERSION%\Community\VC\Auxiliary\Build\vcvars64.bat" (
    set VCVARS_PATH=C:\Program Files\Microsoft Visual Studio\%MSVC_VERSION%\Community\VC\Auxiliary\Build\vcvars64.bat
    echo Found: Community edition
    goto :vcvars_found
)
echo Checking BuildTools...
if exist "C:\Program Files (x86)\Microsoft Visual Studio\%MSVC_VERSION%\BuildTools\VC\Auxiliary\Build\vcvars64.bat" (
    set VCVARS_PATH=C:\Program Files (x86)\Microsoft Visual Studio\%MSVC_VERSION%\BuildTools\VC\Auxiliary\Build\vcvars64.bat
    echo Found: BuildTools
    goto :vcvars_found
)
echo Checking Professional edition...
if exist "C:\Program Files\Microsoft Visual Studio\%MSVC_VERSION%\Professional\VC\Auxiliary\Build\vcvars64.bat" (
    set VCVARS_PATH=C:\Program Files\Microsoft Visual Studio\%MSVC_VERSION%\Professional\VC\Auxiliary\Build\vcvars64.bat
    echo Found: Professional edition
    goto :vcvars_found
)
echo ERROR: Could not find MSVC Build Tools vcvars64.bat
echo Please install Visual Studio Build Tools or update the paths in this script.
exit /b 1
:vcvars_found

echo Checking OpenCV directory...
REM Check if OpenCV directory exists
if not exist "%OPENCV_DIR%" (
    echo WARNING: OpenCV directory not found at %OPENCV_DIR%
    echo Please update OPENCV_DIR in build.bat to point to your OpenCV installation.
    echo.
    echo You can download OpenCV from: https://opencv.org/releases/
    exit /b 1
)

REM Set OpenCV paths
set OPENCV_INCLUDE=%OPENCV_DIR%\build\include
set OPENCV_LIB=%OPENCV_DIR%\build\%OPENCV_BUILD%\lib
set OPENCV_BIN=%OPENCV_DIR%\build\%OPENCV_BUILD%\bin

if not exist "%OPENCV_INCLUDE%" (
    echo ERROR: OpenCV include directory not found at %OPENCV_INCLUDE%
    exit /b 1
)

if not exist "%OPENCV_LIB%" (
    echo ERROR: OpenCV lib directory not found at %OPENCV_LIB%
    exit /b 1
)

REM Create output directory
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

echo ========================================
echo Building %PLUGIN_NAME% for Unity 2021
echo Architecture: %ARCH%
echo Configuration: %CONFIG%
echo ========================================
echo.

REM Initialize MSVC environment
echo Initializing MSVC Build Tools environment...
call "%VCVARS_PATH%"
if errorlevel 1 (
    echo ERROR: Failed to initialize MSVC environment
    exit /b 1
)

REM Compiler flags
set COMPILER_FLAGS=/c /EHsc /MD /O2 /W3 /nologo /Zi /DNDEBUG /D_CRT_SECURE_NO_WARNINGS
set COMPILER_FLAGS=%COMPILER_FLAGS% /I"%OPENCV_INCLUDE%"
set COMPILER_FLAGS=%COMPILER_FLAGS% /std:c++17

REM Linker flags
set LINKER_FLAGS=/DLL /MACHINE:X64 /NOLOGO /OPT:REF /OPT:ICF
set LINKER_FLAGS=%LINKER_FLAGS% /LIBPATH:"%OPENCV_LIB%"
set LINKER_FLAGS=%LINKER_FLAGS% /OUT:"%OUTPUT_DIR%\%PLUGIN_NAME%.dll"

REM Check for opencv_world library and find actual filename
set OPENCV_LIBS=
for %%f in ("%OPENCV_LIB%\opencv_world*.lib") do (
    set OPENCV_LIBS=%%~nxf
    echo Using opencv_world library: %%~nxf
    goto :lib_found
)
REM If no world library, use individual libraries
echo Using individual OpenCV libraries...
for %%f in ("%OPENCV_LIB%\opencv_core*.lib") do set OPENCV_LIBS=!OPENCV_LIBS! %%~nxf
for %%f in ("%OPENCV_LIB%\opencv_imgproc*.lib") do set OPENCV_LIBS=!OPENCV_LIBS! %%~nxf
for %%f in ("%OPENCV_LIB%\opencv_imgcodecs*.lib") do set OPENCV_LIBS=!OPENCV_LIBS! %%~nxf
for %%f in ("%OPENCV_LIB%\opencv_highgui*.lib") do set OPENCV_LIBS=!OPENCV_LIBS! %%~nxf
for %%f in ("%OPENCV_LIB%\opencv_videoio*.lib") do set OPENCV_LIBS=!OPENCV_LIBS! %%~nxf
:lib_found
set LINKER_FLAGS=%LINKER_FLAGS% %OPENCV_LIBS%

REM Compile
echo Compiling %PLUGIN_NAME%.cpp...
cl.exe %COMPILER_FLAGS% UVCCameraPlugin.cpp /Fo:%OUTPUT_DIR%\UVCCameraPlugin.obj
if errorlevel 1 (
    echo ERROR: Compilation failed
    exit /b 1
)

REM Link
echo Linking %PLUGIN_NAME%.dll...
link.exe %LINKER_FLAGS% %OUTPUT_DIR%\UVCCameraPlugin.obj
if errorlevel 1 (
    echo ERROR: Linking failed
    echo.
    echo Make sure OpenCV libraries are available in %OPENCV_LIB%
    echo Common library names: opencv_world*.lib or opencv_core*.lib, opencv_imgproc*.lib, etc.
    exit /b 1
)

REM Copy OpenCV DLLs to output directory (required at runtime)
echo Copying OpenCV runtime DLLs...
dir /b "%OPENCV_BIN%\opencv_world*.dll" >nul 2>&1
if not errorlevel 1 (
    copy /Y "%OPENCV_BIN%\opencv_world*.dll" "%OUTPUT_DIR%\" >nul 2>&1
) else (
    copy /Y "%OPENCV_BIN%\opencv_core*.dll" "%OUTPUT_DIR%\" >nul 2>&1
    copy /Y "%OPENCV_BIN%\opencv_imgproc*.dll" "%OUTPUT_DIR%\" >nul 2>&1
    copy /Y "%OPENCV_BIN%\opencv_imgcodecs*.dll" "%OUTPUT_DIR%\" >nul 2>&1
    copy /Y "%OPENCV_BIN%\opencv_highgui*.dll" "%OUTPUT_DIR%\" >nul 2>&1
    copy /Y "%OPENCV_BIN%\opencv_videoio*.dll" "%OUTPUT_DIR%\" >nul 2>&1
)

echo.
echo ========================================
echo Build completed successfully!
echo ========================================
echo Output: %OUTPUT_DIR%\%PLUGIN_NAME%.dll
echo.
echo To use in Unity:
echo 1. Copy %PLUGIN_NAME%.dll to your Unity project's Assets\Plugins\x86_64\ folder
echo 2. Copy all OpenCV DLLs from %OUTPUT_DIR%\ to the same folder
echo 3. In Unity, select the DLL and ensure:
echo    - Platform: Windows
echo    - Architecture: x86_64
echo    - Don't Process: unchecked (or checked if you want to handle it manually)
echo.

endlocal

