using System.Diagnostics;

namespace PortAudioNet;

public static class PaErrorEx
{
    public static void Throw(this PaError error)
        => throw new PortAudioException(error);

    public static void ThrowIfFailure(this PaError error)
    {
        if (error.IsFailure())
            error.Throw();
    }

    public unsafe static string GetErrorText(this PaError error)
    {
        byte* errorText = PortAudio.GetErrorText(error);
        return PortAudio.PtrToString(errorText) ?? throw new UnreachableException();
    }

    public static bool IsSuccess(this PaError error)
        => (int)error >= 0;

    public static bool IsFailure(this PaError error)
        => !error.IsSuccess();
}
