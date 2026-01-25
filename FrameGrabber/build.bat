@echo off
setlocal

echo Building UVC Camera Application with MSVC Build Tools...

REM Find and setup MSVC Build Tools environment
set VCVARSALL=

REM Check for Visual Studio 2022 Build Tools
if exist "C:\Program Files\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvarsall.bat" (
    call "C:\Program Files\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvarsall.bat" x64
    if not errorlevel 1 goto :found
)
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvarsall.bat" (
    call "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvarsall.bat" x64
    if not errorlevel 1 goto :found
)
if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvarsall.bat" (
    call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvarsall.bat" x64
    if not errorlevel 1 goto :found
)
if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvarsall.bat" (
    call "C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvarsall.bat" x64
    if not errorlevel 1 goto :found
)
if exist "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\VC\Auxiliary\Build\vcvarsall.bat" (
    call "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\VC\Auxiliary\Build\vcvarsall.bat" x64
    if not errorlevel 1 goto :found
)
if exist "C:\Program Files\Microsoft Visual Studio\2019\BuildTools\VC\Auxiliary\Build\vcvarsall.bat" (
    call "C:\Program Files\Microsoft Visual Studio\2019\BuildTools\VC\Auxiliary\Build\vcvarsall.bat" x64
    if not errorlevel 1 goto :found
)
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\VC\Auxiliary\Build\vcvarsall.bat" (
    call "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\VC\Auxiliary\Build\vcvarsall.bat" x64
    if not errorlevel 1 goto :found
)
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvarsall.bat" (
    call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvarsall.bat" x64
    if not errorlevel 1 goto :found
)

echo ERROR: MSVC Build Tools or Visual Studio not found!
echo Please install Microsoft Visual Studio Build Tools or Visual Studio with C++ workload.
echo Expected locations:
echo   - C:\Program Files\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvarsall.bat
echo   - C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvarsall.bat
echo   - C:\Program Files\Microsoft Visual Studio\2019\BuildTools\VC\Auxiliary\Build\vcvarsall.bat
exit /b 1

:found
echo MSVC Build Tools environment setup complete.

REM Check if build directory exists, if not create it
if not exist "build" (
    echo Creating build directory...
    mkdir build
)

cd build

REM Always reconfigure CMake to ensure latest settings (like static linking) are applied
echo Configuring CMake with MSVC (static runtime linking)...
cmake .. -G "NMake Makefiles" -DCMAKE_BUILD_TYPE=Release
if errorlevel 1 (
    echo CMake configuration failed!
    cd ..
    exit /b 1
)

REM Build the project
echo Building project with MSVC...
cmake --build .
if errorlevel 1 (
    echo Build failed!
    cd ..
    exit /b 1
)

cd ..

echo.
echo Build completed successfully!
echo Executable location: build\bin\UVCCameraApp.exe
echo.

endlocal

