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
        var file = new FileInfo(path==DefaultCubeSound?Path.Combine(AppContext.BaseDirectory,"data","audio","kitty-meow.wav"):path);
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
        if (Path.GetExtension(uri.LocalPath).Equals(".wav", StringComparison.OrdinalIgnoreCase))
        {
            try { using var wave=File.OpenRead(uri.LocalPath); return Core.Audio.WaveSound.Validate(wave, int.MaxValue); }
            catch (InvalidDataException) { /* Other WAV encodings still use the Windows decoder. */ }
        }
        return await ProbeDurationAsync(uri, () => new WindowsEventSoundOutput());
    }

    // MediaOpened can precede a usable NaturalDuration. Keep the decoder alive while
    // metadata settles, then retry one fresh decoder after a transient Windows failure.
    internal static async Task<TimeSpan> ProbeDurationAsync(Uri uri, Func<IEventSoundOutput> create)
    {
        Exception? last = null;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var player = create();
            var completion = new TaskCompletionSource();
            player.Opened += () => completion.TrySetResult();
            player.Failed += error => completion.TrySetException(error);
            try
            {
                player.Open(uri, 0);
                await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
                var until = Environment.TickCount64 + 5000;
                while (!player.Duration.HasTimeSpan && Environment.TickCount64 < until)
                    await Task.Delay(25);
                if (player.Duration.HasTimeSpan && player.Duration.TimeSpan > TimeSpan.Zero)
                    return player.Duration.TimeSpan;
                last = new InvalidDataException("Windows returned no audio duration.");
            }
            catch (Exception error) when (error is TimeoutException or InvalidOperationException or ArgumentException or System.Runtime.InteropServices.COMException or IOException or NotSupportedException or FormatException)
            { last = error; }
            if (attempt == 0) await Task.Delay(150);
        }
        throw new InvalidDataException(UiText.Current.Localize("Windows could not decode this audio file or determine its duration. Choose another WAV or MP3 file."), last);
    }
}
