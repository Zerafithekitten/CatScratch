using System.IO;
using System.Text.Json;
using System.Windows;
using AionMeter.Core.Events;
using AionMeter.App.Windows;

namespace AionMeter.App.Services;

public sealed record TimerCardData(string Id, string Name, string Kind, DateTimeOffset? Next, string Source, bool Alive = false);
public sealed class CardPlacement
{
    public double Left { get; set; } = 100;
    public double Top { get; set; } = 100;
    public double Width { get; set; } = 330;
    public double Height { get; set; } = 180;
    public bool Open { get; set; }
}
public sealed class TimerCards : IDisposable
{
    public static readonly TimeZoneInfo DisplayZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
    public static DateTimeOffset Berlin(DateTimeOffset value) => TimeZoneInfo.ConvertTime(value, DisplayZone);
    public static string StartLabel(DateTimeOffset start, DateTimeOffset now)
    {
        var local = Berlin(start);
        var today = Berlin(now).Date;
        var day = local.Date == today ? "Today" : local.Date == today.AddDays(1) ? "Tomorrow" : local.ToString("dd.MM.");
        return $"{UiText.Current.Localize(day)} {local:HH:mm}";
    }
    public sealed class State
    {
        public string EventRegion { get; set; } = "Europe";
        public List<string> SelectedEvents { get; set; } = ["shugo", "rift", "invasion"];
        public double EventOpacity { get; set; } = 0.85;
        public bool ShowAllWorldBosses { get; set; }
        public int EventTileVersion { get; set; }
        public bool EventWindowOpen { get; set; } = true;
        public List<ScheduledTimer> Events { get; set; } = [];
        public Dictionary<string, CardPlacement> Positions { get; set; } = [];
        public Dictionary<string, EventSoundPreference> Sounds { get; set; } = [];
        public int SoundLeadMinutes { get; set; } = 2;
        public double SoundVolume { get; set; } = .6;
    }
    private readonly MeterService _meter;
    private readonly bool _persistent;
    private readonly string _path = Path.Combine(AppSettings.AppDataDir, "timer-cards.json");
    private readonly Dictionary<string, TimerCardWindow> _windows = [];
    public State Saved { get; private set; } = new();
    private IReadOnlyList<EventStart> _nextEvents = [];
    private string? _scheduleKey;
    private DateTimeOffset _scheduleAt, _scheduleUntil;
    public IReadOnlyList<EventStart> NextEvents(DateTimeOffset now)
    {
        var key = Saved.EventRegion + ":" + Saved.ShowAllWorldBosses + ":" + string.Join('|', Saved.SelectedEvents);
        if (key == _scheduleKey && now >= _scheduleAt && now < _scheduleUntil) return _nextEvents;
        var selected = Saved.ShowAllWorldBosses
            ? Saved.SelectedEvents.Concat(AutomaticEventSchedule.Catalog.Where(e => EventKinds.For(e.Id) == "BOSS").Select(e => e.Id))
            : Saved.SelectedEvents;
        var scheduled = AutomaticEventSchedule.Next(now, Saved.EventRegion, selected, TimeSpan.FromMinutes(2));
        var liveBosses = Saved.ShowAllWorldBosses
            ? _meter.Timers.ForServer(_meter.Timers.CurrentServer)
                .Where(b => b.NpcCode > 0 && b.NextSpawn is { } next && next > now.AddMinutes(-2) && next <= now.AddHours(1))
                .Select(b => new EventStart($"boss:{b.ServerId}:{b.NpcCode}", _meter.Data.NpcName(b.NpcCode), b.NextSpawn!.Value))
            : Enumerable.Empty<EventStart>();
        var candidates = scheduled.Concat(liveBosses).ToArray();
        var first = candidates.Length == 0 ? (DateTimeOffset?)null : candidates.Min(c => c.StartsAt);
        _nextEvents = candidates.Where(e => e.StartsAt == first)
            .OrderBy(e => e.Id).ToArray();
        _scheduleKey = key;
        _scheduleAt = now;
        _scheduleUntil = now.AddSeconds(1);
        if (_nextEvents.Count > 0)
        {
            var start = _nextEvents[0].StartsAt;
            var transition = start > now ? start : start.AddMinutes(2);
            if (transition < _scheduleUntil) _scheduleUntil = transition;
        }
        return _nextEvents;
    }
    public TimerCards(MeterService meter, bool persistent = true)
    {
        _meter = meter;
        _persistent = persistent && !meter.Settings.Transient;
        if (_persistent && File.Exists(_path))
            try { Saved = JsonSerializer.Deserialize<State>(File.ReadAllText(_path)) ?? new(); }
            catch (Exception ex) when (ex is IOException or JsonException) { Log.Error("Timer cards could not be loaded", ex); }
        if (Saved.EventRegion == "Europa") Saved.EventRegion = "Europe";
        if (Saved.EventTileVersion < 1)
        {
            if (Saved.Positions.TryGetValue("auto-events", out var placement)) placement.Width = 480;
            Saved.EventTileVersion = 1;
        }
    }
    public IReadOnlyList<TimerCardData> Items(DateTimeOffset now)
    {
        var events = Saved.Events.Select(e => new TimerCardData(e.Id, e.Name, e.Kind, e.NextAt(now), "Manual schedule · Europe/Berlin"));
        var bosses = _meter.Timers.ForServer(_meter.Timers.CurrentServer).Where(b => b.NpcCode > 0)
            .Select(b => BossCard(b.ServerId, b.NpcCode));
        return events.Concat(bosses).OrderBy(e => e.Next is { } next && next >= now && !e.Alive ? next : DateTimeOffset.MaxValue).ToList();
    }
    private TimerCardData BossCard(int server, int code)
    {
        var boss = _meter.Timers.ForServer(server).FirstOrDefault(b => b.NpcCode == code);
        return new($"boss:{server}:{code}", _meter.Data.NpcName(code), "BOSS", boss?.NextSpawn,
            $"Server {server} · " + UiText.Current.Localize(boss?.FromGame == true ? "Time from in-game boss list" : "Observation/interval · estimate"), boss?.AliveNow == true);
    }
    public TimerCardData? Get(string id)
    {
        if (id.StartsWith("auto:"))
        {
            var key = id[5..];
            var next = NextEvents(DateTimeOffset.UtcNow).FirstOrDefault(e => e.Id == key);
            return next is null ? null : new(id, next.Name, EventKinds.For(key), next.StartsAt, "Community schedule · Shugo.GG");
        }
        if (id.StartsWith("boss:"))
        {
            var parts = id.Split(':');
            if (parts.Length == 3 && int.TryParse(parts[1], out var server) && int.TryParse(parts[2], out var code)) return BossCard(server, code);
            return null;
        }
        var item = Saved.Events.FirstOrDefault(e => e.Id == id);
        return item is null ? null : new(item.Id, item.Name, item.Kind, item.NextAt(DateTimeOffset.UtcNow), "Manual schedule · Europe/Berlin");
    }
    public void Pin(string id)
    {
        if (_windows.TryGetValue(id, out var existing)) { existing.Activate(); return; }
        if (Get(id) is null) return;
        if (!Saved.Positions.TryGetValue(id, out var placement))
            Saved.Positions[id] = placement = new() { Left = 100 + _windows.Count * 30, Top = 100 + _windows.Count * 40 };
        if (id.StartsWith("auto:") && placement.Height > 100) { placement.Width = 240; placement.Height = 54; }
        placement.Open = true;
        var window = new TimerCardWindow(this, id, placement);
        WindowClickThrough.Attach(window, () => _meter.Settings.ClickThrough, () => _meter.Settings.WindowOpacity);
        _windows[id] = window;
        window.Closed += (_, _) => { _windows.Remove(id); if (!AppHost.Current.IsExiting) placement.Open = false; Save(); };
        window.Show();
        Save();
    }
    public void Restore()
    {
        foreach (var id in Saved.Positions.Where(p => p.Value.Open).Select(p => p.Key).ToList()) Pin(id);
    }
    public void Remove(string id)
    {
        Saved.Events.RemoveAll(e => e.Id == id);
        if (_windows.TryGetValue(id, out var window)) window.Close();
        Saved.Positions.Remove(id);
        Save();
    }
    public void Save()
    {
        if (!_persistent) return;
        try
        {
            Directory.CreateDirectory(AppSettings.AppDataDir);
            File.WriteAllText(_path + ".tmp", JsonSerializer.Serialize(Saved, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(_path + ".tmp", _path, true);
        }
        catch (IOException ex) { Log.Error("Could not save timer cards", ex); }
    }
    public void Dispose()
    {
        // Shutdown closes the windows: remember which cards were open before the close handlers run.
        var open = _windows.Keys.ToList();
        foreach (var window in _windows.Values.ToList()) window.Close();
        foreach (var id in open) Saved.Positions[id].Open = true;
        Save();
    }
    public static string Countdown(TimerCardData card, DateTimeOffset now)
    {
        if (card.Alive) return UiText.Current.Localize("Boss is active");
        if (card.Next is not { } next) return UiText.Current.Localize("Time not known yet");
        var remaining = next - now;
        if (remaining <= TimeSpan.Zero) return UiText.Current.Localize(card.Kind == "BOSS" ? "Spawn time reached · awaiting confirmation" : "Scheduled time reached");
        return $"{UiText.Current.Localize("in")} {(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
    }
}
