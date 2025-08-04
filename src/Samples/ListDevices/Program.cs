using PortAudioNet;
using System;
using System.Runtime.InteropServices;

Console.WriteLine($"Hello, {RuntimeInformation.FrameworkDescription}!");

PortAudio.Initialize().ThrowIfFailure();

unsafe
{
    PaVersionInfo* versionInfo = PortAudio.GetVersionInfo();
    Console.WriteLine($"Initialized PortAudio version '{PortAudio.PtrToString(versionInfo->versionText)}'");

    int deviceCount = PortAudio.GetDeviceCount();
    PortAudio.CheckReturn(deviceCount);
    int hostApiCount = PortAudio.GetHostApiCount();
    PortAudio.CheckReturn(hostApiCount);

    Console.WriteLine();
    Console.WriteLine($"Found {deviceCount} devices across {hostApiCount} host APIs...");

    for (int hostIndex = 0; hostIndex < hostApiCount; hostIndex++)
    {
        PaHostApiInfo* hostApi = PortAudio.GetHostApiInfo(hostIndex);
        Console.WriteLine();
        Console.WriteLine($"===== {PortAudio.PtrToString(hostApi->name)} =====");

        for (int hostDeviceIndex = 0; hostDeviceIndex < hostApi->deviceCount; hostDeviceIndex++)
        {
            int deviceIndex = PortAudio.HostApiDeviceIndexToDeviceIndex(hostIndex, hostDeviceIndex);
            PortAudio.CheckReturn(deviceIndex);
            PaDeviceInfo* device = PortAudio.GetDeviceInfo(deviceIndex);
            Console.WriteLine($"[{deviceIndex}] '{PortAudio.PtrToString(device->name)}' (Max channels: {device->maxInputChannels}/{device->maxOutputChannels})");
        }
    }
}

PortAudio.Terminate().ThrowIfFailure();
