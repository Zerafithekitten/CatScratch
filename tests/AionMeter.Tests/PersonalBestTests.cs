using System.Text.Json;
using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Storage;

namespace AionMeter.Tests;

public sealed class PersonalBestTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "catscratch-bests-" + Guid.NewGuid().ToString("N"));
    private static GameData Data()
    {
        var data = GameData.Empty;
        data.Npcs[900] = new(900, "Boss", true, false);
        data.Npcs[901] = new(901, "Dummy", true, true);
        data.Npcs[902] = new(902, "Trash", false, false);
        return data;
    }
    private static CombatTracker Tracker(int code = 900)
    {
        var tracker = new CombatTracker(Data(), new MeterOptions { TargetMode = TargetMode.All, PartyOnly = true, AutomaticFightEnd = false });
        tracker.Process(new ZoneChangedEvent(900, 600001, "Dungeon", true));
        tracker.Process(new SelfIdentifiedEvent(1000, 1, "Own", 2305, GameClass.Cleric));
        tracker.Process(new NpcSeenEvent(1000, 500, code, 10000));
        tracker.Process(new NpcHpEvent(1000, 500, 10000, 10000));
        tracker.Process(new DamageEvent(1100, 1, 500, 17010000, 100, HitFlags.None));
        return tracker;
    }
    private static FightRecord Kill(int code = 900, int damage = 900)
    {
        var tracker = Tracker(code);
        tracker.Process(new DamageEvent(2100, 1, 500, 17010000, damage, HitFlags.None));
        tracker.Process(new DeathEvent(2200, 500));
        return tracker.Record(null)!;
    }

    [Fact]
    public void Deaths_are_deduplicated_until_confirmed_alive_and_preserved_in_saved_fight()
    {
        var tracker = Tracker();
        tracker.Process(new DeathEvent(1300, 1));
        tracker.Process(new DeathEvent(1400, 1));
        tracker.Process(new DeathEvent(1500, 1, AlreadyDead: true));
        tracker.Process(new NpcHpEvent(1600, 1, 100, 100, IsNpc: false));
        tracker.Process(new DeathEvent(1800, 1));
        tracker.Process(new DeathEvent(1900, 500));
        var record = tracker.Record(null)!;
        Assert.Equal(2, record.Summary.Combatants.Single(c => c.IsSelf).Deaths);
        Assert.Equal(new[] { 200, 700 }, record.Details.Single(c => c.IsSelf).DeathOffsetsMs);
        var history = new HistoryStore(_directory);
        var reread = history.Load(history.Save(record))!;
        Assert.Equal(2, reread.Summary.Combatants.Single(c => c.IsSelf).Deaths);
        Assert.Equal(new[] { 200, 700 }, reread.Details.Single(c => c.IsSelf).DeathOffsetsMs);
    }

    [Fact]
    public void Seen_dead_foreign_players_and_deaths_outside_combat_do_not_count()
    {
        var tracker = Tracker();
        tracker.Process(new PlayerSeenEvent(1200, 2, "Foreign", 2305, GameClass.Cleric));
        tracker.Process(new DeathEvent(1300, 2));
        tracker.Process(new DeathEvent(1400, 1, true));
        tracker.Process(new DeathEvent(1500, 500));
        tracker.Process(new DeathEvent(1600, 1));
        Assert.Equal(0, tracker.Record(null)!.Summary.Combatants.Single(c => c.IsSelf).Deaths);
        Assert.DoesNotContain(tracker.Record(null)!.Summary.Combatants, c => c.ActorId == 2);
    }

    [Fact]
    public void Respawn_entity_change_keeps_deaths_and_counts_next_death()
    {
        var tracker = Tracker();
        tracker.Process(new DeathEvent(1300, 1));
        tracker.Process(new SelfIdentifiedEvent(1400, 3, "Own", 2305, GameClass.Cleric));
        tracker.Process(new DamageEvent(1500, 3, 500, 17010000, 100, HitFlags.None));
        tracker.Process(new DeathEvent(1600, 3));
        tracker.Process(new DeathEvent(1700, 500));
        Assert.Equal(2, tracker.Record(null)!.Summary.Combatants.Single(c => c.IsSelf).Deaths);
    }

    [Fact]
    public void Legacy_fights_have_unknown_deaths_and_do_not_seed_records()
    {
        var json = JsonSerializer.Serialize(Kill()).Replace(",\"Deaths\":0", "");
        var legacy = JsonSerializer.Deserialize<FightRecord>(json)!;
        Assert.Null(legacy.Summary.Combatants.Single(c => c.IsSelf).Deaths);
        Assert.Null(new PersonalBestStore(Data()).Record(legacy));
    }

    [Theory]
    [InlineData(901)]
    [InlineData(902)]
    [InlineData(999)]
    public void Dummies_trash_and_unknown_npcs_never_create_personal_records(int code)
    {
        var store = new PersonalBestStore(Data());
        Assert.Null(store.Record(Kill(code)));
        Assert.Empty(store.List());
    }

    [Fact]
    public void Eligible_boss_kill_sets_only_own_records_and_equal_results_keep_previous_record()
    {
        var store = new PersonalBestStore(Data());
        var first = Kill();
        var stats = store.Record(first)!;
        Assert.Equal(1000, stats.BestDamage!.Value);
        Assert.Equal(1000, stats.BestDps!.Value);
        Assert.Equal(900, stats.BestHit!.Value);
        Assert.Equal(0, stats.FewestDeaths!.Value);
        Assert.Null(store.Record(first));
        var same = Kill();
        Assert.Null(store.Record(same));
        var again = Assert.Single(store.List());
        Assert.Equal(first.Summary.Id, again.BestDps!.FightId);
        var stronger = store.Record(Kill(damage: 1200))!;
        Assert.Equal(1300, stronger.BestDamage!.Value);
    }

    [Theory]
    [InlineData(EncounterEndReason.Wipe)]
    [InlineData(EncounterEndReason.Idle)]
    [InlineData(EncounterEndReason.Manual)]
    [InlineData(EncounterEndReason.ZoneChange)]
    public void Unsuccessful_attempts_do_not_create_any_permanent_values(EncounterEndReason reason)
    {
        var record = Kill();
        var own = record.Summary.Combatants[0] with { Deaths = 1 };
        record = record with { Summary = record.Summary with { Reason = reason, Combatants = [own] },
            Details = [record.Details[0] with { DeathOffsetsMs = [500] }] };
        var store = new PersonalBestStore(Data());
        Assert.Null(store.Record(record));
        Assert.Empty(store.List());
    }

    [Theory]
    [InlineData("late")]
    [InlineData("unknownHP")]
    [InlineData("noBaseline")]
    [InlineData("mixedDamage")]
    [InlineData("captureLoss")]
    public void Incomplete_or_mixed_fights_never_set_records(string problem)
    {
        var record = Kill();
        var summary = record.Summary;
        summary = problem switch
        {
            "late" => summary with { Boss = summary.Boss! with { Uncounted = 100 } },
            "unknownHP" => summary with { Boss = summary.Boss! with { MaxHpKnown = false } },
            "noBaseline" => summary with { BossStartObserved = false },
            "mixedDamage" => summary with { BossOnlyDamage = false },
            _ => summary,
        };
        var store = new PersonalBestStore(Data());
        Assert.Null(store.Record(record with { Summary = summary }, problem != "captureLoss"));
        Assert.Empty(store.List());
    }

    [Fact]
    public void Real_mixed_target_damage_is_marked_ineligible()
    {
        var tracker = Tracker();
        tracker.Process(new NpcSeenEvent(1200, 501, 902, 500));
        tracker.Process(new DamageEvent(1300, 1, 501, 17010000, 100000, HitFlags.None));
        tracker.Process(new DeathEvent(1400, 500));
        var record = tracker.Record(null)!;
        Assert.False(record.Summary.BossOnlyDamage);
        Assert.Null(new PersonalBestStore(Data()).Record(record));
    }

    [Fact]
    public void Records_are_separate_per_character_server_class_but_combine_same_boss_across_maps()
    {
        var store = new PersonalBestStore(Data());
        store.Record(Kill());
        foreach (var variant in new[] { "name", "server", "class", "map" })
        {
            var r = Kill();
            var own = r.Summary.Combatants[0];
            own = variant switch { "name" => own with { Name = "Alt" }, "server" => own with { ServerId = 2306 },
                "class" => own with { Class = GameClass.Ranger }, _ => own };
            store.Record(r with { Summary = r.Summary with { Combatants = [own], MapId = variant == "map" ? 600002 : r.Summary.MapId } });
        }
        Assert.Equal(4, store.List().Count);
    }

    [Fact]
    public void Own_healing_and_incoming_damage_are_retained_even_for_a_healer_without_damage()
    {
        var tracker = Tracker();
        tracker.Options.PartyOnly = false;
        tracker.Process(new PlayerSeenEvent(1200, 2, "Party", 2305, GameClass.Cleric));
        tracker.Process(new HealEvent(1300, 1, 2, 17010000, 250, HitFlags.None));
        tracker.Process(new HealEvent(1400, 1, 1, 17010000, 150, HitFlags.None));
        tracker.Process(new DamageEvent(1500, 500, 1, 1000001, 180, HitFlags.None));
        tracker.Process(new HealEvent(1600, 2, 1, 17010000, 90000, HitFlags.None));
        tracker.Process(new DeathEvent(1700, 500));
        var stats = new PersonalBestStore(Data()).Record(tracker.Record(null)!)!;
        Assert.Equal(400, stats.BestHealing!.Value);
        Assert.Equal(180, stats.LeastDamageTaken!.Value);
        var r = Kill();
        var own = r.Summary.Combatants[0] with { Damage = 0, Dps = 0, MaxHit = 0, Healing = 700, DamageTaken = 90 };
        stats = new PersonalBestStore(Data()).Record(r with { Summary = r.Summary with { Combatants = [own] } })!;
        Assert.Equal(700, stats.BestHealing!.Value);
        Assert.Equal(0, stats.BestDps!.Value);
        Assert.Equal(0, stats.BestDamage!.Value);
        Assert.Equal(90, stats.LeastDamageTaken!.Value);
    }

    [Fact]
    public void Records_survive_restart_and_over_one_hundred_later_kills_without_history()
    {
        var path = Path.Combine(_directory, "records.json");
        var store = new PersonalBestStore(Data(), path);
        var best = Kill(damage: 50000);
        store.Record(best);
        for (var i = 0; i < 150; i++)
        {
            var r = Kill();
            store.Record(r with { Summary = r.Summary with { StartedAt = best.Summary.StartedAt.AddMinutes(i + 1) } });
        }
        var stats = Assert.Single(new PersonalBestStore(Data(), path).List());
        Assert.Equal(best.Summary.Id, stats.BestDps!.FightId);
        using var saved = JsonDocument.Parse(File.ReadAllText(path));
        var item = Assert.Single(saved.RootElement.GetProperty("Records").EnumerateArray());
        Assert.False(item.TryGetProperty("RecentAttempts", out _));
        Assert.False(item.TryGetProperty("Attempts", out _));
        Assert.False(item.TryGetProperty("Kills", out _));
    }

    [Fact]
    public void Each_statistic_keeps_its_own_best_kill_and_zero_deaths_is_permanent()
    {
        FightRecord Values(double dps, long damage, long hit, long healing, int deaths, long taken)
        {
            var r = Kill(); var own = r.Summary.Combatants[0] with { Dps = dps, Damage = damage, MaxHit = hit, Healing = healing, Deaths = deaths, DamageTaken = taken };
            return r with { Summary = r.Summary with { Combatants = [own] }, Details = [r.Details[0] with { DeathOffsetsMs = Enumerable.Range(1, deaths).Select(i => i * 100).ToArray() }] };
        }
        var store = new PersonalBestStore(Data(), Path.Combine(_directory, "records.json"));
        var first = Values(1000, 2000, 500, 3000, 3, 100);
        var second = Values(800, 4000, 450, 5000, 1, 250);
        store.Record(first); var stats = store.Record(second)!;
        Assert.Equal(first.Summary.Id, stats.BestDps!.FightId);
        Assert.Equal(second.Summary.Id, stats.BestDamage!.FightId);
        Assert.Equal(first.Summary.Id, stats.BestHit!.FightId);
        Assert.Equal(second.Summary.Id, stats.BestHealing!.FightId);
        Assert.Equal(1, stats.FewestDeaths!.Value);
        Assert.Equal(100, stats.LeastDamageTaken!.Value);
        store.Record(Values(1, 1, 1, 0, 0, 0));
        Assert.Null(store.Record(Values(1, 1, 1, 0, 2, 50)));
        stats = Assert.Single(new PersonalBestStore(Data(), Path.Combine(_directory, "records.json")).List());
        Assert.Equal(0, stats.FewestDeaths!.Value);
        Assert.Equal(0, stats.LeastDamageTaken!.Value);
        Assert.Equal(5000, stats.BestHealing!.Value);
    }

    [Fact]
    public void Every_known_boss_is_available_before_first_kill_and_more_than_ten_bosses_keep_records()
    {
        var data = Data();
        foreach (var code in Enumerable.Range(1000, 40)) data.Npcs[code] = new(code, "Dungeon or world boss " + code, true, false);
        var path = Path.Combine(_directory, "records.json");
        var store = new PersonalBestStore(data, path);
        Assert.Equal(41, store.Catalog().Count);
        Assert.Empty(store.List());
        foreach (var boss in store.Catalog())
        {
            var r = Kill();
            store.Record(r with { Summary = r.Summary with { Boss = r.Summary.Boss! with { NpcCode = boss.Code, Name = boss.Name } } });
        }
        Assert.Equal(41, new PersonalBestStore(data, path).List().Count);
        Assert.DoesNotContain(store.Catalog(), n => n.IsDummy || !n.IsBoss);
    }

    [Fact]
    public void Beta_one_data_migrates_to_independent_values_and_discards_attempt_history()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "records.json");
        object Attempt(double dps, long damage, long healing, int deaths, long taken, bool eligible = true) => new
        {
            FightId = Guid.NewGuid(), StartedAt = DateTimeOffset.UtcNow, Dps = dps, Damage = damage, MaxHit = damage / 2,
            DeathOffsetsMs = Enumerable.Range(1, deaths).Select(i => i * 100).ToArray(), EligibleForBest = eligible, Healing = healing, DamageTaken = taken,
        };
        var dpsBest = Attempt(2000, 1000, 100, 2, 500);
        var healingBest = Attempt(800, 2000, 5000, 1, 400);
        File.WriteAllText(path, JsonSerializer.Serialize(new[] { new
        {
            Character = "Own", ServerId = 2305, Class = GameClass.Cleric, BossCode = 900, MapId = 600001, BossName = "Boss",
            BestDps = dpsBest, BestDamage = healingBest, BestHit = healingBest, BestHealing = healingBest,
            RecentAttempts = new[] { Attempt(100, 500, 1000, 0, 50), Attempt(90000, 90000, 90000, 0, 0, false) },
        } }));
        var stats = Assert.Single(new PersonalBestStore(Data(), path).List());
        Assert.Equal(2000, stats.BestDps!.Value);
        Assert.Equal(5000, stats.BestHealing!.Value);
        Assert.Equal(0, stats.FewestDeaths!.Value);
        Assert.Equal(50, stats.LeastDamageTaken!.Value);
        Assert.DoesNotContain("RecentAttempts", File.ReadAllText(path));
        Assert.Equal(stats, Assert.Single(new PersonalBestStore(Data(), path).List()));
    }

    [Fact]
    public void Damaged_store_does_not_prevent_startup()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "records.json");
        File.WriteAllText(path, "[null,{}]");
        Assert.Empty(new PersonalBestStore(Data(), path).List());
        File.WriteAllText(path, "{ torn");
        Assert.Empty(new PersonalBestStore(Data(), path).List());
    }
    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
}
