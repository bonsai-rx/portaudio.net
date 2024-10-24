using System;

namespace PortAudioNet;

public sealed class PortAudioException : Exception
{
    public unsafe PortAudioException(PaErrorCode errorCode)
        : base(errorCode.GetErrorText())
    { }
}
