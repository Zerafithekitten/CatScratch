using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
namespace AionMeter.Tests;
public class PartyOnlyTests
{
    private static CombatTracker Setup(bool roster = true)
    {
        var t = new CombatTracker(GameData.Empty, new MeterOptions { PartyOnly = true, TargetMode = TargetMode.All });
        t.Process(new SelfIdentifiedEvent(100, 1, "Self", 1001, GameClass.Gladiator));
        t.Process(new PlayerSeenEvent(100, 2, "Mate", 1001, GameClass.Gladiator));
        t.Process(new PlayerSeenEvent(100, 3, "Mate", 1002, GameClass.Gladiator));
        if(roster) Roster(t, "Mate");
        return t;
    }
    private static void Roster(CombatTracker t, string mate) => t.Process(new PartyRosterEvent(110,
        [new("Self",1001,50,1000,20000,GameClass.Gladiator),new(mate,1001,50,1000,20000,GameClass.Gladiator)]));
    private static void Hit(CombatTracker t,uint actor,long time=200) => t.Process(new DamageEvent(time,actor,500,11010000,100,HitFlags.None));
    [Fact] public void SoloCountsOnlyFreshSelf() { var t=Setup(false); Hit(t,2); Hit(t,3); Hit(t,1); Assert.Equal(100,t.Snapshot(null,300)!.TotalDamage); }
    [Fact] public void SoloSurvivesTickAndCountsOwnPet()
    { var t=Setup(false); t.Process(new SummonSeenEvent(150,8,1,null,0)); Hit(t,8,40000); t.Tick(40001); Hit(t,1,40002); Assert.Equal(200,t.Snapshot(null,40003)!.TotalDamage); }
    [Fact] public void ReusedSelfActorRequiresFreshIdentity()
    { var t=Setup(false); Hit(t,1); t.Process(new PlayerSeenEvent(250,1,"Other",1002,GameClass.Gladiator)); Hit(t,1,260); Assert.Null(t.Snapshot(null,300)); }
    [Fact] public void ZoneChangeRequiresFreshSoloIdentity()
    { var t=Setup(false); t.Process(new ZoneChangedEvent(150,600,"New zone",false)); Hit(t,1); Assert.Null(t.Snapshot(null,300)); t.Process(new SelfIdentifiedEvent(350,1,"Self",1001,GameClass.Gladiator)); Hit(t,1,400); Assert.Equal(100,t.Snapshot(null,500)!.TotalDamage); }
    [Fact] public void CachedSelfDoesNotEnableSoloBeforeLiveConfirmation()
    { var t=new CombatTracker(GameData.Empty,new MeterOptions {PartyOnly=true,TargetMode=TargetMode.All}); t.ImportCache(new SessionState(1,1,[new(1,"Self",GameClass.Gladiator,1001)],[])); Hit(t,1); Assert.Null(t.Snapshot(null,300)); }
    [Fact] public void ForeignServerAndUnknownActorsAreExcluded()
    { var t=Setup(); Hit(t,3); Hit(t,99); Hit(t,2); Hit(t,1); Assert.Equal(200,t.Snapshot(null,300)!.TotalDamage); }
    [Fact] public void OnlyExplicitlyOwnedGroupPetsCount()
    { var t=Setup(); t.Process(new SummonSeenEvent(150,8,2,null,0)); t.Process(new SummonSeenEvent(150,9,3,null,0)); Hit(t,8); Hit(t,9); Assert.Equal(100,t.Snapshot(null,300)!.TotalDamage); }
    [Fact] public void GroupChangeClearsPreviousDamage()
    { var t=Setup(); Hit(t,2); Roster(t,"NewMate"); Hit(t,2); Assert.Null(t.Snapshot(null,300)); Hit(t,1); Assert.Equal(100,t.Snapshot(null,300)!.TotalDamage); }
    [Fact] public void RosterWithoutSelfRevokesConfirmation()
    { var t=Setup(); Hit(t,1); t.Process(new PartyRosterEvent(250,[])); Hit(t,1,260); Assert.False(t.PartyConfirmed); Assert.Equal(100,t.Snapshot(null,300)!.TotalDamage); }
    [Fact] public void ConfirmedGroupSurvivesThirtySecondsAndIdleTicks()
    {
        var t=Setup(); Hit(t,1); Hit(t,2);
        t.Tick(30111); Assert.True(t.PartyConfirmed);
        Hit(t,1,30112); Hit(t,2,30112); Hit(t,3,30112);
        var snapshot=t.Snapshot(null,30113)!;
        Assert.Equal(200,snapshot.TotalDamage);
        Assert.Equal(2,snapshot.Combatants.Count);
        t.Tick(600000); Assert.True(t.PartyConfirmed);
        Hit(t,2,600001); Assert.Equal(100,t.Snapshot(null,600002)!.TotalDamage);
    }
    [Fact] public void ExplicitLeaveAndRestartRevokeGroup()
    {
        var t=Setup(); t.Process(new PartyLeftEvent(200));
        Assert.False(t.PartyConfirmed); Hit(t,2,201); Hit(t,1,202);
        Assert.Equal(100,t.Snapshot(null,203)!.TotalDamage);
        Roster(t,"Mate"); Assert.True(t.PartyConfirmed);
        t.InvalidateParty(); Assert.False(t.PartyConfirmed);
        Hit(t,1,301); Hit(t,2,301); Assert.Null(t.Snapshot(null,302));
    }
}
