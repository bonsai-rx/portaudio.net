using Biohazrd;
using System.IO;

namespace PortAudioNet.Generator;

internal static class BiohazrdExtensions
{
    public static string? CSharpFriendlyName(this TranslatedFile file)
        => Path.GetFileNameWithoutExtension(file.FilePath) switch
        {
            "pa_asio" => "Asio",
            "pa_jack" => "Jack",
            "pa_linux_alsa" => "Alsa",
            "pa_linux_pulseaudio" => "PuleAudio",
            "pa_mac_core" => "CoreAudio",
            "pa_win_ds" => "DirectSound",
            "pa_win_wasapi" => "Wasapi",
            "pa_win_waveformat" => "WaveFormat",
            "pa_win_wdmks" => "WdmKs",
            "pa_win_wmme" => "Mme",
            "portaudio" => null,
            _ => null,
        };
}
