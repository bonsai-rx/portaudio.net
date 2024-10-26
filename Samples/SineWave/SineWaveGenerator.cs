using PortAudioNet;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SineWave;

internal unsafe sealed class SineWaveGenerator : IDisposable
{
    private GCHandle ThisHandle;
    public void* UserData => (void*)GCHandle.ToIntPtr(ThisHandle);

    private PaStream* Stream;

    public static PaSampleFormat SampleFormat => PaSampleFormat.Float32;

    private double Value;
    private double ValueIncrement;

    public double SampleRate { get; } // Hz
    private double _Frequency;
    public double Frequency // Hz
    {
        get => _Frequency;
        set
        {
            _Frequency = value;
            ValueIncrement = ((Math.PI * 2.0) * value) / SampleRate;
        }
    }

    private readonly int ChannelCount;
    private int CurrentChannel = 0;

    private double NextChannelTime;
    public double SecondsPerChannel { get; set; } = 0.5;

    public SineWaveGenerator(double sampleRate, int channelCount)
    {
        ThisHandle = GCHandle.Alloc(this);
        SampleRate = sampleRate;
        Frequency = 261.626;
        ChannelCount = channelCount;
    }

    public void BeforeStart(PaStream* stream)
    {
        if (Stream != null && Stream != stream)
            throw new InvalidOperationException("This generator is already bound to a different stream!");
        Stream = stream;

        double now = PortAudio.GetStreamTime(stream);
        Debug.Assert(now != 0.0);
        NextChannelTime = now + SecondsPerChannel;
        CurrentChannel = 0;
        Value = 0.0;
    }

    private PaStreamCallbackResult _StreamCallback(void* input, void* output, uint frameCount, in PaStreamCallbackTimeInfo timeInfo, PaStreamCallbackFlags statusFlags)
    {
        float* outputBuffer = (float*)output;

        if (timeInfo.outputBufferDacTime > NextChannelTime)
        {
            CurrentChannel = (CurrentChannel + 1) % ChannelCount;
            NextChannelTime = timeInfo.outputBufferDacTime + SecondsPerChannel;
        }

        for (int i = 0; i < frameCount; i++)
        {
            float sample = (float)Math.Sin(Value);

            Value += ValueIncrement;
            if (Value > Math.PI * 2.0)
                Value -= Math.PI * 2.0;

            for (int channelIndex = 0; channelIndex < ChannelCount; channelIndex++)
                *outputBuffer++ = channelIndex == CurrentChannel ? sample : 0f;
        }

        return PaStreamCallbackResult.Continue;
    }

#if NET5_0_OR_GREATER
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
#endif
    private static PaStreamCallbackResult _StreamCallback(void* input, void* output, uint frameCount, PaStreamCallbackTimeInfo* timeInfo, PaStreamCallbackFlags statusFlags, void* userData)
    {
        GCHandle handle = GCHandle.FromIntPtr((IntPtr)userData);
        SineWaveGenerator generator = ((SineWaveGenerator)handle.Target!);
        return generator._StreamCallback(input, output, frameCount, in *timeInfo, statusFlags);
    }

    // If you don't need downlevel .NET Framework support, just make _StreamCallback public and use it directly
#if NET5_0_OR_GREATER
    public static readonly delegate* unmanaged[Cdecl]<void*, void*, uint, PaStreamCallbackTimeInfo*, PaStreamCallbackFlags, void*, PaStreamCallbackResult> StreamCallback = &_StreamCallback;
#else
    private static PortAudio.PaStreamCallback ManagedStreamCallback = _StreamCallback;
    public static readonly delegate* unmanaged[Cdecl]<void*, void*, uint, PaStreamCallbackTimeInfo*, PaStreamCallbackFlags, void*, PaStreamCallbackResult> StreamCallback
        = (delegate* unmanaged[Cdecl]<void*, void*, uint, PaStreamCallbackTimeInfo*, PaStreamCallbackFlags, void*, PaStreamCallbackResult>)Marshal.GetFunctionPointerForDelegate(ManagedStreamCallback);
#endif

    public void Dispose()
        => ThisHandle.Free();
}
