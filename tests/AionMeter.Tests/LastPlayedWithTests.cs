using AionMeter.Core.Events;
using AionMeter.Core.Storage;

namespace AionMeter.Tests;

public sealed class LastPlayedWithTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "catscratch-recent-" + Guid.NewGuid().ToString("N"));
    private static SelfIdentifiedEvent Own(string name = "Own", int server = 2305) => new(1000, 1, name, server, GameClass.Cleric);
    private static PartyRosterEvent Roster(long time = 2000, string friend = "Friend", int server = 2305, GameClass job = GameClass.Ranger) =>
        new(time, [new("Own", 2305, 50, 0, 0, GameClass.Cleric), new(friend, server, 50, 0, 0, job)]);
    [Fact]
    public void Only_confirmed_group_members_are_saved_and_self_is_excluded()
    {
        var store = new LastPlayedWithStore();
        store.Observe(new PlayerSeenEvent(900, 2, "Nearby stranger", 2305, GameClass.Ranger));
        store.Observe(Own()); store.Observe(Roster());
        var member = Assert.Single(store.List());
        Assert.Equal("Friend", member.Name); Assert.Equal(GameClass.Ranger, member.Class);
        Assert.Equal("Own", member.Character);
    }
    [Fact]
    public void Roster_before_identity_waits_for_matching_live_character()
    {
        var store = new LastPlayedWithStore(); store.Observe(Roster()); Assert.Empty(store.List());
        store.Observe(Own()); Assert.Single(store.List());
        store.Reset(); store.Observe(Roster(friend: "Other")); store.Observe(Own("Alt"));
        Assert.Single(store.List());
    }
    [Fact]
    public void Repeated_rosters_do_not_write_and_rejoining_updates_recency()
    {
        var store = new LastPlayedWithStore(Path.Combine(_directory, "players.json"));
        var changes = 0; store.Changed += () => changes++;
        store.Observe(Own()); store.Observe(Roster());
        var content = File.ReadAllText(Path.Combine(_directory, "players.json"));
        store.Observe(Roster(3000)); Assert.Equal(1, changes);
        Assert.Equal(content, File.ReadAllText(Path.Combine(_directory, "players.json")));
        store.Observe(new PartyLeftEvent(3500)); store.Observe(Roster(4000));
        Assert.Equal(2, changes); Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(4000), store.List()[0].LastPlayedAt);
    }
    [Fact]
    public void Members_are_unique_per_server_and_class_refresh_does_not_lose_known_class()
    {
        var store = new LastPlayedWithStore(); store.Observe(Own()); store.Observe(Roster(job: GameClass.Unknown));
        store.Observe(new PlayerSeenEvent(2100, 2, "Friend", 2305, GameClass.Ranger));
        store.Observe(Roster(2200, job: GameClass.Unknown));
        Assert.Equal(GameClass.Ranger, Assert.Single(store.List()).Class);
        store.Observe(Roster(3000, server: 2306)); Assert.Equal(2, store.List().Count);
    }
    [Fact]
    public void Players_survive_restart_and_have_no_ten_player_limit()
    {
        var path = Path.Combine(_directory, "players.json"); var store = new LastPlayedWithStore(path); store.Observe(Own());
        for (var i = 0; i < 30; i++) store.Observe(Roster(2000 + i, "Friend " + i));
        var reloaded = new LastPlayedWithStore(path);
        Assert.Equal(30, reloaded.List().Count); Assert.Equal("Friend 29", reloaded.List()[0].Name);
    }
    [Fact]
    public void Switching_character_and_reset_prevent_stale_party_attribution()
    {
        var store = new LastPlayedWithStore(); store.Observe(Own()); store.Observe(Roster());
        store.Reset(); store.Observe(Roster(friend: "Not confirmed")); store.Observe(Own("Alt"));
        Assert.Single(store.List());
        store.Observe(new PartyRosterEvent(4000, [new("Alt",2305,50,0,0,GameClass.Cleric),new("Friend",2305,50,0,0,GameClass.Ranger)]));
        Assert.Equal(2, store.List().Count);
        Assert.Equal(2, store.List().Select(e => e.Character).Distinct().Count());
    }
    [Fact]
    public void Corrupt_player_file_does_not_prevent_startup()
    {
        Directory.CreateDirectory(_directory); var path = Path.Combine(_directory, "players.json"); File.WriteAllText(path, "[null,{}]");
        Assert.Empty(new LastPlayedWithStore(path).List());
        File.WriteAllText(path, "torn"); Assert.Empty(new LastPlayedWithStore(path).List());
    }
    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
}
