using System.IO;
using System.Windows.Media;
using System.Windows;

namespace AionMeter.App.Services;

internal static class CustomEventAudio
{
    public static bool IsCustom(string sound) => sound is "Custom WAV" or "Custom audio";
    public static Uri FileUri(string path)
    {
        var file = new FileInfo(path);
        if (!file.Exists || file.Length is <= 0 or > 10_000_000 ||
            !new[] { ".wav", ".mp3" }.Contains(file.Extension, StringComparer.OrdinalIgnoreCase))
            throw new InvalidDataException("Choose a valid WAV or MP3 file, up to 10 seconds and 10 MB.");
        return new Uri(file.FullName);
    }
    public static bool AllowedDuration(Duration duration) => duration.HasTimeSpan &&
        duration.TimeSpan > TimeSpan.Zero && duration.TimeSpan <= TimeSpan.FromSeconds(10);

    // WPF's decoder supports WAV encodings and MP3, including variable bit rates. No helper process is started.
    public static async Task ValidateAsync(string path)
    {
        if (!AllowedDuration(new Duration(await DurationAsync(path))))
            throw new InvalidDataException("Choose a valid WAV or MP3 file, up to 10 seconds and 10 MB.");
    }
    internal static async Task<TimeSpan> DurationAsync(string path)
    {
        var uri = FileUri(path);
        var player = new MediaPlayer();
        var completion = new TaskCompletionSource<TimeSpan>();
        player.MediaOpened += (_, _) => completion.TrySetResult(player.NaturalDuration.HasTimeSpan ? player.NaturalDuration.TimeSpan : TimeSpan.Zero);
        player.MediaFailed += (_, _) => completion.TrySetResult(TimeSpan.Zero);
        try
        {
            player.Open(uri);
            var duration = await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
            if (duration <= TimeSpan.Zero)
                throw new InvalidDataException("Choose a valid WAV or MP3 file, up to 10 seconds and 10 MB.");
            return duration;
        }
        catch (TimeoutException) { throw new InvalidDataException("The audio file could not be opened. Choose another WAV or MP3 file."); }
        finally { player.Close(); }
    }
}
