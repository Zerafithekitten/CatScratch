using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Protocol;
using AionMeter.Core.Storage;
using AionMeter.Core;
using AionMeter.Core.Combat;

namespace AionMeter.Tests;

public sealed class RobustnessTests
{
    [Fact]
    public void Known_packet_readers_tolerate_short_and_random_payloads()
    {
        var random = new Random(254);
        var opcodes = typeof(Opcodes).GetFields().Where(f => f.IsLiteral && f.FieldType == typeof(ushort))
            .Select(f => (ushort)f.GetRawConstantValue()!).ToArray();
        foreach (var opcode in opcodes)
        {
            var parser = new PacketParser(GameData.Empty, _ => { });
            for (var length = 0; length < 256; length++)
            {
                var bytes = new byte[length]; random.NextBytes(bytes);
                var error = Record.Exception(() => parser.Handle(opcode, bytes));
                Assert.True(error is null, $"Opcode {opcode:x4}, length {length}: {error}");
            }
        }
    }

    [Fact]
    public void Gear_for_same_named_players_stays_on_their_own_server()
    {
        var entities = new EntityRegistry(GameData.Empty);
        var first = entities.UpsertPlayer(1, "SameName", 2305, GameClass.Cleric);
        var second = entities.UpsertPlayer(2, "SameName", 2306, GameClass.Cleric);
        entities.SetGear("SameName", 1800, 80000, 2305);
        entities.SetGear("SameName", 2200, 110000, 2306);
        Assert.Equal((1800, 80000L), (first.GearScore, first.CombatPower));
        Assert.Equal((2200, 110000L), (second.GearScore, second.CombatPower));
    }

    [Fact]
    public void An_unknown_server_does_not_remove_a_known_same_named_player()
    {
        var entities = new EntityRegistry(GameData.Empty);
        entities.UpsertPlayer(1,"SameName",2305,GameClass.Cleric);
        entities.UpsertPlayer(2,"SameName",0,GameClass.Cleric);
        Assert.Equal(2,entities.Export().Players.Count);
        entities.UpsertPlayer(2,"SameName",2306,GameClass.Cleric);
        Assert.Equal(2,entities.Export().Players.Count);
    }

    [Fact]
    public void Reused_actor_does_not_keep_the_previous_players_gear()
    {
        var entities = new EntityRegistry(GameData.Empty);
        entities.UpsertPlayer(1, "Old", 2305, GameClass.Cleric);
        entities.SetGear("Old", 1800, 80000, 2305);
        var next = entities.UpsertPlayer(1, "New", 2306, GameClass.Gladiator);
        Assert.Equal((0, 0L), (next.GearScore, next.CombatPower));
        entities.SetGear("New", 2200, 110000, 2306);
        Assert.Equal((2200, 110000L), (next.GearScore, next.CombatPower));
    }

    [Fact]
    public void Server_specific_gear_survives_cache_and_late_server_identity()
    {
        var entities = new EntityRegistry(GameData.Empty);
        entities.Import(new SessionState(null, 1,
            [new(1,"SameName",GameClass.Cleric,2305,1800,80000),new(2,"SameName",GameClass.Cleric,2306,2200,110000)], []));
        var first = entities.UpsertPlayer(3,"SameName",2305,GameClass.Cleric);
        var second = entities.UpsertPlayer(4,"SameName",2306,GameClass.Cleric);
        Assert.Equal((1800,80000L), (first.GearScore, first.CombatPower));
        Assert.Equal((2200,110000L), (second.GearScore, second.CombatPower));
        var delayed = entities.UpsertPlayer(5,"Delayed",0,GameClass.Cleric);
        entities.SetGear("Delayed",2000,90000,2305);
        Assert.Equal(0,delayed.CombatPower);
        entities.UpsertPlayer(5,"Delayed",2305,GameClass.Cleric);
        Assert.Equal((2000,90000L), (delayed.GearScore,delayed.CombatPower));
    }

    [Fact]
    public void Reused_actor_also_clears_old_gear_in_the_displayed_fight()
    {
        var tracker = new CombatTracker(GameData.Empty, new MeterOptions { TargetMode = TargetMode.All });
        tracker.Process(new SelfIdentifiedEvent(0, 1, "Old", 2305, GameClass.Cleric,45,80000,1800));
        tracker.Process(new DamageEvent(100,1,500,17010000,100,HitFlags.None));
        Assert.Equal(80000, Assert.Single(tracker.Snapshot(null,200)!.Combatants).CombatPower);
        tracker.Process(new PlayerSeenEvent(300,1,"New",2306,GameClass.Gladiator));
        var row = Assert.Single(tracker.Snapshot(null,400)!.Combatants);
        Assert.Equal("New",row.Name);
        Assert.Equal((0,0L),(row.GearScore,row.CombatPower));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[null,{\"Id\":1,\"Name\":null}]")]
    public void Invalid_name_cache_entries_do_not_break_startup(string players)
    {
        var path = Path.Combine(Path.GetTempPath(), "catscratch-cache-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, "{\"SavedAt\":\"" + DateTimeOffset.UtcNow.ToString("O") + "\",\"Players\":" + players + ",\"Npcs\":[null]}");
            var state = new NameCache(path).Load();
            if (state is not null)
            {
                Assert.Empty(state.Players); Assert.Empty(state.Npcs);
                new EntityRegistry(GameData.Empty).Import(state);
            }
        }
        finally { File.Delete(path); }
    }
}
