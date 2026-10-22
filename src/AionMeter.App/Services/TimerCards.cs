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
    public int VerticalEdge {get;set;} // -1 top, 1 bottom, 0 free
    public double EdgeGap {get;set;}
}
public sealed partial class TimerCards : IDisposable
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
        public HashSet<int> SelectedWorldBosses {get;set;}=[];
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
    private readonly string _path;
    private readonly Dictionary<string, TimerCardWindow> _windows = [];
    public State Saved { get; private set; } = new();
    private AppSettings? _settingsOverride;
    internal AppSettings Settings=>_settingsOverride ?? _meter.Settings;

    private HashSet<string> _favoritesBaseline=[];
    internal TimerCards CreatePreferencesDraft() => new(_meter, persistent: false)
    {
        Saved = JsonSerializer.Deserialize<State>(JsonSerializer.Serialize(Saved))!,
        _favoritesBaseline=new(Saved.FavoriteEvents),
        _settingsOverride = Settings.CreateDraft()
    };

    private TimerCards? _previewBaseline;
    private readonly HashSet<string> _previewFields=[],_previewFavorites=[],_previewPlacements=[];
    private static readonly string[] PreviewFields=["EventRegion","SelectedEvents","EventOpacity","ShowAllWorldBosses","SelectedWorldBosses","EventWindowOpen","StartedEventWindowOpen","SoundLeadMinutes","SoundVolume","Sounds"];
    internal void BeginPreferencesPreview()=>_previewBaseline??=CreatePreferencesDraft();
    private static object? CloneValue(System.Reflection.PropertyInfo property,State value)=>JsonSerializer.Deserialize(JsonSerializer.Serialize(property.GetValue(value),property.PropertyType),property.PropertyType);
    internal void PreviewPreferences(TimerCards draft)
    {
        BeginPreferencesPreview();var baseline=_previewBaseline!.Saved;
        foreach(var name in PreviewFields)
        {
            var property=typeof(State).GetProperty(name)!;
            var desired=JsonSerializer.Serialize(property.GetValue(draft.Saved),property.PropertyType);
            var original=JsonSerializer.Serialize(property.GetValue(baseline),property.PropertyType);
            if(desired!=original){_previewFields.Add(name);property.SetValue(Saved,CloneValue(property,draft.Saved));}
            else if(_previewFields.Remove(name))property.SetValue(Saved,CloneValue(property,baseline));
        }
        foreach(var id in baseline.FavoriteEvents.Union(draft.Saved.FavoriteEvents).Union(_previewFavorites).ToArray())
        {
            var desired=draft.Saved.FavoriteEvents.Contains(id);var original=baseline.FavoriteEvents.Contains(id);
            if(desired!=original){_previewFavorites.Add(id);if(desired)Saved.FavoriteEvents.Add(id);else Saved.FavoriteEvents.Remove(id);}
            else if(_previewFavorites.Remove(id)){if(original)Saved.FavoriteEvents.Add(id);else Saved.FavoriteEvents.Remove(id);}
        }
        foreach(var key in new[]{"auto-events","started-events"})
        {
            var original=baseline.Positions.GetValueOrDefault(key)?.Width??480;
            var desired=draft.Saved.Positions.GetValueOrDefault(key)?.Width??480;
            if(desired!=original)
            {
                _previewPlacements.Add(key);if(!Saved.Positions.TryGetValue(key,out var position))Saved.Positions[key]=position=new();position.Width=desired;
            }
            else if(_previewPlacements.Remove(key))RestorePreviewPlacement(Saved,key);
        }
        Settings.BossAlertMinutes=draft.Settings.BossAlertMinutes;
        _scheduleKey=null;FavoritesChanged?.Invoke();
    }
    private void RestorePreviewPlacement(State target,string key)
    {
        if(_previewBaseline!.Saved.Positions.TryGetValue(key,out var original))target.Positions[key]=JsonSerializer.Deserialize<CardPlacement>(JsonSerializer.Serialize(original))!;
        else target.Positions.Remove(key);
    }
    private void RestorePreviewPreferences(State target)
    {
        foreach(var name in _previewFields)
        {
            var property=typeof(State).GetProperty(name)!;property.SetValue(target,CloneValue(property,_previewBaseline!.Saved));
        }
        foreach(var id in _previewFavorites)
            if(_previewBaseline!.Saved.FavoriteEvents.Contains(id))target.FavoriteEvents.Add(id);else target.FavoriteEvents.Remove(id);
        foreach(var key in _previewPlacements)RestorePreviewPlacement(target,key);
    }
    internal void EndPreferencesPreview(bool commit)
    {
        if(_previewBaseline is not {} baseline)return;
        if(!commit){RestorePreviewPreferences(Saved);_scheduleKey=null;FavoritesChanged?.Invoke();}
        _previewFields.Clear();_previewFavorites.Clear();_previewPlacements.Clear();_previewBaseline=null;baseline.Dispose();if(commit)Save();
    }
    internal void ApplyPreferences(TimerCards draft)
    {
        var source = draft.Saved;
        Saved.EventRegion = source.EventRegion;
        Saved.SelectedEvents = new(source.SelectedEvents);
        Saved.EventOpacity = source.EventOpacity;
        Saved.ShowAllWorldBosses = source.ShowAllWorldBosses;
        Saved.SelectedWorldBosses=new(source.SelectedWorldBosses);
        foreach(var id in source.FavoriteEvents.Except(draft._favoritesBaseline))Saved.FavoriteEvents.Add(id);
        foreach(var id in draft._favoritesBaseline.Except(source.FavoriteEvents))Saved.FavoriteEvents.Remove(id);
        draft._favoritesBaseline=new(source.FavoriteEvents);
        FavoritesChanged?.Invoke();
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
    public bool IsFavorite(string id)=>Saved.FavoriteEvents.Any(f=>FavoriteKey(f)==FavoriteKey(id));
    public void SetFavorite(string id,bool favorite)
    {
        id=FavoriteKey(id);
        var changed=favorite?Saved.FavoriteEvents.Add(id):Saved.FavoriteEvents.RemoveWhere(f=>FavoriteKey(f)==id)>0;
        if(!changed)return;
        _scheduleKey=null;
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
            var examples=AutomaticEventSchedule.Catalog.Where(e=>EffectiveScheduledIds().Contains(e.Id)).Select((e,i)=>new EventStart(e.Id,e.Name,i==0?anchor.AddSeconds(-30):anchor.AddMinutes(5*(i+1))));
            if(Saved.ShowAllWorldBosses || SelectedBossCodes().Any())examples=examples.Concat(_meter.Timers.All().Where(b=>b.NextSpawn is not null && IncludeWorldBoss(b.NpcCode)).Select(b=>new EventStart($"boss:{b.ServerId}:{b.NpcCode}",_meter.Data.NpcName(b.NpcCode),b.NextSpawn!.Value)));
            return examples.Where(e=>(e.StartsAt>now || EventActivity.IsActive(e,now)) && e.StartsAt<=now.AddHours(1)).OrderBy(e=>e.StartsAt).ToArray();
        }
        var key = Saved.EventRegion + ":" + Saved.ShowAllWorldBosses + ":" + string.Join('|', EffectiveScheduledIds())+":"+string.Join('|',SelectedBossCodes());
        if (key == _scheduleKey && now >= _scheduleAt && now < _scheduleUntil) return _nextEvents;
        var selected=EffectiveScheduledIds();
        var scheduled = AutomaticEventSchedule.Next(now, Saved.EventRegion, selected, TimeSpan.FromMinutes(10))
            .Where(e => e.StartsAt > now || EventActivity.IsActive(e, now));
        var liveBosses = Saved.ShowAllWorldBosses || SelectedBossCodes().Any()
            ? _meter.Timers.ForServer(_meter.Timers.CurrentServer)
                .Where(b => b.NpcCode > 0 && IncludeWorldBoss(b.NpcCode) && b.NextSpawn is { } next && next > now.AddMinutes(-2) && next <= now.AddHours(1))
                .Select(b => new EventStart($"boss:{b.ServerId}:{b.NpcCode}", _meter.Data.NpcName(b.NpcCode), b.NextSpawn!.Value))
            : Enumerable.Empty<EventStart>();
        var retainedBosses = _nextEvents.Where(e =>
            e.Id.StartsWith($"boss:{_meter.Timers.CurrentServer}:") && TryBossCode(e.Id,out var code) && IncludeWorldBoss(code) && EventActivity.IsActive(e,now));
        var candidates = scheduled.Concat(liveBosses).Concat(retainedBosses).Concat(PersistentScheduledBosses(now)).ToArray();
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
    public TimerCards(MeterService meter, bool persistent = true,string? directory=null)
    {
        _meter = meter;
        _path=Path.Combine(directory??AppSettings.AppDataDir,"timer-cards.json");
        _persistent = persistent && (!meter.Settings.Transient||directory is not null);
        if (_persistent) Saved = JsonSettingsFile.Read<State>(_path) ?? new();
        Saved.SelectedWorldBosses ??=[]; Saved.SelectedEvents ??= []; Saved.FavoriteEvents ??= []; Saved.Positions ??= []; Saved.Sounds ??= []; Saved.Events ??= [];
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
    private TimerCardData BossCard(int server, int code, DateTimeOffset? clock=null)
    {
        var boss = _meter.Timers.ForServer(server).FirstOrDefault(b => b.NpcCode == code);
        // Only this exact NPC is the scheduled Watcher; other Kaira variants have different identities.
        if(code==2600089 && boss?.NextSpawn is null && boss?.AliveNow!=true)
        {
            var scheduled=NextScheduledOccurrence("kaira",clock??DateTimeOffset.UtcNow);
            if(scheduled is not null)return new($"boss:{server}:{code}",_meter.Data.NpcName(code),"BOSS",scheduled.StartsAt,"Community schedule · Shugo.GG");
        }
        return new($"boss:{server}:{code}", _meter.Data.NpcName(code), "BOSS", boss?.NextSpawn,
            $"Server {server} · " + UiText.Current.Localize(boss?.FromGame == true ? "Time from in-game boss list" : "Observation/interval · estimate"), boss?.AliveNow == true);
    }
    public TimerCardData? Get(string id)=>GetAt(id,DateTimeOffset.UtcNow);
    internal TimerCardData? GetAt(string id,DateTimeOffset now)
    {
        if (id.StartsWith("auto:"))
        {
            var key = id[5..];
            var next = TestMode?NextEvents(now).FirstOrDefault(e=>e.Id==key):NextScheduledOccurrence(key,now);
            return next is null ? null : new(id, next.Name, EventKinds.For(key), next.StartsAt, "Community schedule · Shugo.GG");
        }
        if (id.StartsWith("boss:"))
        {
            var parts = id.Split(':');
            if (parts.Length == 3 && int.TryParse(parts[1], out var server) && int.TryParse(parts[2], out var code)) return BossCard(server, code,now);
            return null;
        }
        var item = Saved.Events.FirstOrDefault(e => e.Id == id);
        return item is null ? null : new(item.Id, item.Name, item.Kind, item.NextAt(now), "Manual schedule · Europe/Berlin");
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
    internal event Action? PreferencesEdited;
    public void Save()
    {
        PreferencesEdited?.Invoke();
        if (!_persistent) return;
        try
        {
            var persisted=Saved;
            if(_previewBaseline is {} baseline)
            {
                persisted=JsonSerializer.Deserialize<State>(JsonSerializer.Serialize(Saved))!;
                RestorePreviewPreferences(persisted);
            }
            JsonSettingsFile.Write(_path, persisted, new JsonSerializerOptions { WriteIndented = true });
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
