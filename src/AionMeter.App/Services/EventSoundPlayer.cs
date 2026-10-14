using System.IO;
using System.Windows.Media;

namespace AionMeter.App.Services;

public sealed class EventSoundPreference
{
    public bool Enabled { get; set; }
    public string Sound { get; set; } = "Chime";
    public string? FilePath { get; set; }
}

/// <summary>One reusable player; simultaneous reminders are queued, with no polling or resident helper.</summary>
internal sealed class EventSoundPlayer : IDisposable
{
    private readonly MediaPlayer _player = new();
    private readonly Queue<(Uri File, double Volume)> _queue = new();
    private bool _playing, _disposed;
    public EventSoundPlayer()
    {
        _player.MediaEnded += (_, _) => { _player.Close(); _playing = false; PlayNext(); };
        _player.MediaOpened += (_, _) =>
        {
            if (!CustomEventAudio.AllowedDuration(_player.NaturalDuration))
            { _player.Close(); _playing = false; PlayNext(); }
            else _player.Play();
        };
        _player.MediaFailed += (_, e) => { Log.Error("Event sound could not be played", e.ErrorException); _player.Close(); _playing = false; PlayNext(); };
    }
    public void Play(EventSoundPreference preference, double volume, bool preview = false)
    {
        if (_disposed || !preview && !preference.Enabled) return;
        var path = CustomEventAudio.IsCustom(preference.Sound) ? preference.FilePath :
            Path.Combine(AppContext.BaseDirectory, "sounds", preference.Sound is "Bell" or "Ping" ? preference.Sound + ".wav" : "Chime.wav");
        if (path is null || !File.Exists(path)) { Log.Info("Configured event sound is missing."); return; }
        if (CustomEventAudio.IsCustom(preference.Sound))
        {
            try { CustomEventAudio.FileUri(path); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            { Log.Error("Custom event sound must be WAV or MP3, up to 10 seconds and 10 MB",error); return; }
        }
        if (preview) { _queue.Clear(); _player.Close(); _playing = false; }
        if (_queue.Count >= 16) return;
        _queue.Enqueue((new Uri(Path.GetFullPath(path)), double.IsFinite(volume) ? Math.Clamp(volume, 0, 1) : .6));
        PlayNext();
    }
    private void PlayNext()
    {
        if (_disposed || _playing || !_queue.TryDequeue(out var sound)) return;
        _playing = true;
        try
        {
            // Recheck queued files: an external editor may have replaced them since the reminder was queued.
            CustomEventAudio.FileUri(sound.File.LocalPath);
            _player.Volume = sound.Volume;
            _player.Open(sound.File);

        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            Log.Error("Event sound could not start", error);
            _player.Close(); _playing = false; PlayNext();
        }
    }
    public void Dispose() { _disposed = true; _queue.Clear(); _player.Close(); }
}
