using PortAudioNet;
using SineWave;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

unsafe
{
    Console.WriteLine($"Hello, {RuntimeInformation.FrameworkDescription}!");

    PortAudio.Initialize().ThrowIfFailure();
    PaVersionInfo* versionInfo = PortAudio.GetVersionInfo();
    Console.WriteLine($"Initialized PortAudio version '{PortAudio.PtrToString(versionInfo->versionText)}'");

    //=============================================================================================================
    // Device selection
    //=============================================================================================================
    PaDeviceInfo* selectedDevice;
    int selectedDeviceIndex;
    {
        int deviceCount = PortAudio.GetDeviceCount();
        PortAudio.CheckReturn(deviceCount);
        if (deviceCount == 0)
        {
            Console.Error.WriteLine("No audio output devices found.");
            return 1;
        }

        int hostApiCount = PortAudio.GetHostApiCount();
        PortAudio.CheckReturn(hostApiCount);

        int defaultDevice = PortAudio.GetDefaultOutputDevice();

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
                if (device->maxOutputChannels <= 0)
                    continue;

                Console.WriteLine($"[{deviceIndex}] {device->maxOutputChannels} channel '{PortAudio.PtrToString(device->name)}'{(deviceIndex == defaultDevice ? " (Default output)" : "")}");
            }
        }

        Console.WriteLine();
        while (true)
        {
            Console.WriteLine("Enter a device number or blank for default: ");

            string? selection = Console.ReadLine();
            if (selection is null or "")
            {
                if (defaultDevice == PortAudio.NoDevice)
                {
                    Console.Error.WriteLine("No default output device was found.");
                    return 1;
                }

                selectedDevice = PortAudio.GetDeviceInfo(selectedDeviceIndex = defaultDevice);
                Debug.Assert(selectedDevice != null);
                break;
            }

            if (!int.TryParse(selection, out int selectedIndex) || selectedIndex < 0 || selectedIndex > deviceCount)
            {
                Console.Error.WriteLine("Please enter a valid index.");
                continue;
            }

            selectedDevice = PortAudio.GetDeviceInfo(selectedDeviceIndex = selectedIndex);
            if (selectedDevice->maxOutputChannels <= 0)
            {
                Console.Error.WriteLine("The specified device is not an output.");
                continue;
            }

            Debug.Assert(selectedDevice != null);
            break;
        }
    }

    //=============================================================================================================
    // Play sine wave tone sweeping across all channels of the device
    //=============================================================================================================
    PaStreamParameters parameters = new()
    {
        device = selectedDeviceIndex,
        channelCount = selectedDevice->maxOutputChannels,
        sampleFormat = SineWaveGenerator.SampleFormat,
        suggestedLatency = selectedDevice->defaultLowOutputLatency,
        hostApiSpecificStreamInfo = null,
    };

    const double sampleRate = 96_000.0;

    PaStream* stream;
    using SineWaveGenerator generator = new(sampleRate, parameters.channelCount);

    PortAudio.OpenStream
    (
        &stream,
        null,
        &parameters,
        sampleRate,
        PortAudio.FramesPerBufferUnspecified,
        PaStreamFlags.ClipOff,
        SineWaveGenerator.StreamCallback,
        generator.UserData
    ).ThrowIfFailure();

    Console.WriteLine($"Starting stream...");
    generator.BeforeStart(stream);
    PortAudio.StartStream(stream).ThrowIfFailure();

    Console.WriteLine("Press escape to terminate");
    while (Console.ReadKey(intercept: true).Key != ConsoleKey.Escape)
    { }

    Console.WriteLine("Ending stream.");
    PortAudio.StopStream(stream).ThrowIfFailure();

    Console.WriteLine("Goodbye.");
    PortAudio.Terminate().ThrowIfFailure();
    return 0;
}
