using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PortAudioNet;

unsafe partial class PortAudio
{
    public static string? PtrToString(byte* ptr)
    {
#if NETCOREAPP1_1_OR_GREATER
        return Marshal.PtrToStringUTF8((IntPtr)ptr);
#else
        int byteCount = 0;
        while (ptr[byteCount] != 0)
            byteCount++;
        return Encoding.UTF8.GetString(ptr, byteCount);
#endif
    }
}
