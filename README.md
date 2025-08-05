PortAudio.NET
=======================================================================================================================

[![MIT Licensed](https://img.shields.io/github/license/bonsai-rx/portaudio.net?style=flat-square)](LICENSE.md)
[![CI Status](https://img.shields.io/github/actions/workflow/status/bonsai-rx/portaudio.net/PortAudioNet.yml?branch=main&style=flat-square&label=CI)](https://github.com/bonsai-rx/portaudio.net/actions/workflows/PortAudioNet.yml?query=branch%3Amain)
[![NuGet Version](https://img.shields.io/nuget/v/PortAudioNet?style=flat-square)](https://www.nuget.org/packages/PortAudioNet/)

Low-level C# bindings for [PortAudio](https://www.portaudio.com/).

Currently only Windows x64 is well-tested, although Linux x64 is expected to work if you regenerate the bindings.

Most of PortAudio's host API-specific funcionality is not yet exposed with the exception of WASAPI.

## License

`PortAudio.NET` is released as open source under the [MIT license](LICENSE.md).

Additionally, this project integrates and makes use of third-party dependencies, subject to separate license agreements. [See the third-party notice listing for details](THIRD-PARTY-NOTICES.md).

## Building

### Windows Prerequisites

Windows 10 22H2 x64 or later is recommended.

Tool | Tested Version
-----|--------------------
[Visual Studio](https://visualstudio.microsoft.com/vs/) | 2022 (17.11.5)
[.NET 8.0 SDK](http://dot.net/) | 8.0.403
[CMake](https://cmake.org/) | 3.30.2

Visual Studio must have the "Desktop development with C++" workload installed.

### Linux Prerequisites

Ubuntu 24.04 Noble x64 is tested, but most distros are expected to work.

Package | Tested Version
--------|--------------------
`build-essential` | 12.10
`cmake` | 3.28.3
`dotnet-sdk-8.0` | 8.0.110

Ubuntu 22.10 and later currently require manually installing `libtinfo5`, see [this issue](https://github.com/MochiLibraries/Biohazrd/issues/248) for details.

### Building PortAudio and generating the bindings

1. Ensure Git submodules are up-to-date with `git submodule update --init --recursive`
2. Build and run `generate.cmd` (Windows) or `generate.sh` (Linux) from the repository root

### Building and running the samples

Simply build+run any of the samples as you would any other .NET project. (IE: Using F5 in Visual Studio or `dotnet run --project src/Samples/ListDevices --framework net8.0`.)

The native PortAudio code will be built automatically if you didn't build it yourself.
