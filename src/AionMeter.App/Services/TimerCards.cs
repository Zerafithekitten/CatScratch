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
        if(local.Date == today) return local.ToString("HH:mm");
        var day = local.Date == today.AddDays(1) ? "Tomorrow" : local.ToString("dd.MM.");
        return $"{UiText.Current.Localize(day)} {local:HH:mm}";
    }
    public sealed class State
    {
        public string EventRegion { get; set; } = "Europe";
        public List<string> SelectedEvents { get; set; } = ["shugo", "rift", "invasion"];
        public HashSet<string> FavoriteEvents { get; set; } = [];
        public double EventOpacity { get; set; } = 0.85;
        public bool ShowAllWorldBosses { get; set; }
        public int EventTileVersion { get; set; }
        public bool EventWindowOpen { get; set; } = true;
        public bool StartedEventWindowOpen { get; set; } = true;
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
    private AppSettings? _settingsOverride;
    internal AppSettings Settings=>_settingsOverride ?? _meter.Settings;

    internal TimerCards CreatePreferencesDraft() => new(_meter, persistent: false)
    {
        Saved = JsonSerializer.Deserialize<State>(JsonSerializer.Serialize(Saved))!,
        _settingsOverride = Settings.CreateDraft()
    };

    internal void ApplyPreferences(TimerCards draft)
    {
        var source = draft.Saved;
        Saved.EventRegion = source.EventRegion;
        Saved.SelectedEvents = new(source.SelectedEvents);
        Saved.EventOpacity = source.EventOpacity;
        Saved.ShowAllWorldBosses = source.ShowAllWorldBosses;
        Saved.EventWindowOpen = source.EventWindowOpen;
        Saved.StartedEventWindowOpen = source.StartedEventWindowOpen;
        Saved.SoundLeadMinutes = source.SoundLeadMinutes;
        Saved.SoundVolume = source.SoundVolume;
        Saved.Sounds = JsonSerializer.Deserialize<Dictionary<string,EventSoundPreference>>(JsonSerializer.Serialize(source.Sounds))!;
        foreach (var key in new[] { "auto-events", "started-events" })
            if (source.Positions.TryGetValue(key, out var placement))
            {
                if (!Saved.Positions.TryGetValue(key, out var live)) Saved.Positions[key] = live = new();
                live.Width = placement.Width; // Keep positions moved while Settings was open.
            }
        Settings.BossAlertMinutes = draft.Settings.BossAlertMinutes;
        _scheduleKey = null;
        Save();
    }
    public bool TestMode => _meter.DemoRunning;
    public event Action? FavoritesChanged;
    public bool IsFavorite(string id)=>Saved.FavoriteEvents.Contains(id.StartsWith("auto:")?id[5..]:id);
    public void SetFavorite(string id,bool favorite)
    {
        if(id.StartsWith("auto:"))id=id[5..];
        if(favorite?!Saved.FavoriteEvents.Add(id):!Saved.FavoriteEvents.Remove(id))return;
        Save();FavoritesChanged?.Invoke();
    }
    public IReadOnlyList<EventStart> Schedule24Hours(DateTimeOffset now)
    {
        var until=now.AddHours(24);
        var scheduled=AutomaticEventSchedule.Next(now,Saved.EventRegion,AutomaticEventSchedule.Catalog.Select(e=>e.Id),horizon:TimeSpan.FromHours(24));
        var bosses=_meter.Timers.ForServer(_meter.Timers.CurrentServer)
            .Where(b=>b.NpcCode>0 && b.NextSpawn is { } at && at>=now && at<until)
            .Select(b=>new EventStart($"boss:{b.ServerId}:{b.NpcCode}",_meter.Data.NpcName(b.NpcCode),b.NextSpawn!.Value));
        return scheduled.Concat(bosses).Where(e=>e.StartsAt>=now && e.StartsAt<until).OrderBy(e=>e.StartsAt).ThenBy(e=>e.Id).DistinctBy(e=>(e.Id,e.StartsAt)).ToArray();
    }
    private IReadOnlyList<EventStart> _nextEvents = [];
    private string? _scheduleKey;
    private DateTimeOffset _scheduleAt, _scheduleUntil;
    public IReadOnlyList<EventStart> NextEvents(DateTimeOffset now)
    {
        if(TestMode)
        {
            var anchor=_meter.TestStartedAt;
            if(now>anchor.AddMinutes(8))anchor=new DateTimeOffset(now.Year,now.Month,now.Day,now.Hour,now.Minute,0,now.Offset);
            var examples=AutomaticEventSchedule.Catalog.Where(e=>Saved.SelectedEvents.Contains(e.Id)).Select((e,i)=>new EventStart(e.Id,e.Name,i==0?anchor.AddSeconds(-30):anchor.AddMinutes(5*(i+1))));
            if(Saved.ShowAllWorldBosses)examples=examples.Concat(_meter.Timers.All().Where(b=>b.NextSpawn is not null).Select(b=>new EventStart($"boss:{b.ServerId}:{b.NpcCode}",_meter.Data.NpcName(b.NpcCode),b.NextSpawn!.Value)));
            return examples.Where(e=>(e.StartsAt>now || EventActivity.IsActive(e,now)) && e.StartsAt<=now.AddHours(1)).OrderBy(e=>e.StartsAt).ToArray();
        }
        var key = Saved.EventRegion + ":" + Saved.ShowAllWorldBosses + ":" + string.Join('|', Saved.SelectedEvents);
        if (key == _scheduleKey && now >= _scheduleAt && now < _scheduleUntil) return _nextEvents;
        var selected = Saved.ShowAllWorldBosses
            ? Saved.SelectedEvents.Concat(AutomaticEventSchedule.Catalog.Where(e => EventKinds.For(e.Id) == "BOSS").Select(e => e.Id))
            : Saved.SelectedEvents;
        var scheduled = AutomaticEventSchedule.Next(now, Saved.EventRegion, selected, TimeSpan.FromMinutes(10))
            .Where(e => e.StartsAt > now || EventActivity.IsActive(e, now));
        var liveBosses = Saved.ShowAllWorldBosses
            ? _meter.Timers.ForServer(_meter.Timers.CurrentServer)
                .Where(b => b.NpcCode > 0 && b.NextSpawn is { } next && next > now.AddMinutes(-2) && next <= now.AddHours(1))
                .Select(b => new EventStart($"boss:{b.ServerId}:{b.NpcCode}", _meter.Data.NpcName(b.NpcCode), b.NextSpawn!.Value))
            : Enumerable.Empty<EventStart>();
        var retainedBosses = Saved.ShowAllWorldBosses ? _nextEvents.Where(e =>
            e.Id.StartsWith($"boss:{_meter.Timers.CurrentServer}:") && EventActivity.IsActive(e,now)) : [];
        var candidates = scheduled.Concat(liveBosses).Concat(retainedBosses).ToArray();
        _nextEvents = candidates.OrderBy(e => e.StartsAt).ThenBy(e => e.Id)
            .DistinctBy(e => (e.Id,e.StartsAt)).ToArray();
        _scheduleKey = key;
        _scheduleAt = now;
        _scheduleUntil = now.AddSeconds(1);
        foreach (var item in _nextEvents)
        {
            var start = item.StartsAt;
            var transition = start > now ? start : start.Add(EventActivity.Duration(item.Id));
            if (transition < _scheduleUntil) _scheduleUntil = transition;
        }
        return _nextEvents;
    }
    public static string StartedText(EventStart item, DateTimeOffset now)
    {
        if (EventKinds.For(item.Id) == "BOSS") return UiText.Current.Localize("Spawned");
        var seconds = EventActivity.RemainingSeconds(item,now);
        return $"{UiText.Current.Localize("Open!")} · {seconds / 60:00}:{seconds % 60:00}";
    }
    public TimerCards(MeterService meter, bool persistent = true)
    {
        _meter = meter;
        _persistent = persistent && !meter.Settings.Transient;
        if (_persistent) Saved = JsonSettingsFile.Read<State>(_path) ?? new();
        Saved.SelectedEvents ??= []; Saved.FavoriteEvents ??= []; Saved.Positions ??= []; Saved.Sounds ??= []; Saved.Events ??= [];
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
            JsonSettingsFile.Write(_path, Saved, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex) when(ex is IOException or UnauthorizedAccessException) { Log.Error("Could not save timer cards", ex); }
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
