using System.IO;
using System.Windows.Media;
using System.Windows;

namespace AionMeter.App.Services;

internal static class CustomEventAudio
{
    public const string DefaultCubeSound="builtin:kitty-meow";
    public static bool IsCustom(string sound) => sound is "Custom WAV" or "Custom audio";
    public static Uri FileUri(string path)
    {
        var file = new FileInfo(path==DefaultCubeSound?Path.Combine(AppContext.BaseDirectory,"data","audio","kitty-meow.mp3"):path);
        if (!file.Exists) throw new InvalidDataException(UiText.Current.Localize("The selected audio file no longer exists. Choose it again."));
        if (file.Length <= 0) throw new InvalidDataException(UiText.Current.Localize("The selected audio file is empty."));
        if (file.Length > 10_000_000) throw new InvalidDataException(UiText.Current.Localize("The selected audio file exceeds 10 MB. Choose a smaller file."));
        if (!new[] { ".wav", ".mp3" }.Contains(file.Extension, StringComparer.OrdinalIgnoreCase))
            throw new InvalidDataException(UiText.Current.Localize("Unsupported audio format. Choose a WAV or MP3 file."));
        return new Uri(file.FullName);
    }
    public static bool AllowedDuration(Duration duration) => duration.HasTimeSpan &&
        duration.TimeSpan > TimeSpan.Zero && duration.TimeSpan <= TimeSpan.FromSeconds(10);

    // WPF's decoder supports WAV encodings and MP3, including variable bit rates. No helper process is started.
    public static async Task ValidateAsync(string path)
    {
        var duration = await DurationAsync(path);
        if (!AllowedDuration(new Duration(duration)))
            throw new InvalidDataException(string.Format(UiText.Current.Localize("This sound is {0:0.00} seconds long. Maximum: 10 seconds. Choose a shorter file."), duration.TotalSeconds));
    }
    internal static async Task<TimeSpan> DurationAsync(string path)
    {
        var uri = FileUri(path);
        if (Path.GetExtension(path).Equals(".wav", StringComparison.OrdinalIgnoreCase))
        {
            try { using var wave=File.OpenRead(path); return Core.Audio.WaveSound.Validate(wave, int.MaxValue); }
            catch (InvalidDataException) { /* Other WAV encodings still use the Windows decoder. */ }
        }
        var player = new MediaPlayer();
        var completion = new TaskCompletionSource<TimeSpan>();
        player.MediaOpened += (_, _) => completion.TrySetResult(player.NaturalDuration.HasTimeSpan ? player.NaturalDuration.TimeSpan : TimeSpan.Zero);
        player.MediaFailed += (_, _) => completion.TrySetResult(TimeSpan.Zero);
        try
        {
            player.Open(uri);
            var duration = await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
            if (duration <= TimeSpan.Zero)
                throw new InvalidDataException(UiText.Current.Localize("Windows could not decode this audio file or determine its duration. Choose another WAV or MP3 file."));
            return duration;
        }
        catch (TimeoutException) { throw new InvalidDataException("The audio file could not be opened. Choose another WAV or MP3 file."); }
        finally { player.Close(); }
    }
}
