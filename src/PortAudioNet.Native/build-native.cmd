@echo off
setlocal enabledelayedexpansion

:: Start in the directory containing this script
cd %~dp0

set SOURCE_FOLDER=..\external\portaudio

:: Determine platform RID and build folder
call ..\..\build\determine-rid.cmd || exit /B !ERRORLEVEL!
set BUILD_FOLDER=..\..\artifacts\obj\PortAudioNet.Native\cmake\%PLATFORM_RID%

:: We don't actually use the `install` target (it doesn't support multiple configurations),
:: but we want to avoid PortAudio's `uninstall` target from messing with the system PortAudio (if it exists)
set INSTALL_FOLDER=..\..\artifacts\bin\PortAudioNet.Native\cmake\%PLATFORM_RID%

:: Ensure build folder is protected from Directory.Build.* influences
if not exist %BUILD_FOLDER% (
    mkdir %BUILD_FOLDER%
    echo ^<Project^>^</Project^> > %BUILD_FOLDER%/Directory.Build.props
    echo ^<Project^>^</Project^> > %BUILD_FOLDER%/Directory.Build.targets
    echo # > %BUILD_FOLDER%/Directory.Build.rsp
)

:: (Re)generate the Visual Studio solution and build in all configurations
:: We don't specify a generator specifically so that CMake will default to the latest installed version of Visual Studio
:: https://github.com/Kitware/CMake/blob/0c038689be424ca71a6699a993adde3bcaa15b6c/Source/cmake.cxx#L2213-L2214
cmake ^
    -S %SOURCE_FOLDER% ^
    -B %BUILD_FOLDER% ^
    -DPA_BUILD_SHARED_LIBS=ON ^
    -DCMAKE_INSTALL_PREFIX=%INSTALL_FOLDER% ^
    -DCMAKE_SKIP_INSTALL_RULES=ON ^
    -DCMAKE_SHARED_LINKER_FLAGS="/ignore:4197" ^
    || exit /B 1
echo ==============================================================================
echo Building PortAudioNet.Native %PLATFORM_RID% debug build...
echo ==============================================================================
cmake --build %BUILD_FOLDER% --config Debug || exit /B 1
echo ==============================================================================
echo Building PortAudioNet.Native %PLATFORM_RID% release build...
echo ==============================================================================
cmake --build %BUILD_FOLDER% --config Release || exit /B 1
