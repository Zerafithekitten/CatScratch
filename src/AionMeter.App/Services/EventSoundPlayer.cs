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
        _player.MediaFailed += (_, e) => { Log.Error("Event sound could not be played", e.ErrorException); _player.Close(); _playing = false; PlayNext(); };
    }
    public void Play(EventSoundPreference preference, double volume, bool preview = false)
    {
        if (_disposed || !preview && !preference.Enabled) return;
        var path = preference.Sound == "Custom WAV" ? preference.FilePath :
            Path.Combine(AppContext.BaseDirectory, "sounds", preference.Sound is "Bell" or "Ping" ? preference.Sound + ".wav" : "Chime.wav");
        if (path is null || !File.Exists(path)) { Log.Info("Configured event sound is missing."); return; }
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
            _player.Volume = sound.Volume;
            _player.Open(sound.File);
            _player.Play();
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or ArgumentException)
        {
            Log.Error("Event sound could not start", error);
            _player.Close(); _playing = false; PlayNext();
        }
    }
    public void Dispose() { _disposed = true; _queue.Clear(); _player.Close(); }
}
