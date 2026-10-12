using AionMeter.Core.Combat;
using AionMeter.Core.Capture;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
namespace AionMeter.Tests;
public class SoloIdentityTests
{
    private static CombatTracker Meter() => new(GameData.Empty,new MeterOptions { PartyOnly=true,TargetMode=TargetMode.All });
    private static void Hit(CombatTracker t,uint actor,long at=200)=>t.Process(new DamageEvent(at,actor,500,11010000,100,HitFlags.None));
    [Fact] public void OwnActorWithoutNameOrServerCountsSoloAndExcludesStrangers()
    { var t=Meter(); t.Process(new SelfIdentifiedEvent(100,1,"",0,GameClass.Unknown)); Hit(t,2); Hit(t,1); var s=t.Snapshot(null,300)!; Assert.Equal(100,s.TotalDamage); Assert.Equal("You",Assert.Single(s.Combatants).Name); }
    [Fact] public void OwnNameWithoutServerStillCounts()
    { var t=Meter(); t.Process(new SelfIdentifiedEvent(100,1,"Self",0,GameClass.Unknown)); Hit(t,1); Assert.Equal(100,t.Snapshot(null,300)!.TotalDamage); }
    [Fact] public void PartialRefreshDoesNotClearRunningFight()
    { var t=Meter(); t.Process(new SelfIdentifiedEvent(100,1,"Self",1001,GameClass.Gladiator)); Hit(t,1); t.Process(new SelfIdentifiedEvent(220,1,"",0,GameClass.Unknown)); Hit(t,1,250); Assert.Equal(200,t.Snapshot(null,300)!.TotalDamage); }
    [Fact] public void AnonymousOwnRecordDoesNotTrustCachedPartyName()
    { var t=Meter(); t.ImportCache(new SessionState(1,1,[new(1,"Old",GameClass.Gladiator,1001),new(2,"Other",GameClass.Gladiator,1001)],[])); t.Process(new SelfIdentifiedEvent(100,1,"",0,GameClass.Unknown)); t.Process(new PartyRosterEvent(150,[new("Old",1001,50,1000,20000,GameClass.Gladiator),new("Other",1001,50,1000,20000,GameClass.Gladiator)])); Hit(t,2); Hit(t,1); Assert.False(t.PartyConfirmed); Assert.Equal(100,t.Snapshot(null,300)!.TotalDamage); }
    [Fact] public void LiveOwnActorDoesNotClaimAConfirmedName()
    { var i=new LiveIdentity(); i.Observe(new SelfIdentifiedEvent(100,1,"",0,GameClass.Unknown)); Assert.Equal((uint)1,i.ActorId); Assert.Null(i.Current); i.Reset(); Assert.Null(i.ActorId); }

    [Fact] public void SoloDamageSurvivesRepeatedCaptureStatusAndTrafficPauses()
    {
        var t=Meter(); var identity=new LiveIdentity();
        var self=new SelfIdentifiedEvent(100,1,"Self",1001,GameClass.Gladiator);
        t.Process(self); identity.Observe(self); Hit(t,1);
        foreach(var status in new[] {
            new CaptureStatus(CaptureState.Capturing,"Capturing · server",10,2),
            new CaptureStatus(CaptureState.WaitingForGame,"Connected · no game traffic for a while",10,2),
            new CaptureStatus(CaptureState.Capturing,"Capturing · server",20,4) })
        {
            identity.ObserveCaptureStatus(status,t.InvalidateParty);
            Hit(t,1,250); Hit(t,2,250);
        }
        Assert.Equal((uint)1,identity.ActorId);
        Assert.Equal(400,t.Snapshot(null,300)!.TotalDamage);
        Assert.Equal("Self",Assert.Single(t.Snapshot(null,300)!.Combatants).Name);
    }

    [Fact] public void GameExitClearsIdentityAndRequiresFreshOwnActor()
    {
        var t=Meter(); var identity=new LiveIdentity();
        var self=new SelfIdentifiedEvent(100,1,"Self",1001,GameClass.Gladiator);
        t.Process(self); identity.Observe(self); Hit(t,1);
        identity.ObserveCaptureStatus(new CaptureStatus(CaptureState.WaitingForGame,"AION 2 is not running"),t.InvalidateParty);
        identity.ObserveCaptureStatus(new CaptureStatus(CaptureState.Capturing,"Capturing · server"),t.InvalidateParty);
        Hit(t,1,250);
        Assert.Null(identity.ActorId); Assert.Null(t.Snapshot(null,300));
        t.Process(self); identity.Observe(self); Hit(t,1,270);
        Assert.Equal(100,t.Snapshot(null,300)!.TotalDamage);
    }
}
