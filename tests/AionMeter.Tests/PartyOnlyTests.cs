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
    [Fact] public void Group_survives_dungeon_load_but_requires_new_actor_proofs()
    {
        var t=Setup(); Hit(t,2);
        t.Process(new ZoneChangedEvent(300,600001,"Dungeon",true));
        Hit(t,2,301); Assert.Null(t.Snapshot(null,302));
        t.Process(new SelfIdentifiedEvent(400,11,"Self",1001,GameClass.Gladiator));
        Assert.True(t.PartyConfirmed);
        Hit(t,2,401); Assert.Null(t.Snapshot(null,402));
        t.Process(new PlayerSeenEvent(410,12,"Mate",1001,GameClass.Gladiator));
        t.Process(new PlayerSeenEvent(411,13,"Mate",1002,GameClass.Gladiator));
        Hit(t,12,420); Hit(t,11,421); Hit(t,13,422);
        var snapshot=t.Snapshot(null,430)!;
        Assert.Equal(200,snapshot.TotalDamage); Assert.Equal(2,snapshot.Combatants.Count);
        t.Process(new PartyLeftEvent(440)); Hit(t,12,441); Hit(t,11,442);
        Assert.Equal(100,t.Snapshot(null,450)!.TotalDamage);
    }
    [Fact] public void Roster_before_identity_enables_only_fresh_confirmed_members()
    {
        var t=Setup(false); t.InvalidateParty();
        Roster(t,"Mate"); Assert.False(t.PartyConfirmed);
        t.Process(new SelfIdentifiedEvent(150,1,"Self",1001,GameClass.Gladiator));
        Assert.True(t.PartyConfirmed); Hit(t,1); Hit(t,2); Hit(t,3);
        Assert.Equal(200,t.Snapshot(null,300)!.TotalDamage);
    }
    [Fact] public void Character_change_revokes_group_after_loading()
    {
        var t=Setup(); t.Process(new ZoneChangedEvent(300,600001,"Dungeon",true));
        t.Process(new SelfIdentifiedEvent(400,11,"Other",1001,GameClass.Gladiator));
        Assert.False(t.PartyConfirmed); Hit(t,2,420); Hit(t,11,421);
        Assert.Equal(100,t.Snapshot(null,430)!.TotalDamage);
    }
    [Fact] public void RespawnedMemberKeepsDamageHealingAndTakenInTheSameLongBossFight()
    {
        var t=Setup();t.Process(new NpcSeenEvent(120,500,0,10_000_000));Hit(t,1,200);Hit(t,2,201);
        t.Process(new HealEvent(202,2,2,11010000,50,HitFlags.None));
        t.Process(new DamageEvent(203,500,2,2000000,25,HitFlags.None));
        var id=t.Snapshot(null,204)!.Id;t.Process(new DeathEvent(300,2));
        for(long time=1000;time<=121000;time+=1000){Hit(t,1,time);t.Tick(time);}
        Assert.Equal(100,t.Snapshot(null,121001)!.Combatants.Single(c=>c.Name=="Mate").Damage);
        Hit(t,12,121002); // an unproven replacement actor must not be accepted
        t.Process(new PlayerSeenEvent(121003,12,"Mate",1001,GameClass.Gladiator));
        var returned=t.Snapshot(null,121004)!;Assert.Equal(id,returned.Id);
        var mate=Assert.Single(returned.Combatants,c=>c.Name=="Mate");Assert.Equal(12u,mate.ActorId);Assert.Equal(100,mate.Damage);
        Hit(t,12,121005);mate=t.Snapshot(null,121006)!.Combatants.Single(c=>c.Name=="Mate");
        Assert.Equal(200,mate.Damage);Assert.Equal(50,mate.Healing);Assert.Equal(25,mate.DamageTaken);
    }
    [Fact] public void SameNameFromAnotherServerCannotTakeOverARespawnedPartyMember()
    {
        var t=Setup();Hit(t,2);t.Process(new PlayerSeenEvent(300,12,"Mate",1002,GameClass.Gladiator));Hit(t,12,310);
        var mate=Assert.Single(t.Snapshot(null,320)!.Combatants);Assert.Equal(2u,mate.ActorId);Assert.Equal(100,mate.Damage);
    }
    [Fact] public void RebindingLongFightTotalsDoesNotLoseHitsBeyondRotationHistoryLimit()
    {
        var old=new Combatant{ActorId=2};
        for(var i=0;i<=Combatant.MaxCasts;i++)old.AddDamage(0,11010000,1,HitFlags.None);
        old.AddDamage(1,11010001,100,HitFlags.Critical);var returned=new Combatant{ActorId=12};returned.MergeFrom(old);
        Assert.Equal(Combatant.MaxCasts+101,returned.Total.Damage);Assert.Equal(Combatant.MaxCasts+2,returned.Total.Hits);
        Assert.Equal(1,returned.Total.Crits);Assert.Equal(11010001,returned.MaxHitSkill);Assert.Equal(100,returned.Skills[11010001].Stats.Damage);
        Assert.Equal(Combatant.MaxCasts,returned.Casts.Count);
    }
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

    [Fact] public void MeasurementResetKeepsConfirmedPartyButANewLoginInvalidatesIt()
    {
        var tracker=Setup();Hit(tracker,2);tracker.Reset();
        Assert.True(tracker.PartyConfirmed);Hit(tracker,2,300);
        Assert.Equal(100,tracker.Snapshot(null,301)!.TotalDamage);
        tracker.Process(new ResourceSnapshotStartedEvent(400));
        Assert.False(tracker.PartyConfirmed);Hit(tracker,2,401);Assert.Null(tracker.Snapshot(null,402));
    }
}
