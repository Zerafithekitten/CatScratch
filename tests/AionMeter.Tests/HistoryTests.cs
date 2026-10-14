using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Storage;

namespace AionMeter.Tests;

public sealed class HistoryTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "aionmeter-history-" + Guid.NewGuid().ToString("N"));

    private static FightRecord Fight(string title, int minute, bool boss, int selfPlace)
    {
        var combatants = new List<CombatantSnapshot>();
        for (var i = 1; i <= 3; i++)
            combatants.Add(new CombatantSnapshot((uint)i, $"P{i}", GameClass.Cleric, i == selfPlace, 1000 - i, 100 - i, 0.33, 10, 0.5, 50, 0));
        var summary = new EncounterSnapshot(
            Guid.NewGuid(), title, "Krao Cave", new DateTimeOffset(2026, 10, 5, 18, minute, 0, TimeSpan.Zero), 30_000, 30_000,
            false, EncounterEndReason.Kill, 3000, 100,
            boss ? new BossSnapshot(7, 2300243, title, 0, 1000) : null, combatants, boss ? 2300243 : 0);
        return new FightRecord(1, summary, []);
    }

    [Fact]
    public void Incomplete_index_entries_and_fight_files_do_not_prevent_startup()
    {
        var store = new HistoryStore(_dir);
        store.Save(Fight("Berk", 10, true, 1));
        File.AppendAllText(Path.Combine(_dir, "index.jsonl"), "\n{}\n{\"FileName\":null}\n");
        File.WriteAllText(Path.Combine(_dir, "incomplete.json"), "{}");
        File.WriteAllText(Path.Combine(_dir, "missing-players.json"), "{\"Summary\":{\"Combatants\":null}}");
        var reopened = new HistoryStore(_dir);
        Assert.Equal("Berk", Assert.Single(reopened.List()).Title);
        Assert.Null(reopened.Load(Path.Combine(_dir, "incomplete.json")));
        Assert.Null(reopened.Load(Path.Combine(_dir, "missing-players.json")));
    }

    [Fact]
    public void Relative_history_directory_supports_delete_without_stale_index_entries()
    {
        var store = new HistoryStore(Path.GetRelativePath(Environment.CurrentDirectory, _dir));
        var path = store.Save(Fight("Berk", 10, true, 1));
        store.Delete(path);
        Assert.Empty(store.List());
        Assert.Empty(new HistoryStore(_dir).List());
    }

    [Fact]
    public void Delete_does_not_remove_untracked_files_in_history_directory()
    {
        var store = new HistoryStore(_dir);
        store.Save(Fight("Berk", 10, true, 1));
        var unrelated = Path.Combine(_dir, "notes.txt");
        File.WriteAllText(unrelated, "keep");
        store.Delete(unrelated);
        store.Delete(Path.Combine(_dir, "index.jsonl"));
        Assert.True(File.Exists(unrelated));
        Assert.Single(new HistoryStore(_dir).List());
    }

    [Fact]
    public void Only_ten_newest_fights_and_index_entries_remain()
    {
        var store = new HistoryStore(_dir);
        for (var i = 0; i < 14; i++) store.Save(Fight($"Fight {i}", i, false, 1));
        Assert.Equal(Enumerable.Range(4, 10).Reverse().Select(i => $"Fight {i}"), store.List().Select(e => e.Title));
        Assert.Equal(10, System.IO.Directory.GetFiles(_dir, "*.json").Length);
        Assert.Equal(10, File.ReadAllLines(Path.Combine(_dir, "index.jsonl")).Length);
        Assert.Equal(10, new HistoryStore(_dir).List().Count);
    }

    [Fact]
    public void Existing_large_history_is_pruned_on_start_even_without_index()
    {
        System.IO.Directory.CreateDirectory(_dir);
        for (var i = 0; i < 14; i++)
            File.WriteAllText(Path.Combine(_dir, $"legacy-{i}.json"), System.Text.Json.JsonSerializer.Serialize(Fight($"Fight {i}", i, false, 1)));
        var store = new HistoryStore(_dir);
        Assert.Equal(10, store.List().Count);
        Assert.Equal("Fight 13", store.List()[0].Title);
        Assert.Equal("Fight 4", store.List()[9].Title);
        Assert.Equal(10, System.IO.Directory.GetFiles(_dir, "*.json").Length);
    }

    [Fact]
    public void Equal_time_and_title_do_not_overwrite_distinct_fights()
    {
        var store = new HistoryStore(_dir);
        store.Save(Fight("Berk", 10, true, 1));
        store.Save(Fight("Berk", 10, true, 1));
        Assert.Equal(2, store.List().Count);
    }

    [Fact]
    public void Index_survives_restart_and_tracks_deletes()
    {
        var store = new HistoryStore(_dir);
        store.Save(Fight("Berk", 10, boss: true, selfPlace: 2));
        var trash = store.Save(Fight("Spider +3", 12, boss: false, selfPlace: 1));

        var reopened = new HistoryStore(_dir);
        var list = reopened.List();
        Assert.Equal(["Spider +3", "Berk"], list.Select(e => e.Title));
        Assert.True(list[1].IsBoss);
        Assert.Equal(2, list[1].SelfPlace);
        Assert.Equal("P2", list[1].SelfName);
        Assert.NotNull(reopened.Load(list[0].Path));

        reopened.Delete(trash);
        Assert.Single(new HistoryStore(_dir).List());
    }

    [Fact]
    public void Missing_index_is_rebuilt_from_fight_files()
    {
        var store = new HistoryStore(_dir);
        store.Save(Fight("Berk", 10, boss: true, selfPlace: 1));
        File.Delete(Path.Combine(_dir, "index.jsonl"));
        File.AppendAllText(Path.Combine(_dir, "index.jsonl"), "{ torn line");

        var list = new HistoryStore(_dir).List();
        Assert.Equal("Berk", Assert.Single(list).Title);
    }

    [Fact]
    public void Fights_saved_by_older_versions_read_their_boss_as_fully_known()
    {
        var store = new HistoryStore(_dir);
        var path = store.Save(Fight("Berk", 10, boss: true, selfPlace: 1));
        // Strip the fields added in 0.2.4, as a fight file written by 0.2.3 lacks them.
        var json = File.ReadAllText(path).Replace(",\"MaxHpKnown\":true", "").Replace(",\"Uncounted\":0", "");
        Assert.DoesNotContain("MaxHpKnown", json);
        File.WriteAllText(path, json);

        var boss = store.Load(path)!.Summary.Boss!;
        Assert.True(boss.MaxHpKnown);
        Assert.Equal(0, boss.Uncounted);
    }

    [Fact]
    public void Session_cache_keeps_bosses_for_a_short_time_and_reads_old_files()
    {
        var path = Path.Combine(_dir, "names-cache.json");
        var cache = new NameCache(path);
        cache.Save(new Core.Game.SessionState(5, 600012, [new(5, "Sylvaen", GameClass.Elementalist, 2305)], [new(23443, 2300218, 9_860_000)]));
        var loaded = cache.Load()!;
        Assert.Equal(600012, loaded.MapId);
        Assert.Equal(9_860_000, Assert.Single(loaded.Npcs).MaxHp);

        // Written 20 minutes ago: names are still trusted, bosses no longer.
        var old = DateTimeOffset.UtcNow.AddMinutes(-20).ToString("O");
        File.WriteAllText(path, $$"""{"SavedAt":"{{old}}","SelfId":5,"MapId":600012,"Players":[{"Id":5,"Name":"Sylvaen","Class":"Elementalist","ServerId":2305}],"Npcs":[{"Id":23443,"Code":2300218,"MaxHp":9860000}]}""");
        loaded = cache.Load()!;
        Assert.Single(loaded.Players);
        Assert.Empty(loaded.Npcs);

        // A cache written by 0.2.3 has no bosses at all.
        File.WriteAllText(path, $$"""{"SavedAt":"{{DateTimeOffset.UtcNow:O}}","SelfId":5,"MapId":600012,"Players":[]}""");
        Assert.Empty(cache.Load()!.Npcs);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }
}
