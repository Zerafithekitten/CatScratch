using AionMeter.Core.Events;
using AionMeter.Core.Game;
namespace AionMeter.Tests;
public class PvpCombatTests
{
    private static PvpCombat Setup()
    {
        var p=new PvpCombat();p.Observe(new SelfIdentifiedEvent(1000,1,"Own",1,GameClass.Ranger));
        p.Observe(new PlayerSeenEvent(1000,2,"Other",1,GameClass.Templar));return p;
    }
    [Fact] public void OnlyIdentifiedNonPartyPlayersCount()
    {
        var p=Setup();p.Observe(new DamageEvent(1100,1,99,1,100,0));Assert.Equal(0,p.Snapshot(1100).Outgoing);
        p.Observe(new PartyRosterEvent(1100,[new("Other",1,1,0,0,GameClass.Templar)]));
        p.Observe(new DamageEvent(1200,1,2,1,100,0));Assert.Equal(0,p.Snapshot(1200).Outgoing);
        p.Observe(new PartyLeftEvent(1300));p.Observe(new DamageEvent(1400,1,2,1,100,0));p.Observe(new DamageEvent(1400,2,1,1,70,0));
        var s=p.Snapshot(1400);Assert.Equal(2u,s.ActorId);Assert.Equal(100,s.Outgoing);Assert.Equal(70,s.Incoming);
    }
    [Fact] public void HpMustBeConfirmedFreshAndNeverDerivedFromDamage()
    {
        var p=Setup();p.Observe(new DamageEvent(1200,1,2,1,100,0));Assert.Null(p.Snapshot(1200).Hp);
        p.Observe(new NpcHpEvent(1200,2,900,1000,false));Assert.Equal(900,p.Snapshot(1200).Hp);
        p.Observe(new DamageEvent(1300,1,2,1,300,0));Assert.Equal(900,p.Snapshot(1300).Hp);
        Assert.Null(p.Snapshot(6201).Hp);Assert.True(p.Snapshot(6201).HpStale);
        p.Observe(new NpcHpEvent(6400,2,400,0,false));Assert.Equal(400,p.Snapshot(6400).Hp);Assert.Null(p.Snapshot(6400).MaxHp);
        p.Observe(new NpcHpEvent(6000,2,600,1000,false));Assert.Equal(400,p.Snapshot(6400).Hp);
    }
    [Fact] public void DeathAndContextChangesRemoveTargets()
    {
        var p=Setup();p.Observe(new DamageEvent(1200,1,2,1,100,0));p.Observe(new DeathEvent(1300,2));Assert.Equal(0u,p.Snapshot(1400).ActorId);
        p.Observe(new PlayerSeenEvent(1500,2,"New",1,GameClass.Cleric));p.Observe(new DamageEvent(1600,1,2,1,100,0));
        Assert.Equal("New",p.Snapshot(1600).Name);Assert.Null(p.Snapshot(1600).Hp);
        p.Observe(new ZoneChangedEvent(1700,1,"Zone",false));Assert.Equal(0u,p.Snapshot(1800).ActorId);
        p.Reset();Assert.Equal(0,p.Snapshot(1800).Outgoing);
    }
    [Fact] public void TargetExpiresAndResetPreservesIdentitiesButClearsSession()
    {
        var p=Setup();p.Observe(new DamageEvent(1200,1,2,1,100,0));Assert.Equal(0u,p.Snapshot(21201).ActorId);
        p.ClearSession();Assert.Equal(0,p.Snapshot(21201).Outgoing);
        p.Observe(new DamageEvent(22000,1,2,1,10,0));Assert.Equal(2u,p.Snapshot(22000).ActorId);
        p.Observe(new SummonSeenEvent(23000,2,1,null,1));Assert.Equal(0u,p.Snapshot(23000).ActorId);
    }
    [Fact] public void OtherAttackersDoNotKeepAnOldOpponentFresh()
    {
        var p=Setup();p.Observe(new DamageEvent(1200,1,2,1,100,0));
        p.Observe(new PlayerSeenEvent(20000,3,"Third",1,GameClass.Chanter));
        p.Observe(new DamageEvent(21000,3,1,1,30,0));Assert.Equal(0u,p.Snapshot(21201).ActorId);
    }
}
