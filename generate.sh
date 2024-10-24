#!/bin/bash -Eeu

# Start in the directory containing this script
cd `dirname "${BASH_SOURCE[0]}"`

# Ensure PortAudio has been cloned
if [[ ! -d external/portaudio/ ]]; then
    echo PortAudio source not found, did you forget to clone recursively? 1>&2
    exit 1
fi

echo ==============================================================================
echo Building PortAudio...
echo ==============================================================================
./PortAudioNet.Native/build-native.sh

echo ==============================================================================
echo Generating PortAudioNet...
echo ==============================================================================
dotnet run --configuration Release --project PortAudioNet.Generator -- "external/portaudio/" "artifacts/obj/PortAudioNet.Native/cmake/linux-x64-Debug/libportaudio.so" "PortAudioNet/#Generated/"
