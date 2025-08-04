using System;

namespace PortAudioNet;

public sealed class PortAudioException : Exception
{
    public unsafe PortAudioException(PaError errorCode)
        : base(errorCode.GetErrorText())
    { }
}
