@echo off
setlocal

:: Start in the directory containing this script
cd %~dp0

:: Ensure PortAudio has been cloned
if not exist src\external\portaudio\ (
    echo PortAudio source not found, did you forget to clone recursively? 1>&2
    exit /B 1
)

echo ==============================================================================
echo Building PortAudio...
echo ==============================================================================
call src\PortAudioNet.Native\build-native.cmd

echo ==============================================================================
echo Generating PortAudioNet...
echo ==============================================================================
dotnet run --configuration Release --project src\PortAudioNet.Generator -- "src/external/portaudio/" "artifacts/obj/PortAudioNet.Native/cmake/win-x64/Debug/portaudio.lib" "src/PortAudioNet/#Generated/"
