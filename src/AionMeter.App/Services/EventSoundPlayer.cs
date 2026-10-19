using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace AionMeter.App.Services;

public sealed class EventSoundPreference
{
    public bool Enabled { get; set; }
    public string Sound { get; set; } = "Chime";
    public string? FilePath { get; set; }
}

internal interface IEventSoundOutput : IDisposable
{
    event Action? Opened;
    event Action? Ended;
    event Action<Exception>? Failed;
    Duration Duration {get;}
    void Open(Uri file,double volume);
    void Play();
}

internal sealed class WindowsEventSoundOutput : IEventSoundOutput
{
    private readonly MediaPlayer _player=new();
    public event Action? Opened;
    public event Action? Ended;
    public event Action<Exception>? Failed;
    public Duration Duration=>_player.NaturalDuration;
    public WindowsEventSoundOutput()
    {
        _player.MediaOpened+=(_,_)=>Opened?.Invoke();
        _player.MediaEnded+=(_,_)=>Ended?.Invoke();
        _player.MediaFailed+=(_,e)=>Failed?.Invoke(e.ErrorException);
    }
    public void Open(Uri file,double volume){_player.Volume=volume;_player.Open(file);}
    public void Play()=>_player.Play();
    public void Dispose()=>_player.Close();
}

/// <summary>Bounded queued audio with a fresh decoder for each clip and recovery from missing decoder callbacks.</summary>
internal sealed class EventSoundPlayer : IDisposable
{
    private readonly Queue<(Uri File,double Volume)> _queue=new();
    private readonly Func<IEventSoundOutput> _create;
    private readonly Func<long> _clock;
    private readonly DispatcherTimer _watchdog=new(){Interval=TimeSpan.FromMilliseconds(500)};
    private IEventSoundOutput? _current;
    private long _deadline;
    private bool _disposed;
    internal EventSoundPlayer(Func<IEventSoundOutput>? create=null,Func<long>? clock=null)
    {
        _create=create??(()=>new WindowsEventSoundOutput());_clock=clock??(()=>Environment.TickCount64);
        _watchdog.Tick+=(_,_)=>RecoverStalledPlayback();
    }
    public void Play(EventSoundPreference preference,double volume,bool preview=false)
    {
        if(_disposed || !preview && !preference.Enabled)return;
        var path=CustomEventAudio.IsCustom(preference.Sound)?preference.FilePath:
            Path.Combine(AppContext.BaseDirectory,"sounds",preference.Sound is "Bell" or "Ping"?preference.Sound+".wav":"Chime.wav");
        if(path is null || !File.Exists(path)){Log.Info("Configured event sound is missing.");return;}
        Uri file;
        try{file=CustomEventAudio.FileUri(path);}
        catch(Exception error) when(error is InvalidDataException or IOException or UnauthorizedAccessException or ArgumentException)
        {Log.Error("Custom event sound must be WAV or MP3, up to 10 seconds and 10 MB",error);return;}
        if(preview){_queue.Clear();ReleaseCurrent();}
        RecoverStalledPlayback();
        if(_queue.Count>=16)return;
        _queue.Enqueue((file,double.IsFinite(volume)?Math.Clamp(volume,0,1):.6));PlayNext();
    }
    internal void RecoverStalledPlayback()
    {
        if(_disposed || _current is null || _clock()<_deadline)return;
        Log.Info("Audio decoder timed out; releasing it for subsequent notifications.");
        ReleaseCurrent();PlayNext();
    }
    private void ReleaseCurrent()
    {
        _watchdog.Stop();var previous=_current;_current=null;
        previous?.Dispose();
    }
    private void Finish(IEventSoundOutput sound)
    {
        if(!ReferenceEquals(_current,sound) || _disposed)return;
        ReleaseCurrent();PlayNext();
    }
    private void PlayNext()
    {
        if(_disposed || _current is not null || !_queue.TryDequeue(out var clip))return;
        try
        {
            CustomEventAudio.FileUri(clip.File.LocalPath);
            var sound=_current=_create();
            sound.Opened+=()=>
            {
                if(!ReferenceEquals(_current,sound) || _disposed)return;
                if(!CustomEventAudio.AllowedDuration(sound.Duration)){Finish(sound);return;}
                // Files are limited to ten seconds. Never depend solely on MediaEnded to release a decoder.
                _deadline=_clock()+(long)Math.Ceiling(sound.Duration.TimeSpan.TotalMilliseconds)+1500;
                try{sound.Play();}
                catch(Exception error) when(error is InvalidOperationException or ArgumentException)
                {Log.Error("Event sound could not play",error);Finish(sound);}
            };
            sound.Ended+=()=>Finish(sound);
            sound.Failed+=error=>{if(ReferenceEquals(_current,sound)){Log.Error("Event sound could not be played",error);Finish(sound);}};
            _deadline=_clock()+5000;_watchdog.Start();sound.Open(clip.File,clip.Volume);
        }
        catch(Exception error) when(error is InvalidDataException or IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {Log.Error("Event sound could not start",error);ReleaseCurrent();PlayNext();}
    }
    public void Dispose(){if(_disposed)return;_disposed=true;_queue.Clear();ReleaseCurrent();}
}
