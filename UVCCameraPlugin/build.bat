@echo off
REM Build script for UVCCameraPlugin DLL for Unity (x64)
REM Requires MSVC Build Tools (no OpenCV -- uses Windows DirectShow)

echo Starting build script...
echo Current directory: %CD%

setlocal enabledelayedexpansion

REM Configuration
set PLUGIN_NAME=UVCCameraPlugin
set OUTPUT_DIR=bin
set ARCH=x64
set CONFIG=Release

REM MSVC Configuration
set MSVC_VERSION=2022

echo Checking for MSVC Build Tools...
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

REM Create output directory
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

echo ========================================
echo Building %PLUGIN_NAME% for Unity
echo Architecture: %ARCH%
echo Configuration: %CONFIG%
echo Backend: DirectShow (Windows SDK -- no OpenCV)
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
set COMPILER_FLAGS=%COMPILER_FLAGS% /std:c++17

REM Linker flags
REM DirectShow libs are part of the Windows SDK -- no extra paths needed.
set LINKER_FLAGS=/DLL /MACHINE:X64 /NOLOGO /OPT:REF /OPT:ICF
set LINKER_FLAGS=%LINKER_FLAGS% /OUT:"%OUTPUT_DIR%\%PLUGIN_NAME%.dll"
set LINKER_FLAGS=%LINKER_FLAGS% strmiids.lib ole32.lib oleaut32.lib quartz.lib

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
    exit /b 1
)

echo.
echo ========================================
echo Build completed successfully!
echo ========================================
echo Output: %OUTPUT_DIR%\%PLUGIN_NAME%.dll
echo.
echo To use in Unity:
echo 1. Copy %PLUGIN_NAME%.dll to your Unity project's Assets\Plugins\x86_64\ folder
echo    (No extra DLLs needed -- DirectShow is built into Windows)
echo 2. In Unity, select the DLL and ensure:
echo    - Platform: Windows
echo    - Architecture: x86_64
echo.

endlocal
