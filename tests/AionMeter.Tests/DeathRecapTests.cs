using AionMeter.Core;
using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Protocol;

namespace AionMeter.Tests;

public class DeathRecapTests
{
    private static CombatTracker New()
    {
        var tracker = new CombatTracker(GameData.Empty, new MeterOptions { PartyOnly = true });
        tracker.Process(new SelfIdentifiedEvent(1000, 1, "Own", 2305, GameClass.Cleric));
        tracker.Process(new NpcSeenEvent(1100, 10, 123, Name: "Enemy"));
        return tracker;
    }
    private static DamageEvent Hit(long at, long damage = 100, uint target = 1) => new(at, 10, target, 123, damage, HitFlags.None);

    [Fact]
    public void Shows_only_own_received_hits_in_the_ten_seconds_before_death_even_without_outgoing_fight()
    {
        var t = New(); t.Process(Hit(1999)); t.Process(Hit(2000, 200)); t.Process(Hit(11000, 300));
        t.Process(Hit(11500, target: 2)); t.Process(Hit(11600, 0));
        Assert.Null(t.DeathRecap()); t.Process(new DeathEvent(12000, 1));
        var recap = t.DeathRecap()!;
        Assert.Equal(new long[] { 300, 200 }, recap.Hits.Select(h => h.Damage));
        Assert.All(recap.Hits, h => Assert.Equal("Enemy", h.SourceName));
        t.Process(new DeathEvent(12100, 1)); t.Process(Hit(12200));
        Assert.Equal(12000, t.DeathRecap()!.DiedAtMs); Assert.Equal(2, t.DeathRecap()!.Hits.Count);
    }
    [Fact]
    public void Fresh_positive_own_hp_hides_report_and_next_death_has_no_previous_life_hits()
    {
        var t = New(); t.Process(Hit(2000)); t.Process(new DeathEvent(2100, 1));
        t.Process(new NpcHpEvent(2200, 2, 100, 100, false)); Assert.NotNull(t.DeathRecap());
        t.Process(new NpcHpEvent(2000, 1, 100, 100, false)); Assert.NotNull(t.DeathRecap());
        t.Process(new HealEvent(2250, 2, 1, 1, 100, HitFlags.None)); Assert.NotNull(t.DeathRecap());
        t.Process(new NpcHpEvent(2300, 1, 100, 100, false)); Assert.Null(t.DeathRecap());
        t.Process(Hit(2400, 50)); t.Process(new DeathEvent(2500, 1));
        Assert.Equal(50, Assert.Single(t.DeathRecap()!.Hits).Damage);
    }
    [Fact]
    public void Other_deaths_and_already_dead_spawns_do_not_open_report_and_empty_deaths_are_honest()
    {
        var t = New(); t.Process(new DeathEvent(2000, 2)); t.Process(new DeathEvent(2000, 1, true));
        Assert.Null(t.DeathRecap()); t.Process(new DeathEvent(2100, 1)); Assert.Empty(t.DeathRecap()!.Hits);
    }
    [Fact]
    public void Zero_own_hp_detects_death_and_identity_or_capture_change_clears_it()
    {
        var t = New(); t.Process(Hit(2000)); t.Process(new NpcHpEvent(2100, 1, 0, 100, false));
        Assert.NotNull(t.DeathRecap());
        t.Process(new SelfIdentifiedEvent(2200, 2, "Alt", 2305, GameClass.Cleric)); Assert.Null(t.DeathRecap());
        t.Process(new DeathEvent(2300, 2)); Assert.NotNull(t.DeathRecap());
        t.InvalidateParty(); Assert.Null(t.DeathRecap()); t.Process(new DeathEvent(2400, 2)); Assert.Null(t.DeathRecap());
    }
    [Fact]
    public void Remaining_player_hp_packet_closes_report_through_the_real_decoder()
    {
        var t = New();
        t.Process(new SelfIdentifiedEvent(1300, 9492, "Own", 2305, GameClass.Cleric));
        t.Process(new DamageEvent(2000, 10, 9492, 123, 100, HitFlags.None));
        t.Process(new DeathEvent(2100, 9492)); Assert.NotNull(t.DeathRecap());
        var parser = new PacketParser(GameData.Empty, t.Process) { TimeMs = 3000 };
        parser.Handle(Opcodes.RemainHp, Wire.FromHex("944a01010304040200"));
        Assert.NotNull(t.DeathRecap()); // Other resources do not prove that the dead player has HP.
        parser.Handle(Opcodes.RemainHp, Wire.FromHex("944a020100812e000000000000"));
        Assert.Null(t.DeathRecap());
    }
    [Fact]
    public void Batch_hp_closes_recap_without_map_load_and_allows_the_next_life()
    {
        var t = New();
        t.Process(new SelfIdentifiedEvent(1300, 5958, "Own", 2305, GameClass.Cleric));
        t.Process(new DamageEvent(2000, 10, 5958, 123, 100, HitFlags.None));
        t.Process(new DeathEvent(2100, 5958)); Assert.NotNull(t.DeathRecap());
        var parser = new PacketParser(GameData.Empty, t.Process) { TimeMs = 3000 };
        parser.Handle(Opcodes.RemainHp, Wire.FromHex(
            "c62e0306011c120000020000000003142b0200040000000005a086010006b00009000100d42e000000000000"));
        Assert.Null(t.DeathRecap());
        t.Process(new DamageEvent(3200, 10, 5958, 123, 50, HitFlags.None));
        t.Process(new DeathEvent(3300, 5958));
        Assert.Equal(50, Assert.Single(t.DeathRecap()!.Hits).Damage);
    }
    [Fact]
    public void Respawn_reload_of_same_map_clears_report_and_old_damage()
    {
        var t = New(); t.Process(new ZoneChangedEvent(1200, 1, "Dungeon", true));
        t.Process(new SelfIdentifiedEvent(1300, 1, "Own", 2305, GameClass.Cleric));
        t.Process(Hit(2000)); t.Process(new DeathEvent(2100, 1)); Assert.NotNull(t.DeathRecap());
        t.Process(new ZoneChangedEvent(3000, 1, "Dungeon", true, IsTeleport: true));
        Assert.Null(t.DeathRecap());
        t.Process(Hit(3200, 50)); t.Process(new DeathEvent(3300, 1));
        Assert.Equal(50, Assert.Single(t.DeathRecap()!.Hits).Damage);
    }
    [Fact]
    public void Delayed_actions_dots_pet_and_foreign_actions_do_not_fake_a_resurrection()
    {
        var t = New(); t.Process(Hit(2000)); t.Process(new DeathEvent(2100, 1));
        t.Process(new CastEvent(2000, 1, 10, 17010000));
        t.Process(new DamageEvent(2200, 1, 10, 17010000, 100, HitFlags.Dot));
        t.Process(new DamageEvent(2250, 1, 10, 17010000, 100, HitFlags.None)); // An airborne hit may land after death.
        t.Process(new SummonSeenEvent(2300, 3, 1, null, 0));
        t.Process(new CastEvent(2400, 3, 10, 17010000));
        t.Process(new DamageEvent(2500, 3, 10, 17010000, 100, HitFlags.None));
        t.Process(new CastEvent(2600, 2, 10, 17010000));
        t.Process(new HealEvent(2700, 1, 1, 17010000, 100, HitFlags.None));
        Assert.NotNull(t.DeathRecap());
    }
    [Fact]
    public void Buffer_is_bounded_and_reports_truncation()
    {
        var t = New(); for (var i = 0; i < 4200; i++) t.Process(Hit(2000 + i));
        t.Process(new DeathEvent(6300, 1)); Assert.Equal(4096, t.DeathRecap()!.Hits.Count); Assert.True(t.DeathRecap()!.Truncated);
    }
}
