using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
namespace AionMeter.Tests;

public class AutomaticFightEndTests
{
    private static CombatTracker Fight(bool automatic=true)
    {
        var tracker=new CombatTracker(GameData.Empty,new MeterOptions { PartyOnly=true,TargetMode=TargetMode.All,IdleTimeoutMs=10000,BossIdleTimeoutMs=30000,AutomaticFightEnd=automatic });
        tracker.Process(new SelfIdentifiedEvent(1000,1,"Own",2305,GameClass.Cleric));
        tracker.Process(new NpcSeenEvent(1000,500,999,90000000));
        tracker.Process(new DamageEvent(1000,1,500,17010000,100,HitFlags.None));return tracker;
    }
    [Theory] [InlineData(true)] [InlineData(false)]
    public void Healing_and_incoming_damage_keep_same_fight_active(bool healing)
    {
        var t=Fight();var id=t.Snapshot(null,1000)!.Id;
        if(healing)t.Process(new HealEvent(30000,1,1,17010000,500,HitFlags.None));
        else t.Process(new DamageEvent(30000,500,1,1000000,500,HitFlags.None));
        t.Tick(40000);Assert.True(t.Snapshot(null,40000)!.IsActive);Assert.Equal(id,t.Snapshot(null,40000)!.Id);
        t.Tick(61000);var finished=t.Snapshot(null,61000)!;Assert.False(finished.IsActive);Assert.Equal(EncounterEndReason.Idle,finished.Reason);Assert.Equal(id,finished.Id);
        Assert.Equal(id,t.Snapshot(null,90000)!.Id);
        t.Process(new DamageEvent(91000,1,500,17010000,100,HitFlags.None));Assert.NotEqual(id,t.Snapshot(null,91000)!.Id);
    }
    [Fact] public void Foreign_healing_cannot_extend_group_fight()
    {
        var t=Fight();t.Process(new PlayerSeenEvent(2000,2,"Stranger",2305,GameClass.Cleric));
        t.Process(new HealEvent(30000,2,2,17010000,500,HitFlags.None));t.Tick(32000);
        Assert.False(t.Snapshot(null,32000)!.IsActive);
    }
    [Fact] public void Boss_hp_changes_keep_phase_active_but_repeated_identical_hp_does_not()
    {
        var t=Fight();t.Process(new NpcHpEvent(2000,500,89000000,90000000));
        t.Process(new NpcHpEvent(30000,500,88000000,90000000));t.Tick(40000);Assert.True(t.Snapshot(null,40000)!.IsActive);
        t.Process(new NpcHpEvent(60000,500,88000000,90000000));t.Tick(61000);Assert.False(t.Snapshot(null,61000)!.IsActive);
    }
    [Fact] public void Disabling_inactivity_end_does_not_disable_confirmed_boss_death()
    {
        var t=Fight(false);t.Tick(100000);Assert.True(t.Snapshot(null,100000)!.IsActive);
        t.Process(new NpcHpEvent(100001,500,0,90000000));var finished=t.Snapshot(null,100002)!;
        Assert.False(finished.IsActive);Assert.Equal(EncounterEndReason.Kill,finished.Reason);
    }
    [Fact] public void Non_boss_fight_ends_only_when_all_attacked_targets_are_confirmed_dead()
    {
        var t=new CombatTracker(GameData.Empty,new MeterOptions { TargetMode=TargetMode.All });
        t.Process(new SelfIdentifiedEvent(1000,1,"Own",2305,GameClass.Cleric));
        foreach(var id in new[]{500u,501u})
        {t.Process(new NpcSeenEvent(1000,id,0,10000));t.Process(new DamageEvent(1001,1,id,17010000,100,HitFlags.None));}
        t.Process(new NpcHpEvent(2000,500,0,10000));Assert.True(t.Snapshot(null,2001)!.IsActive);
        t.Process(new DeathEvent(3000,501));var ended=t.Snapshot(null,3001)!;
        Assert.False(ended.IsActive);Assert.Equal(EncounterEndReason.Kill,ended.Reason);
        t.Process(new DamageEvent(4000,1,501,17010000,100,HitFlags.Dot));Assert.Equal(ended.Id,t.Snapshot(null,4001)!.Id);
    }
}
