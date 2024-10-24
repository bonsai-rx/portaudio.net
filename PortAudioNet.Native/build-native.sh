#!/bin/bash -e

# Start in the directory containing this script
cd `dirname "${BASH_SOURCE[0]}"`

SOURCE_FOLDER=../external/portaudio
PLATFORM_RID=`../tooling/determine-rid.sh`
BUILD_FOLDER_PREFIX=../artifacts/obj/PortAudioNet.Native/cmake/$PLATFORM_RID

# Generate and build each configuration
function build_configuration() {
    if [[ -z $1 ]]; then
        echo "Missing configuration type"
        exit 1
    fi

    echo "============================================================================="
    echo "Generating makefile for $1 configuration..."
    echo "============================================================================="
    cmake \
        -G "Unix Makefiles" \
        -S $SOURCE_FOLDER \
        -B $BUILD_FOLDER_PREFIX-$1 \
        -DPA_BUILD_SHARED_LIBS=ON \
        -DCMAKE_BUILD_TYPE=$1

    echo "============================================================================="
    echo "Building $1 configuration..."
    echo "============================================================================="
    make --directory=$BUILD_FOLDER_PREFIX-$1 -j`nproc`
}

build_configuration Debug
build_configuration Release
