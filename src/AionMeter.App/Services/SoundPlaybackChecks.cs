using System.Windows;
namespace AionMeter.App.Services;

internal static class SoundPlaybackChecks
{
    private sealed class Output : IEventSoundOutput
    {
        public event Action? Opened;
        public event Action? Ended;
        public event Action<Exception>? Failed;
        public Duration Duration {get;set;}=new(TimeSpan.FromSeconds(1));
        public Action<Output>? OnOpen;
        public int Plays;public bool Disposed;public double Volume;
        public void Open(Uri file,double volume){Volume=volume;OnOpen?.Invoke(this);}
        public void Play()=>Plays++;
        public void Dispose()=>Disposed=true;
        public void Ready()=>Opened?.Invoke();
        public void Complete()=>Ended?.Invoke();
        public void Fail()=>Failed?.Invoke(new InvalidOperationException("Synthetic decoder failure"));
    }
    internal static async Task CheckProbeAsync(string file,Action<bool,string> check)
    {
        var attempts=0;var created=new List<Output>();
        IEventSoundOutput Create()
        {
            var sound=new Output();created.Add(sound);
            sound.OnOpen=o=>{if(++attempts==1)o.Fail();else o.Ready();};
            return sound;
        }
        var duration=await CustomEventAudio.ProbeDurationAsync(new Uri(file),Create);
        check(attempts==2 && duration==TimeSpan.FromSeconds(1) && created.All(o=>o.Disposed),"Transient validation failure retries and releases both decoders");
        var delayed=new Output{Duration=Duration.Automatic};
        delayed.OnOpen=o=>{o.Ready();_ = SetDuration();};
        async Task SetDuration(){await Task.Delay(75);delayed.Duration=new Duration(TimeSpan.FromSeconds(2));}
        duration=await CustomEventAudio.ProbeDurationAsync(new Uri(file),()=>delayed);
        check(duration==TimeSpan.FromSeconds(2) && delayed.Disposed,"Validation waits for duration metadata after MediaOpened");
        attempts=0;
        try
        {
            await CustomEventAudio.ProbeDurationAsync(new Uri(file),()=>new Output{OnOpen=o=>{attempts++;o.Fail();}});
            check(false,"Persistent decoder failures must be rejected");
        }
        catch(System.IO.InvalidDataException){check(attempts==2,"Persistent failure retries only once");}
    }

    internal static void Run(string file,Action<bool,string> check)
    {
        var outputs=new List<Output>();long now=0;
        IEventSoundOutput Create(){var next=new Output();outputs.Add(next);return next;}
        var preference=new EventSoundPreference{Enabled=true,Sound="Custom audio",FilePath=file};
        using(var player=new EventSoundPlayer(Create,()=>now))
        {
            for(var i=0;i<3;i++){player.Play(preference,.28);outputs[^1].Ready();outputs[^1].Complete();}
            check(outputs.Count==3 && outputs.All(o=>o.Plays==1 && o.Disposed && o.Volume==.28),"Repeated notifications of the same file use independent decoders and retain volume");
            outputs.Clear();player.Play(preference,.28);player.Play(preference,.28);
            var stalled=outputs[0];now=5001;player.RecoverStalledPlayback();
            check(stalled.Disposed && outputs.Count==2,"Missing MediaOpened cannot block later notifications indefinitely");
            stalled.Ready();stalled.Complete();stalled.Fail();
            check(outputs[1].Plays==0 && !outputs[1].Disposed,"Late callbacks from an abandoned decoder cannot affect the next sound");
            outputs[1].Ready();check(outputs[1].Plays==1,"Queued notification plays after stalled-open recovery");
            player.Play(preference,.28);now+=2501;player.RecoverStalledPlayback();
            check(outputs.Count==3 && outputs[1].Disposed,"Missing MediaEnded releases the decoder after its duration and grace period");
            outputs[2].Ready();outputs[2].Complete();
            player.Play(preference,.28);player.Play(preference,.28);var failing=outputs[^1];failing.Fail();
            check(failing.Disposed && !outputs[^1].Disposed,"Decoder failure retries with a fresh decoder");
            outputs[^1].Fail();var failedRetry=outputs[^1];
            check(!failedRetry.Disposed,"A second decoder failure advances to the next queued notification");
            outputs[^1].Ready();outputs[^1].Complete();
            player.Play(preference,.28);player.Play(preference,.28);var interrupted=outputs[^1];
            player.Play(preference,.6,preview:true);var preview=outputs[^1];preview.Ready();preview.Complete();
            var count=outputs.Count;now+=10000;player.RecoverStalledPlayback();
            check(interrupted.Disposed && preview.Plays==1 && preview.Volume==.6 && outputs.Count==count,"Preview replaces active audio and clears old queued reminders");
            player.Play(preference,.28);player.Play(preference,.28);var closing=outputs[^1];player.Dispose();closing.Ready();closing.Complete();
            var closedCount=outputs.Count;now+=10000;player.RecoverStalledPlayback();player.Play(preference,.28);
            check(closing.Disposed && outputs.Count==closedCount,"Disposal stops recovery and prevents subsequent playback");
        }
    }
}
