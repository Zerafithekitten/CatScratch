using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;

namespace AionMeter.Tests;

public class PassiveIdentityTests
{
    [Fact] public void Conflicting_partial_character_cannot_inherit_old_stats()
    {
        var s = Ready(); var fresh = s.Observe(new SelfIdentifiedEvent(3, 12, "", 2306, GameClass.Assassin));
        Assert.Null(s.Current); Assert.Equal("", fresh!.Name); Assert.Equal(0, fresh.CombatPower);
    }
    private static LiveIdentity Ready()
    {
        var state = new LiveIdentity();
        state.Observe(new ZoneChangedEvent(1, 1110, "World", false));
        state.Observe(new SelfIdentifiedEvent(2, 12, "Self", 2305, GameClass.Cleric, 45, 85471, 1894));
        return state;
    }
    [Fact] public void Name_announcement_after_loading_rebinds_the_verified_character_without_teleport()
    {
        var s = Ready(); s.Observe(new ZoneChangedEvent(3, 600001, "Dungeon", true));
        Assert.Null(s.ActorId); Assert.Equal("Self", s.KnownCharacter!.Name);
        var fresh = s.Observe(new PlayerSeenEvent(4, 25, "Self", 2305, GameClass.Cleric));
        Assert.Equal((uint)25, fresh!.ActorId); Assert.Equal(1894, fresh.GearScore);
        Assert.Equal((uint)25, s.ActorId);
    }
    [Theory] [InlineData("Stranger", 2305)] [InlineData("Self", 2306)] [InlineData("Self", 0)]
    public void Name_or_server_mismatch_cannot_rebind_after_loading(string name, int server)
    {
        var s = Ready(); s.Observe(new ZoneChangedEvent(3, 600001, "Dungeon", true));
        Assert.Null(s.Observe(new PlayerSeenEvent(4, 25, name, server, GameClass.Cleric)));
        Assert.Null(s.ActorId);
    }
    [Fact] public void Partial_same_actor_records_keep_name_and_stats()
    {
        var s = Ready(); var fresh = s.Observe(new SelfIdentifiedEvent(3, 12, "", 0, GameClass.Unknown));
        Assert.Equal("Self", fresh!.Name); Assert.Equal(85471, fresh.CombatPower);
        Assert.Equal(2305, fresh.ServerId); Assert.Equal("Self", s.Current!.Name);
    }
    [Fact] public void Newly_announced_own_actor_can_learn_its_name_without_another_teleport()
    {
        var s = Ready(); s.Observe(new SelfIdentifiedEvent(3, 28, "", 0, GameClass.Unknown));
        Assert.Null(s.Current);
        var fresh = s.Observe(new PlayerSeenEvent(4, 28, "Second", 2306, GameClass.Assassin));
        Assert.Equal("Second", fresh!.Name); Assert.Equal(0, fresh.CombatPower);
    }
    [Fact] public void Capture_restart_keeps_metadata_but_never_reuses_an_old_actor_without_fresh_proof()
    {
        var s = Ready(); s.Suspend(); Assert.Null(s.ActorId); Assert.NotNull(s.KnownCharacter);
        Assert.NotNull(s.Observe(new PlayerSeenEvent(4, 28, "Self", 2305, GameClass.Cleric)));
        s.Reset(); Assert.Null(s.Observe(new PlayerSeenEvent(5, 29, "Self", 2305, GameClass.Cleric)));
    }
    [Fact] public void Rebound_self_damage_counts_and_reissued_old_id_does_not()
    {
        var identity = new LiveIdentity();
        var tracker = new CombatTracker(GameData.Empty, new MeterOptions { PartyOnly = true, TargetMode = TargetMode.All });
        void Feed(GameEvent e)
        {
            var fresh = identity.Observe(e);
            tracker.Process(e is SelfIdentifiedEvent && fresh is not null ? fresh : e);
            if (e is PlayerSeenEvent && fresh is not null) tracker.Process(fresh);
        }
        Feed(new ZoneChangedEvent(1, 1110, "World", false));
        Feed(new SelfIdentifiedEvent(2, 12, "Self", 2305, GameClass.Cleric));
        Feed(new ZoneChangedEvent(3, 600001, "Dungeon", true));
        Feed(new DamageEvent(4, 12, 500, 17010000, 999, HitFlags.None));
        Feed(new PlayerSeenEvent(5, 25, "Self", 2305, GameClass.Cleric));
        Feed(new DamageEvent(6, 25, 500, 17010000, 100, HitFlags.None));
        Assert.Equal(100, tracker.Snapshot(null, 7)!.TotalDamage);
    }
    [Fact] public void In_map_teleports_keep_the_confirmed_actor()
    {
        var s = Ready(); s.Observe(new ZoneChangedEvent(3, 1110, "World", false, true));
        Assert.Equal((uint)12, s.ActorId);
    }
}
