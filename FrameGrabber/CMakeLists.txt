cmake_minimum_required(VERSION 3.10)
project(UVCCameraApp)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Windows-specific settings
if(WIN32)
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} /EHsc")
    # For single-configuration generators (NMake, Ninja)
    if(NOT CMAKE_CONFIGURATION_TYPES)
        if(NOT CMAKE_BUILD_TYPE)
            set(CMAKE_BUILD_TYPE Release CACHE STRING "Build type" FORCE)
        endif()
    endif()
    
    # Force static runtime linking for standalone executable
    # This makes the exe work on any Windows machine without requiring VC++ Redistributable
    set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
endif()

# Source files
set(SOURCES
    src/main.cpp
)

# Create executable
add_executable(${PROJECT_NAME} ${SOURCES})

# Set static runtime linking on the target (for standalone executable)
if(WIN32)
    set_property(TARGET ${PROJECT_NAME} PROPERTY MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
endif()

# Link DirectShow libraries
if(WIN32)
    target_link_libraries(${PROJECT_NAME}
        strmiids
        ole32
        oleaut32
    )
endif()

# Set output directory
if(CMAKE_CONFIGURATION_TYPES)
    # Multi-configuration generator (Visual Studio)
    set_target_properties(${PROJECT_NAME} PROPERTIES
        RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin
    )
else()
    # Single-configuration generator (NMake, Ninja)
    set_target_properties(${PROJECT_NAME} PROPERTIES
        RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin
    )
endif()

