using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Protocol;

namespace AionMeter.Tests;

public class EffectiveHealingTests
{
    private static CombatTracker Start()
    {
        var tracker=new CombatTracker(GameData.Empty,new MeterOptions{TargetMode=TargetMode.All});
        tracker.Process(new SelfIdentifiedEvent(0,1,"Healer",1,GameClass.Cleric));
        tracker.Process(new PlayerSeenEvent(0,2,"Ally",1,GameClass.Gladiator));
        tracker.Process(new PlayerSeenEvent(0,3,"Other healer",1,GameClass.Cleric));
        tracker.Process(new NpcSeenEvent(0,9,2300243,1000000));
        tracker.Process(new DamageEvent(1000,1,9,17010000,100,HitFlags.None));return tracker;
    }
    private static CombatantSnapshot Own(CombatTracker tracker)=>tracker.Snapshot(null,1500)!.Combatants.Single(c=>c.IsSelf);

    [Theory]
    [InlineData(5000,10000,78500,10000,5000)]
    [InlineData(10000,10000,78500,10000,0)]
    [InlineData(5000,10000,1000,6000,1000)]
    public void Self_healing_counts_only_restored_hp_not_overhealing(long before,long max,long raw,long after,long effective)
    {
        var tracker=Start();tracker.Process(new NpcHpEvent(1100,1,before,max,false));
        tracker.Process(new HealEvent(1200,1,1,17010000,raw,HitFlags.None));
        tracker.Process(new NpcHpEvent(1250,1,after,max,false));
        var own=Own(tracker);
        Assert.Equal(effective,own.EffectiveSelfHealing);Assert.Equal(0,own.UnverifiedSelfHealing);
        Assert.Equal(raw,own.SelfHealing);Assert.Equal(raw,own.Healing);Assert.Equal(0,own.EffectiveHealingOthers);
    }

    [Fact]
    public void Self_healing_without_hp_confirmation_is_not_estimated()
    {
        var tracker=Start();tracker.Process(new HealEvent(1200,1,1,17010000,1000,HitFlags.None));
        Assert.Equal(0,Own(tracker).EffectiveSelfHealing);Assert.Equal(1000,Own(tracker).UnverifiedSelfHealing);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Self_healing_ignores_intervals_with_other_healers_or_damage(bool otherHealer)
    {
        var tracker=Start();tracker.Process(new NpcHpEvent(1100,1,5000,10000,false));
        tracker.Process(new HealEvent(1200,1,1,17010000,1000,HitFlags.None));
        if(otherHealer)tracker.Process(new HealEvent(1210,3,1,17010000,1000,HitFlags.None));
        else tracker.Process(new DamageEvent(1210,9,1,2300001,500,HitFlags.None));
        tracker.Process(new NpcHpEvent(1250,1,otherHealer?7000:5500,10000,false));
        Assert.Equal(0,Own(tracker).EffectiveSelfHealing);Assert.Equal(1000,Own(tracker).UnverifiedSelfHealing);
    }

    [Fact]
    public void Self_healing_ticks_share_the_confirmed_gain_once()
    {
        var tracker=Start();tracker.Process(new NpcHpEvent(1100,1,9000,10000,false));
        tracker.Process(new HealEvent(1200,1,1,17090000,5000,HitFlags.Dot));
        tracker.Process(new HealEvent(1200,1,1,17090000,7000,HitFlags.Dot));
        tracker.Process(new NpcHpEvent(1250,1,10000,10000,false));
        tracker.Process(new NpcHpEvent(1260,1,10000,10000,false));
        Assert.Equal(1000,Own(tracker).EffectiveSelfHealing);Assert.Equal(0,Own(tracker).UnverifiedSelfHealing);
    }

    [Theory]
    [InlineData(5726,5790,959,5790,64)] // anonymized HP/amount sequence from the supplied boss recording
    [InlineData(2668,4209,939,3607,939)]
    [InlineData(3607,4209,2761,4209,602)]
    [InlineData(4209,4209,5633,4209,0)]
    public void Only_confirmed_hp_gain_is_counted(long before,long max,long raw,long after,long effective)
    {
        var tracker=Start();tracker.Process(new NpcHpEvent(1100,2,before,max,false));
        tracker.Process(new HealEvent(1200,1,2,17800000,raw,HitFlags.None));
        tracker.Process(new NpcHpEvent(1250,2,after,0,false));
        var own=Own(tracker);Assert.Equal(effective,own.EffectiveHealingOthers);Assert.Equal(0,own.UnverifiedHealingOthers);
        Assert.Equal(raw,own.Healing);Assert.Equal(100,own.Damage);
    }

    [Theory]
    [InlineData(false,1250)]
    [InlineData(true,1600)]
    public void Missing_or_late_hp_is_unverified_not_reported_as_zero_healing(bool baseline,long confirmation)
    {
        var tracker=Start();if(baseline)tracker.Process(new NpcHpEvent(1100,2,5000,10000,false));
        tracker.Process(new HealEvent(1200,1,2,17800000,1000,HitFlags.None));
        tracker.Process(new NpcHpEvent(confirmation,2,6000,10000,false));
        Assert.Equal(0,Own(tracker).EffectiveHealingOthers);Assert.Equal(1000,Own(tracker).UnverifiedHealingOthers);
    }

    [Fact]
    public void Unexplained_small_hp_gains_are_not_counted_as_the_cast_heal()
    {
        var tracker=Start();tracker.Process(new NpcHpEvent(1100,2,5000,10000,false));
        tracker.Process(new HealEvent(1200,1,2,17800000,1000,HitFlags.None));
        tracker.Process(new NpcHpEvent(1250,2,5004,10000,false));
        Assert.Equal(0,Own(tracker).EffectiveHealingOthers);Assert.Equal(1000,Own(tracker).UnverifiedHealingOthers);
    }

    [Fact]
    public void Multiple_healers_in_one_hp_interval_are_not_arbitrarily_attributed()
    {
        var tracker=Start();tracker.Process(new NpcHpEvent(1100,2,5000,10000,false));
        tracker.Process(new HealEvent(1200,1,2,17800000,1000,HitFlags.None));
        tracker.Process(new HealEvent(1210,3,2,17800000,1000,HitFlags.None));
        tracker.Process(new NpcHpEvent(1250,2,7000,10000,false));
        Assert.Equal(0,Own(tracker).EffectiveHealingOthers);Assert.Equal(1000,Own(tracker).UnverifiedHealingOthers);
    }

    [Fact]
    public void Damage_during_the_hp_interval_and_resurrection_gains_are_excluded()
    {
        var tracker=Start();tracker.Process(new NpcHpEvent(1100,2,5000,10000,false));
        tracker.Process(new DamageEvent(1150,9,2,2300001,500,HitFlags.None));
        tracker.Process(new HealEvent(1200,1,2,17800000,1000,HitFlags.None));
        tracker.Process(new NpcHpEvent(1250,2,5500,10000,false));
        tracker.Process(new NpcHpEvent(1300,2,0,10000,false));
        tracker.Process(new HealEvent(1350,1,2,17800000,1000,HitFlags.None));
        tracker.Process(new NpcHpEvent(1400,2,1000,10000,false));
        Assert.Equal(0,Own(tracker).EffectiveHealingOthers);Assert.Equal(2000,Own(tracker).UnverifiedHealingOthers);
    }

    [Fact]
    public void Same_healer_ticks_share_one_confirmed_gain_without_double_counting()
    {
        var tracker=Start();tracker.Process(new NpcHpEvent(1100,2,9000,10000,false));
        tracker.Process(new HealEvent(1200,1,2,17090000,5000,HitFlags.Dot));
        tracker.Process(new HealEvent(1200,1,2,17090000,7000,HitFlags.Dot));
        tracker.Process(new NpcHpEvent(1250,2,10000,10000,false));
        tracker.Process(new NpcHpEvent(1260,2,10000,10000,false));
        Assert.Equal(1000,Own(tracker).EffectiveHealingOthers);Assert.Equal(0,Own(tracker).UnverifiedHealingOthers);
    }

    [Theory]
    [InlineData(17800000,true)]
    [InlineData(17120000,true)]
    [InlineData(16770000,false)] // spirit links are not HP healing
    public void Instant_group_heals_are_decoded_as_heals_without_converting_spirit_links(int skill,bool healing)
    {
        var data=GameData.Empty;data.HealSkills.Add(skill);
        var body=Wire.FromHex("b1ea011600f30a40c0f40063028000010b199b5f01000000ac52d007");
        BitConverter.GetBytes((uint)skill).CopyTo(body,7);
        var events=new List<GameEvent>();new PacketParser(data,events.Add).Handle(Opcodes.Damage,body);
        if(healing){var heal=Assert.IsType<HealEvent>(Assert.Single(events));Assert.Equal(976,heal.Amount);Assert.NotEqual(heal.SourceId,heal.TargetId);}
        else Assert.IsType<DamageEvent>(Assert.Single(events));
    }
}
