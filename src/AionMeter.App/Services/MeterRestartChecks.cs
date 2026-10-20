using AionMeter.Core.Capture;
using AionMeter.Core.Combat;
using AionMeter.Core.Events;
namespace AionMeter.App.Services;
internal static class MeterRestartChecks
{
    private sealed class Source:IEventSource
    {
        public event Action<GameEvent>? EventDecoded;
        public event Action<CaptureStatus>? StatusChanged;
        public CaptureStatus Status{get;private set;}=CaptureStatus.Stopped;
        public void Send(GameEvent e)=>EventDecoded?.Invoke(e);
        public void Start(){Status=new(CaptureState.Capturing,"Test stream");StatusChanged?.Invoke(Status);}
        public void Stop(){Status=CaptureStatus.Stopped;StatusChanged?.Invoke(Status);}
        public void Dispose()=>Stop();
    }
    internal static void Run(Action<bool,string> check)
    {
        Source? source=null;
        using var meter=new MeterService(new AppSettings{Transient=true,DownloadIcons=false},_=>source=new Source());
        meter.Options.PartyOnly=true;meter.Options.TargetMode=TargetMode.All;meter.StartCapture();
        source!.Send(new SelfIdentifiedEvent(1,1,"Self",2305,GameClass.Gladiator));
        source.Send(new PlayerSeenEvent(1,2,"Mate",2305,GameClass.Gladiator));
        source.Send(new PartyRosterEvent(1,[new("Self",2305,45,2000,100000,GameClass.Gladiator),new("Mate",2305,45,2000,100000,GameClass.Gladiator)]));
        source.Send(new DamageEvent(2,2,500,11010000,100,HitFlags.None));
        check(meter.Tracker.PartyConfirmed && meter.Tracker.Snapshot(null,3)?.TotalDamage==100,"Restart fixture starts with a confirmed live party");
        meter.Tracker.Reset();meter.RestartCapture(preserveSession:true);
        check(meter.LiveIdentity.Current?.Name=="Self" && meter.Tracker.PartyConfirmed,"Measurement restart retains confirmed own identity and party across capture replacement");
        source!.Send(new DamageEvent(4,2,500,11010000,150,HitFlags.None));
        check(meter.Tracker.Snapshot(null,5)?.TotalDamage==150,"Damage resumes after measurement restart without repeated identity or roster packets");
        source.Send(new ResourceSnapshotStartedEvent(6));source.Send(new DamageEvent(7,2,500,11010000,200,HitFlags.None));
        check(meter.LiveIdentity.Current is null && !meter.Tracker.PartyConfirmed && meter.Tracker.Snapshot(null,8) is null,"A new login invalidates retained party proof even on the same map");
        source.Send(new SelfIdentifiedEvent(9,3,"Other",2305,GameClass.Gladiator));
        meter.RestartCapture();
        check(meter.LiveIdentity.Current is null && !meter.Tracker.PartyConfirmed,"Settings-driven capture restart still requires fresh session confirmation");
    }
}
