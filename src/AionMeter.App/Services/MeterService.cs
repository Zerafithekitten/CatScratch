using System.IO;
using AionMeter.Core;
using AionMeter.Core.Capture;
using AionMeter.Core.Combat;
using AionMeter.Core.Demo;
using AionMeter.Core.Game;
using AionMeter.Core.Storage;

namespace AionMeter.App.Services;

/// <summary>Owns the capture → decoder → combat tracker pipeline and fight history for the whole app.</summary>
public sealed class MeterService : IDisposable
{
    private IEventSource? _capture;
    private readonly SemaphoreSlim _recentSave = new(1, 1);
    public void SetRecentDebug(bool enabled)
    {
        lock (_captureGate) if (_capture is LiveCapture live) live.RecentPackets.Enabled = enabled;
    }
    public async Task<string> SaveRecentDebugAsync()
    {
        if (!await _recentSave.WaitAsync(0)) throw new InvalidOperationException("A debug export is already running.");
        try
        {
            RecentPacketBuffer buffer;
            lock (_captureGate) buffer = (_capture as LiveCapture)?.RecentPackets ?? throw new InvalidOperationException("Live capture is not available.");
            var directory = Path.Combine(AppSettings.AppDataDir, "captures");
            var path = Path.Combine(directory, $"recent_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.pcap");
            await Task.Run(() => { Directory.CreateDirectory(directory); buffer.Save(path); });
            return path;
        }
        finally { _recentSave.Release(); }
    }
    private IEventSource? _replay;
    private DemoFightSource? _demo;
    private readonly PetProgressCache? _petProgress;
    private readonly ResourceProgressCache? _resourceProgress;

    public MeterService(AppSettings settings, Func<GameData, IEventSource>? captureFactory, PersonalBestStore? personalBests = null, LastPlayedWithStore? lastPlayedWith = null, string? historyDirectory = null)
    {
        Settings = settings;
        if (!settings.Transient && !settings.BackgroundUpdateDefaultsApplied)
        {
            settings.CheckUpdates = true; settings.BackgroundUpdateDefaultsApplied = true; settings.Save();
        }
        if (!settings.Transient && !settings.ManualHistoryDefaultsApplied)
        {
            settings.SaveHistory = false;
            settings.ManualHistoryDefaultsApplied = true;
            settings.Save();
        }
        CaptureFactory = captureFactory;
        Data = GameData.Load(DataDirectory, UiText.Normalize(settings.Language));
        if(!settings.Transient){PetFarmCache.Restore(Pets);_petProgress=new PetProgressCache(Pets,Data);_resourceProgress=new ResourceProgressCache(Resources);}
        Icons = new SkillIcons(DataDirectory, settings.DownloadIcons);
        Portraits = new BossPortraits(DataDirectory, settings.DownloadIcons);
        Options = new MeterOptions();
        settings.ApplyTo(Options);
        Options.PartyOnly = !settings.Transient;
        Options.MaxSegments = HistoryStore.MaxSavedFights;
        _realTracker = new CombatTracker(Data, Options);
        History = new HistoryStore(historyDirectory ?? Path.Combine(AppSettings.AppDataDir, "fights"));
        PersonalBests = personalBests ?? new PersonalBestStore(Data, settings.Transient ? null : Path.Combine(AppSettings.AppDataDir, "boss-personal-bests.json"));
        LastPlayedWith = lastPlayedWith ?? new LastPlayedWithStore(settings.Transient ? null : Path.Combine(AppSettings.AppDataDir, "last-played-with.json"));
        _realTimers = new BossTimers(Path.Combine(AppSettings.AppDataDir, "boss-timers.json"), Data, DataDirectory, persistent: !settings.Transient);
        Updates = new Updater(settings);
        Tracker.EncounterFinished += OnEncounterFinished;
        // A recording's or the demo's bosses must not move the live respawn timers.
        Tracker.BossNoticed += n =>
        {
            if (!_replaying && !DemoRunning) Timers.OnNotice(n);
        };
        Tracker.FieldBossListed += list =>
        {
            if (_replaying) return;
            Log.FieldBossList(list);
            _realTimers.OnList(list);
        };
        // Ranges only, no names: enough to see in a user's log whether the roster still reads right.
        Tracker.RosterReceived += roster =>
        {
            if (_replaying) return;
            var gear = roster.Members.Where(m => m.GearScore > 0).Select(m => m.GearScore).DefaultIfEmpty().ToList();
            var power = roster.Members.Where(m => m.CombatPower > 0).Select(m => m.CombatPower).DefaultIfEmpty().ToList();
            var line = $"Party roster: {roster.Members.Count} members, GS {gear.Min()}-{gear.Max()}, CP {power.Min()}-{power.Max()}";
            if (line == _lastRosterLine) return; // the server repeats it every few seconds
            _lastRosterLine = line;
            Log.Info(line);
        };

        // Names and bosses learned before a restart (same zone, recent) come back immediately: no "#id" players, and a boss
        // already in view keeps its name and real max HP.
        if (!settings.Transient && _names.Load() is { } cached)
        {
            Tracker.ImportCache(cached);
            Log.Info($"Restored {cached.Players.Count} cached player names, {cached.Npcs.Count} bosses");
        }
        _savedNamesVersion = Tracker.CacheVersion;

        // Boss timers are per server: follow the character's (from the cache now, then on every login).
        Timers.SetCurrentServer(Tracker.SelfServerId);
        Tracker.SelfIdentified += _ =>
        {
            if (!_replaying && !DemoRunning) Timers.SetCurrentServer(Tracker.SelfServerId);
        };
    }

    public static string DataDirectory { get; } = Path.Combine(AppContext.BaseDirectory, "data");

    public AppSettings Settings { get; }
    public GameData Data { get; }
    public SkillIcons Icons { get; }
    public BossPortraits Portraits { get; }
    public MeterOptions Options { get; }
    public PartyPreview PartyPreview { get; } = new();
    private readonly CombatTracker _realTracker;
    private volatile CombatTracker? _testTracker;
    private Core.Game.PetCollection? _testPets;
    public CombatTracker Tracker => _testTracker ?? _realTracker;
    public Core.Events.LiveIdentity LiveIdentity { get; } = new();
    public HistoryStore History { get; }
    public PersonalBestStore PersonalBests { get; }
    public LastPlayedWithStore LastPlayedWith { get; }
    private readonly BossTimers _realTimers;
    private BossTimers? _testTimers;
    public BossTimers Timers => _testTimers ?? _realTimers;
    public DateTimeOffset TestStartedAt { get; private set; }
    /// <summary>New versions on GitHub; checks only run once the app calls <see cref="Updater.Start"/>.</summary>
    public Updater Updates { get; }
    public Func<GameData, IEventSource>? CaptureFactory { get; }

    public CaptureStatus CaptureStatus =>
        DemoRunning ? new CaptureStatus(CaptureState.Capturing,UiText.Current.Localize("Test mode · sample data")) : _replay is { Status.State: CaptureState.Capturing } r ? r.Status
        : _restarting ? new CaptureStatus(CaptureState.Starting, "Starting capture…")
        : _capture?.Status ?? new CaptureStatus(CaptureState.Stopped, "Capture not available");

    public bool DemoRunning => _testTracker is not null;
    public double? LatencyMs => !_replaying && !DemoRunning && !_restarting && _capture is LiveCapture live
        ? live.Latency.Read(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) : null;

    /// <summary>Raised (on a worker thread) after a fight was written to disk.</summary>
    public event Action<string>? FightSaved;

    /// <summary>
    /// Plays a recorded capture through the meter as fast as it parses. Live events are detached meanwhile (their
    /// timestamps would interleave with the recording's) and replayed fights are not saved to history again.
    /// </summary>
    public void Replay(string path)
    {
        HiddenCubes.Reset(); Resources.Reset();
        StopDemo();
        LastPlayedWith.Reset();
        LiveIdentity.Reset(); PartyPreview.Reset(); Tracker.InvalidateParty();
        _replay?.Dispose();
        Tracker.Reset();
        _replaying = true;
        if (_capture is not null) _capture.EventDecoded -= ProcessLiveEvent;

        var replay = new PcapReplaySource(Data, path, realtime: false);
        replay.EventDecoded += Tracker.Process;
        replay.StatusChanged += s =>
        {
            if (s.State != CaptureState.Stopped) return;
            Tracker.Tick(Tracker.LastEventMs + 3_600_000); // close the recording's last fight
            _savedNamesVersion = Tracker.CacheVersion; // names from a recording must not overwrite the live cache
            _replaying = false;
            if (_capture is not null) _capture.EventDecoded += ProcessLiveEvent;
        };
        _replay = replay;
        replay.Start();
    }

    private volatile bool _replaying;

    private readonly object _captureGate = new();
    private readonly object _restartGate = new();
    private volatile bool _restarting;

    private void ProcessLiveEvent(Core.Events.GameEvent value)
    {
        var identity = !_replaying ? LiveIdentity.Observe(value) : null;
        var resolved = value is Core.Events.SelfIdentifiedEvent && identity is not null ? identity : value;
        if (!_replaying && !DemoRunning && !Settings.Transient)
        {
            try { LastPlayedWith.Observe(identity ?? resolved); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            { Log.Error("Saving recent group members failed", ex); }
        }
        if(!_replaying)
        {
            HiddenCubes.Observe(resolved); Resources.Observe(identity ?? resolved);
            _resourceProgress?.Observe(identity ?? resolved);
            _realPets.Observe(resolved);
            if(resolved is Core.Events.SelfIdentifiedEvent own)_petProgress?.Identity(own);
        }
        if (!_replaying) PartyPreview.Observe(resolved);
        _realTracker.Process(resolved);
        if (value is Core.Events.PlayerSeenEvent && identity is not null)
        {
            _realTracker.Process(identity);
            _realPets.Observe(identity);
            _petProgress?.Identity(identity);
            PartyPreview.Observe(identity);
        }
    }
    private readonly Core.Game.PetCollection _realPets=new();
    public Core.Game.PetCollection Pets => _testPets ?? _realPets;
    public Core.Game.ResourceBalances Resources {get;}=new();
    internal IReadOnlyList<ResourceProgress> ResourceHistory=>_resourceProgress?.Snapshot()??(Resources.SavedProgress() is {} current?new[]{current}:Array.Empty<ResourceProgress>());
    public Core.Game.HiddenCubes HiddenCubes {get;}=new();
    public void StartCapture()
    {
        lock (_captureGate)
        {
            if (_exiting || CaptureFactory is null || _capture is not null) return;
            try
            {
                _capture = CaptureFactory(Data);

                _capture.EventDecoded += ProcessLiveEvent;
                _capture.StatusChanged += status =>
                {
                    LiveIdentity.ObserveCaptureStatus(status, () => { HiddenCubes.Reset(); Resources.Reset(); _resourceProgress?.Suspend(); _realPets.InvalidateLocation(); PartyPreview.Reset(); LastPlayedWith.Reset(); _realTracker.InvalidateParty(); });
                };
                _capture.Start();
            }
            catch (Exception ex)
            {
                Log.Error("Capture start failed", ex);
                _capture?.Dispose();
                _capture = new FailedSource(ex.Message);
            }
        }
    }

    public void RestartCapture(bool preserveSession=false)
    {
        lock (_restartGate) RestartCaptureCore(preserveSession);
    }

    private void RestartCaptureCore(bool preserveSession)
    {
        IEventSource? previous;
        lock (_captureGate)
        {
            if (_exiting) return;
            _restarting = true;
            previous = _capture;
            _capture = null;
        }
        try
        {
            previous?.Dispose();
            if (_exiting) return;
            // Measurement reset retains the current session's already confirmed identity and party.
            // A settings-driven capture restart still requires fresh confirmation.
            if(!preserveSession){LiveIdentity.Suspend();PartyPreview.Reset();LastPlayedWith.Reset();_realTracker.InvalidateParty();_resourceProgress?.Suspend();}
            HiddenCubes.Reset();Resources.MarkCached();_realPets.InvalidateLocation();
            StartCapture();
        }
        finally
        {
            _restarting = false;
        }
    }

    /// <summary>Clears displayed combat values without restarting capture or forgetting the party.</summary>
    internal bool DpsDisplayCleared {get;private set;}
    internal void ClearDpsDisplay()
    {
        Tracker.Reset();
        DpsDisplayCleared=true;
    }
    internal void ResumeDpsDisplay()=>DpsDisplayCleared=false;
    /// <summary>Explicitly restarts measurement and capture while retaining confirmed session identities.</summary>
    public void Restart()
    {
        if(DemoRunning){ _testTracker!.Reset(); StartDemoFight(); return; }
        _demo?.Dispose();
        Tracker.Reset();
        if (_replaying) return;
        _restarting = true;
        Task.Run(()=>RestartCapture(preserveSession:true)); // closing the adapter can take a moment: not on the UI thread
    }

    private bool _animateTest;
    public void StartDemo(int seconds = 45, bool animate = true)
    {
        if (_exiting || DemoRunning || _replaying) return;
        HiddenCubes.Reset();
        _realTracker.Tick(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        _testTracker = new CombatTracker(Data,new MeterOptions { PartyOnly=false });
        _testPets = new();
        TestStartedAt=DateTimeOffset.UtcNow;
        _testTimers=new BossTimers(Path.Combine(AppSettings.AppDataDir,"test-boss-timers.json"),Data,DataDirectory,persistent:false);
        _testTimers.SetCurrentServer(2305);
        _testTimers.OnNotice(new Core.Combat.BossNotice(2400800,1110,"TEST",Core.Combat.BossNoticeKind.Killed,TestStartedAt.ToUnixTimeMilliseconds()-70000,ServerId:2305));
        _testTimers.SetRespawn(2305,2400800,5);
        var now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _testPets.Observe(new Core.Events.SelfIdentifiedEvent(now,1,"Test player",2305,Core.Events.GameClass.Cleric));
        _testPets.Observe(new Core.Game.PetCollectionEvent(now,new Dictionary<int,Core.Game.PetState> { {1193,new(1193,1,11)},{1176,new(1176,3,0)},{1047,new(1047,2,41)} }));
        foreach(var (actor,code) in new[] {(7u,2400455),(8u,2400707),(9u,2100453)})
        { _testPets.Observe(new Core.Events.NpcSeenEvent(now,actor,code));_testPets.Observe(new Core.Events.DamageEvent(now,1,actor,17010000,100,Core.Events.HitFlags.None)); }
        _animateTest=animate;
        if(animate)StartDemoFight(seconds);
    }
    private void StartDemoFight(int seconds=45)
    {
        _demo?.Dispose();
        var tracker=_testTracker!;
        _demo=new DemoFightSource(tracker.Process);
        _demo.Start(TimeSpan.FromSeconds(seconds));
    }
    public void StopDemo()
    {
        if(!DemoRunning)return;
        _demo?.Dispose(); _demo=null;
        _testTracker=null; _testPets=null;_testTimers=null;
        _realTimers.SetCurrentServer(_realTracker.SelfServerId);
        _realTracker.Tick(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    public void Tick()
    {
        if(_exiting)return;
        if(!Settings.Transient)ChecklistSchedule.Update(Settings,DateTimeOffset.UtcNow);
        if (DemoRunning && _animateTest && _demo?.IsRunning != true) StartDemoFight();
        // The wall clock means nothing to a recording's timestamps: idle checks wait until the replay is done.
        if (!_replaying) Tracker.Tick(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        // Persist the name map a few seconds after it changes (not on every packet).
        if (Settings.Transient || _replaying || DemoRunning) return;
        var now = Environment.TickCount64;
        if (now - _lastNamesCheck < 5_000) return;
        _lastNamesCheck = now;
        SaveNames();
    }

    private readonly NameCache _names = new(Path.Combine(AppSettings.AppDataDir, "names-cache.json"));
    private int _savedNamesVersion;
    private long _lastNamesCheck;

    private void SaveNames()
    {
        var version = Tracker.CacheVersion;
        if (version == _savedNamesVersion) return;
        _savedNamesVersion = version;
        _names.Save(Tracker.ExportCache());
    }

    /// <summary>All supported languages for the interface and skill / NPC names, applied without a restart.</summary>
    public void SetLanguage(string language)
    {
        language = UiText.Normalize(language);
        Settings.Language = language;
        Data.ReloadNames(DataDirectory, language);
        UiText.Use(language);
        Settings.Save();
    }

    public void ApplySettings()
    {
        Updates.ApplyPreferences();
        var oldMode=Options.TargetMode;
        Settings.ApplyTo(Options);
        if(oldMode!=Options.TargetMode){_realTracker.Reset();_testTracker?.Reset();}
        if(_testTracker is not null)_testTracker.Options.TargetMode=Options.TargetMode;
        StreamNames.Enabled=Settings.StreamMode;
        WindowPresentation.SetScale(Settings.UiScale);
        Icons.Enabled = Settings.DownloadIcons;
        Portraits.Enabled = Settings.DownloadIcons;
        Settings.Save();
    }

    /// <summary>Boss fights are always kept; trash only when long enough (or everything, per settings).</summary>
    private bool ShouldSave(EncounterSnapshot s)
    {
        if (_replaying || DemoRunning || !Settings.SaveHistory || s.CombatMs < Options.MinSavedFightMs) return false;
        var boss = s.Boss is not null;
        return Settings.HistoryMode switch
        {
            HistoryMode.BossesOnly => boss,
            HistoryMode.BossesAndLong => boss || s.CombatMs >= Settings.HistoryMinSeconds * 1000L,
            _ => true,
        };
    }

    /// <summary>Fight title in the current language (fights saved earlier may have used another one, or none).</summary>
    public string DisplayTitle(string savedTitle, int bossCode, int targetCode)
    {
        var code = bossCode != 0 ? bossCode : targetCode;
        if (code == 0 || !Data.Npcs.ContainsKey(code)) return savedTitle;
        var name = Data.NpcName(code);
        var plus = bossCode == 0 ? savedTitle.LastIndexOf(" +", StringComparison.Ordinal) : -1;
        return plus > 0 ? name + savedTitle[plus..] : name;
    }

    public string DisplayTitle(HistoryEntry e) => DisplayTitle(e.Title, e.BossCode, e.TargetCode);

    /// <summary>Portrait of the fight's boss, or of its main target when there was none (null until downloaded).</summary>
    public System.Windows.Media.ImageSource? PortraitOf(EncounterSnapshot s) => Portraits.Of(s);

    public System.Windows.Media.ImageSource? PortraitOf(HistoryEntry e) => Portraits.Of(e.BossCode, e.TargetCode);

    /// <summary>Fights saved before map names were known carry "Map 1110": resolve those now.</summary>
    public string? DisplayZone(string? zone) =>
        zone is not null && zone.StartsWith("Map ", StringComparison.Ordinal) && int.TryParse(zone.AsSpan(4), out var id)
            ? Data.MapName(id)
            : zone;

    private volatile bool _exiting;

    private string? _lastRosterLine;
    private CaptureHealth? _lastHealth;

    /// <summary>
    /// One line per boss fight: how many players' hits arrived, and whether the capture lost packets since the last one —
    /// tells "the server never sent it" (only own hits, nothing dropped) from "we lost it" in a player's log.
    /// </summary>
    private void LogBossFight(EncounterSnapshot s)
    {
        if (_replaying || DemoRunning || s.Boss is null) return;
        CaptureHealth? health;
        lock (_captureGate) health = (_capture as LiveCapture)?.Health();
        var own = s.MostDamageUnsent ? " (most damage not sent)" : "";
        var capture = "";
        if (health is { } now)
        {
            // The adapter's counters restart when it is reopened.
            var before = _lastHealth is { } last && last.Received <= now.Received && last.Gaps <= now.Gaps ? last : default;
            capture = $"; capture since the last boss: {now.Received - before.Received:N0} packets, {now.Dropped - before.Dropped:N0} dropped, " +
                      $"{now.Gaps - before.Gaps:N0} stream gaps";
        }
        _lastHealth = health;
        Log.Info($"Boss fight {s.Title} ({s.Reason}, {Format.Clock(s.CombatMs)}): {s.Fighting} attacking, {s.PlayerCount} listed, " +
                 $"{s.DamageSenders} with hits{own}, " +
                 $"counted {Format.Compact(s.TotalDamage)}{capture}");
    }

    private readonly object _fightSaveGate = new();
    private FightRecord? _lastFinishedFight;
    private Guid? _lastSavedFight;
    public bool CanSaveLastFight { get { if(DemoRunning)return false; lock (_fightSaveGate) return _lastFinishedFight is { } r && r.Summary.Id != _lastSavedFight; } }

    public string? SaveLastFight()
    {
        if(DemoRunning)return null;
        lock (_fightSaveGate)
            return _lastFinishedFight is { } r && r.Summary.Id != _lastSavedFight ? SaveFight(r) : null;
    }

    private string SaveFight(FightRecord record)
    {
        lock (_fightSaveGate)
        {
            var path = History.Save(record);
            _lastSavedFight = record.Summary.Id;
            FightSaved?.Invoke(path);
            return path;
        }
    }

    public void DeleteSavedFights()
    {
        lock (_fightSaveGate)
        {
            foreach (var entry in History.List()) History.Delete(entry.Path);
            _lastSavedFight = null;
        }
    }

    private void OnEncounterFinished(FightRecord record)
    {
        if (!_replaying && !DemoRunning && !Settings.Transient)
        {
            CaptureHealth? health;
            lock (_captureGate) health = (_capture as LiveCapture)?.Health();
            var clean = health is null || (health.Value.Dropped == 0 && health.Value.Gaps == 0);
            try { PersonalBests.Record(record, clean); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            { Log.Error("Saving personal boss records failed", ex); }
        }
        LogBossFight(record.Summary);
        if (!_replaying && !DemoRunning)
            lock (_fightSaveGate) _lastFinishedFight = record;
        if (!ShouldSave(record.Summary)) return;
        if (_exiting) Save(); // the process is about to end: a worker thread would not get to it
        else ThreadPool.QueueUserWorkItem(_ => Save());

        void Save()
        {
            try
            {
                SaveFight(record);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log.Error("Saving fight failed", ex);
            }
        }
    }

    public void Dispose()
    {
        IEventSource? capture;
        lock (_captureGate)
        {
            if (_exiting) return;
            _exiting = true;
            capture = _capture;
            _capture = null;
        }
        var live = !Settings.Transient && !_replaying && !DemoRunning;
        // A final boss callback also reads _captureGate; never wait for it while holding that lock.
        lock (_restartGate) capture?.Dispose();
        _replay?.Dispose();
        _demo?.Dispose();
        if (live)
        {
            // Closing the meter mid-fight keeps the fight so far, like the restart button does.
            _exiting = true;
            Tracker.Reset();
            SaveNames();
        }
        Updates.Dispose();
        _petProgress?.Dispose();
        _resourceProgress?.Dispose();
        if(!Settings.Transient)PetFarmCache.Save(Pets);
        Icons.Dispose();
        Portraits.Dispose();
    }

    /// <summary>Placeholder source that only reports why capture could not start.</summary>
    private sealed class FailedSource(string message) : IEventSource
    {
        public event Action<Core.Events.GameEvent>? EventDecoded { add { } remove { } }
        public event Action<CaptureStatus>? StatusChanged { add { } remove { } }
        public CaptureStatus Status { get; } = new(CaptureState.Error, message);
        public void Start() { }
        public void Stop() { }
        public void Dispose() { }
    }
}
