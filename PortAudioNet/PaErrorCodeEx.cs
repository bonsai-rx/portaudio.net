using System.Diagnostics;

namespace PortAudioNet;

public static class PaErrorCodeEx
{
    public static void ThrowIfFailure(this PaErrorCode error)
    {
        if (error.IsFailure())
            throw new PortAudioException(error);
    }

    public unsafe static string GetErrorText(this PaErrorCode error)
    {
        byte* errorText = PortAudio.GetErrorText(error);
        return PortAudio.PtrToString(errorText) ?? throw new UnreachableException();
    }

    public static bool IsSuccess(this PaErrorCode error)
        => (int)error >= 0;

    public static bool IsFailure(this PaErrorCode error)
        => !error.IsSuccess();
}
