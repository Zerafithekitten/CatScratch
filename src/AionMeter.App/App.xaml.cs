using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using AionMeter.App.Services;
using AionMeter.App.ViewModels;
using AionMeter.App.Windows;
using AionMeter.Core.Capture;

namespace AionMeter.App;

public static class AppHost
{
    public static App Current => (App)Application.Current;

    /// <summary>Clipboard writes fail while another process holds the clipboard; retry briefly. False if it never let go.</summary>
    public static bool CopyText(string text)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                Clipboard.SetText(text);
                return true;
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                Thread.Sleep(30);
            }
        }
        return false;
    }
}

public partial class App : Application
{
    private const string ShowRequestName = "AionDpsMonitor.ShowOverlay"; // set by a second start of the app
    private Mutex? _singleInstance;
    private EventWaitHandle? _showRequest;
    private RegisteredWaitHandle? _showWait;
    private MeterService _meter = null!;
    private OverlayWindow _overlay = null!;
    private TrayIcon? _tray;
    private HotkeyManager? _hotkeys;
    private readonly WindowVisibilityGroup _windowVisibility=new();
    private DispatcherTimer? _timer;
    private HistoryWindow? _history;
    private SettingsWindow? _settingsWindow;
    private BossTimersWindow? _timersWindow;
    private TimerHubWindow? _timerHub;
    private PetTrackerWindow? _petWindow;
    private TimerCards? _timerCards;
    private readonly Core.Events.EventAlerts _eventAlerts = new();
    private EventSoundPlayer? _eventSoundPlayer;
    private EventSoundsWindow? _soundWindow;
    private PatchNotesWindow? _patchNotes;
    private readonly Dictionary<object, BreakdownWindow> _breakdowns = new();
    private int _ticks;
    private bool _shutdownStarted;
    private System.Threading.Timer? _exitDeadline;
    private string? _shutdownCheckPath;
    public bool IsExiting => _shutdownStarted || Dispatcher.HasShutdownStarted;
    public void ExitProgram()
    {
        if (_shutdownStarted) return;
        _shutdownStarted = true;
        Shutdown();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (ArgValue(e.Args,"--clean-update") is { } cleanupStage)
        {
            try
            {
                if (!int.TryParse(ArgValue(e.Args,"--parent"),out var parent)) throw new InvalidDataException("Missing cleanup requester.");
                DirectUpdate.Clean(cleanupStage,parent);
                Shutdown();
            }
            catch (Exception error) { Log.Error("Update cleanup deferred",error); Shutdown(1); }
            return;
        }
        if (ArgValue(e.Args, "--check-update-start") is { } packagePath)
        {
            try
            {
                DirectUpdate.Launch(packagePath, Core.Updates.UpdateFeed.Normalize(typeof(App).Assembly.GetName().Version!),
                    ArgValue(e.Args, "--update-check") ?? throw new InvalidDataException("Missing check result path."));
                Shutdown();
            }
            catch (Exception error)
            {
                File.WriteAllText(packagePath + ".failure.txt", error.ToString());
                Shutdown(1);
            }
            return;
        }
        if (ArgValue(e.Args, "--apply-update") is { } updateTarget)
        {
            try
            {
                if (!int.TryParse(ArgValue(e.Args, "--parent"), out var parent)) throw new InvalidDataException("Missing update requester.");
                DirectUpdate.Run(updateTarget, parent, ArgValue(e.Args, "--update-check"));
                Shutdown();
            }
            catch (Exception error)
            {
                System.Windows.MessageBox.Show(error.Message, AppInfo.Name, MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
            return;
        }
        if (ArgValue(e.Args, "--check-performance") is { } performancePath)
        {
            Log.Enabled = false;
            SampleRenderer.CheckPerformance(performancePath);
            Shutdown();
            return;
        }
        if (ArgValue(e.Args, "--check-shutdown") is { } shutdownPath)
        {
            Log.Enabled = false;
            _shutdownCheckPath = shutdownPath;
            _meter = new MeterService(new AppSettings { Transient = true, DownloadIcons = false }, null);
            _timerCards = new TimerCards(_meter);
            _timerCards.Saved.SelectedEvents = ["shugo"];
            _timerCards.Pin("auto:shugo");
            _overlay = new OverlayWindow(_meter);
            _timerHub = new TimerHubWindow(_timerCards);
            _timersWindow = new BossTimersWindow(_meter);
            _soundWindow = new EventSoundsWindow(_timerCards, (_, _) => { });
            _patchNotes = new PatchNotesWindow(_meter.Updates);
            foreach (var window in new Window[] { _overlay, _timerHub, _timersWindow, _soundWindow, _patchNotes })
            {
                window.Left = -30000; window.Top = -30000;
                window.ShowInTaskbar = false; window.ShowActivated = false; window.Show();
            }
            Dispatcher.BeginInvoke(() => ((System.Windows.Controls.Button)_overlay.FindName("ShellHideButton"))
                .RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent)), DispatcherPriority.ApplicationIdle);
            return;
        }
        if (ArgValue(e.Args,"--check-event-audio") is { } eventAudioResult)
        {
            Log.Enabled=false;
            Dispatcher.BeginInvoke(async () =>
            {
                try { await EventAudioChecks.RunAsync(eventAudioResult); Shutdown(); }
                catch(Exception ex) { File.WriteAllText(eventAudioResult,ex.ToString()); Shutdown(1); }
            });
            return;
        }
        if (ArgValue(e.Args,"--check-audio") is { } audio && ArgValue(e.Args,"--output") is { } audioResult)
        {
            Log.Enabled=false;
            Dispatcher.BeginInvoke(async () =>
            {
                try { var duration=await CustomEventAudio.DurationAsync(audio); await CustomEventAudio.ValidateAsync(audio); File.WriteAllText(audioResult,"Accepted: "+duration.TotalSeconds); Shutdown(); }
                catch(Exception ex) { File.WriteAllText(audioResult,ex.Message); Shutdown(1); }
            });
            return;
        }
        if (ArgValue(e.Args,"--check-pet-cache") is { } recording && ArgValue(e.Args,"--output") is { } resultPath)
        {
            Log.Enabled=false;
            try{PetProgressCache.CheckRecording(recording,resultPath);Shutdown();}
            catch(Exception ex){File.WriteAllText(resultPath,ex.ToString());Shutdown(1);}
            return;
        }
        if (ArgValue(e.Args, "--check-interaction") is { } checkPath)
        {
            Log.Enabled = false;
            try { InteractionChecks.Run(checkPath); Shutdown(); }
            catch (Exception ex) { File.WriteAllText(checkPath, ex.ToString()); Shutdown(1); }
            return;
        }
        if (ArgValue(e.Args, "--render-sample") is { } samplePath)
        {
            Log.Enabled = false;
            // --render-sample <out.png | folder with --animate> [--lang ru] [--window …] [--width N] [--height N] [--tab …]
            // [--backdrop game|dark|none] [--scale 1.5] [--row-size 50..100] [--animate <seconds> [--fps 10]]
            var options = new SampleRenderer.Options(
                ArgValue(e.Args, "--window") ?? "overlay",
                int.TryParse(ArgValue(e.Args, "--width"), out var width) ? width : null,
                int.TryParse(ArgValue(e.Args, "--height"), out var height) ? height : null,
                ArgValue(e.Args, "--tab"),
                ArgValue(e.Args, "--backdrop") ?? "game",
                double.TryParse(ArgValue(e.Args, "--scale"), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var scale) ? scale : 1.5,
                int.TryParse(ArgValue(e.Args, "--row-size"), out var rowSize) ? rowSize : 100);
            var language = ArgValue(e.Args, "--lang") ?? "en";
            if (double.TryParse(ArgValue(e.Args, "--animate"), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var seconds))
                SampleRenderer.Animate(samplePath, language, options, seconds, int.TryParse(ArgValue(e.Args, "--fps"), out var fps) ? fps : 10);
            else
            {
                try { SampleRenderer.Render(samplePath, language, options); }
                catch (Exception error) { File.WriteAllText(samplePath + ".error.txt", error.ToString()); Shutdown(1); return; }
            }
            Shutdown();
            return;
        }
        if (!AdministratorStartup.Ensure(e.Args)) { Shutdown(); return; }
        _singleInstance = new Mutex(true, "AionDpsMonitor.SingleInstance", out var first);
        if (!first)
        {
            // Started again (the shortcut clicked twice): the running meter shows its overlay, this copy leaves quietly.
            if (EventWaitHandle.TryOpenExisting(ShowRequestName, out var running))
                using (running) running.Set();
            else // a version from before 0.2.2 is running
                MessageBox.Show($"{AppInfo.Name} is already running — look for the gold \"A\" in the notification area.", AppInfo.Name);
            Shutdown();
            return;
        }

        DispatcherUnhandledException += (_, ex) =>
        {
            Log.Error("UI exception", ex.Exception);
            ex.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, ex) => Log.Error("Fatal exception", ex.ExceptionObject as Exception);

        var settings = AppSettings.Load();
        settings.AutoHideSeconds = 0;
        settings.ClickThrough = false;
        settings.ShowGear = true;
        if (Core.Updates.UpdateFeed.IsConfigured && settings.UpdateSource != Core.Updates.UpdateFeed.Repository)
        {
            var renamedRepository = settings.UpdateSource == "Zerafithekitten/zerafi-dps-monitor";
            settings.UpdateSource = Core.Updates.UpdateFeed.Repository;
            if (!renamedRepository)
            {
                settings.CheckUpdates = true;
                settings.AutoInstallUpdates = false;
            }
        }
        if (!Core.Updates.UpdateFeed.IsConfigured) { settings.CheckUpdates = false; settings.AutoInstallUpdates = false; }
        settings.Save(); // persist any migration done while loading
        UiText.Use(settings.Language);
        Func<Core.Game.GameData, IEventSource> factory = data => new LiveCapture(data, new LiveCaptureOptions
        {
            DeviceName = settings.CaptureDevice,
            RecentDebugEnabled = settings.RecentDebugEnabled,
            RecordDirectory = settings.RecordPackets ? Path.Combine(AppSettings.AppDataDir, "captures") : null,
        });

        _meter = new MeterService(settings, factory);
        _timerCards = new TimerCards(_meter);
        Log.Info($"Started. Data: {_meter.Data.Skills.Count} skills, {_meter.Data.Npcs.Count} npcs.");
        _meter.StartCapture();

        _overlay = new OverlayWindow(_meter);
        if (settings.OverlayVisible || e.Args.Contains("--show")) _overlay.Show(); // --show: this run only, not saved
        else
        {
            new WindowInteropHelper(_overlay).EnsureHandle();
            // Hidden by choice: still up for a moment, so it is plain that the meter started.
            if (settings.ShowOnStart) ShowByItself();
        }

        _hotkeys = new HotkeyManager(new WindowInteropHelper(_overlay).Handle);
        RegisterHotkeys();

        _tray = new TrayIcon(new TrayActions(
            ToggleOverlay: () => SetOverlayVisible(!_overlay.IsVisible),
            ToggleClickThrough: ToggleClickThrough,
            Reset: RestartMeter,
            History: ShowHistory,
            BossTimers: ShowBossTimers,
            Events: ShowTimerHub,
            Settings: ShowSettings,
            Demo: () => _meter.StartDemo(),
            Replay: PickReplay,
            ToggleLanguage: ToggleLanguage,
            Update: () => ShowUpdate(),
            CheckUpdates: () => _ = CheckUpdatesFromTrayAsync(),
            Exit: () =>
            {
                Log.Info("Exit chosen in the tray menu");
                ExitProgram();
            }));
        SessionEnding += (_, end) => Log.Info($"Windows {end.ReasonSessionEnding}: closing");
        if (_timerCards!.Saved.EventWindowOpen) ShowTimerHub();

        // A new player's first start: without Npcap nothing can be measured — say so and offer the download.
        if (!LiveCapture.IsNpcapInstalled())
            Dispatcher.BeginInvoke(ShowNpcapHelp, DispatcherPriority.ApplicationIdle);

        // Another start of the app asks this one to show the overlay.
        _showRequest = new EventWaitHandle(false, EventResetMode.AutoReset, ShowRequestName);
        _showWait = ThreadPool.RegisterWaitForSingleObject(_showRequest,
            (_, _) => Dispatcher.BeginInvoke(() => SetOverlayVisible(true)), null, Timeout.Infinite, executeOnlyOnce: false);

        // New versions on GitHub: tray item, the overlay's update button and one notification per version.
        _meter.Updates.Changed += OnUpdatesChanged;
        _meter.Updates.Start();
        Updater.CleanDownloads();
        if (e.Args.Contains("--updated")) // started by the installer after an in-app update
            Dispatcher.BeginInvoke(() => _tray?.ShowBalloon(AppInfo.Name,
                string.Format(UiText.Current.UpdatedBalloon, _meter.Updates.Current.ToString(3))), DispatcherPriority.ApplicationIdle);

        _timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(200) };
        _timer.Tick += (_, _) => OnTick();
        _timer.Start();
        if(settings.ShowPetTrackerModule) TogglePetModule();

        // Developer switches: --demo, --replay <file.pcap> (instant), --open-last (breakdown of the newest fight).
        if (e.Args.Contains("--demo")) _meter.StartDemo();
        if (ArgValue(e.Args, "--replay") is { } replay) _meter.Replay(replay);
        if (e.Args.Contains("--open-last"))
        {
            var once = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            once.Tick += (_, _) =>
            {
                once.Stop();
                var seg = _meter.Tracker.Segments().OrderByDescending(s => s.TotalDamage).FirstOrDefault();
                if (seg is null || _meter.Tracker.Snapshot(seg.Id, 0) is not { } snap || snap.Combatants.Count == 0) return;
                var who = snap.Combatants.FirstOrDefault(c => c.IsSelf) ?? snap.Combatants[0];
                ShowBreakdown(seg.Id, who.ActorId);
                if (ArgValue(e.Args, "--tab") is { } tab && _breakdowns.TryGetValue(seg.Id, out var w)) w.ShowTab(tab);
            };
            once.Start();
        }
        if (ArgValue(e.Args,"--update-ready") is { } readyStage) DirectUpdate.MarkReady(readyStage);
        else if (e.Args.Contains("--updated"))
            try { DirectUpdate.CleanLegacyAfterStartup(); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception) { Log.Error("Legacy update cleanup deferred",error); }
    }

    public static void ShowNpcapHelp()
    {
        var answer = MessageBox.Show(
            "Live DPS requires Npcap. The driver captures network traffic; the meter does not modify game files.\n\n" +
            "Open the official download page now? Download and install Npcap yourself.\n\n" +
            "Then restart the meter and teleport once or change zones in game. Your character is only confirmed after fresh identity data arrives.",
            "Npcap missing – open download?", MessageBoxButton.YesNo, MessageBoxImage.Information);
        if (answer != MessageBoxResult.Yes) return;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://npcap.com/#download") { UseShellExecute = true });
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            Log.Error("Could not open the Npcap page", ex);
        }
    }

    private static string? ArgValue(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    private void OnTick()
    {
        _meter.Tick();
        UpdateAutoVisibility();
        if (_overlay.IsVisible) _overlay.Refresh();
        if (++_ticks % 5 == 0)
        {
            _tray?.Update(_overlay.IsVisible, _meter.Settings.ClickThrough, _meter.DemoRunning ? "demo" : _meter.CaptureStatus.Message);
            CheckBossAlerts();
            CheckEventSounds();
            CheckGameStart();


        }
    }

    // ------------------------------------------------------------ automatic show / hide

    private bool _autoShown;           // the overlay is up because a fight brought it up, not the user
    private Guid? _suppressedFight;    // hidden by hand during this fight: do not pop it back until the next one
    private long _suppressedPartyRevision = -1;
    private long _lastEngagedTick;

    /// <summary>
    /// A boss being fought nearby, or any fight you take part in, brings the overlay up. One that came up by itself
    /// goes away again after <see cref="AppSettings.AutoHideSeconds"/> out of combat (never while the mouse is on it).
    /// </summary>
    private void UpdateAutoVisibility()
    {
        var s = _meter.Settings;
        var fight = _meter.Tracker.LiveFight();
        var party = _meter.PartyPreview.Snapshot();
        var fighting = fight is { } f && (f.Boss || f.Self);
        var engaged = fighting || party.Members.Count > 0;
        var now = Environment.TickCount64;
        if (engaged) _lastEngagedTick = now;
        if (!s.AutoShow || _windowVisibility.Hidden) return;

        var canShow = fighting ? fight!.Value.Id != _suppressedFight
            : party.Members.Count > 0 && party.Revision != _suppressedPartyRevision;
        if (engaged && !_overlay.IsVisible && canShow)
        {
            _overlay.Show();
            _autoShown = true;
        }
        else if (!engaged && _autoShown && _overlay.IsVisible && s.AutoHideSeconds > 0 &&
                 now - _lastEngagedTick > s.AutoHideSeconds * 1000L && !_overlay.IsMouseOver)
        {
            _overlay.Hide();
            _autoShown = false;
        }
    }

    /// <summary>Up without the user asking: goes away again after the usual idle time (Settings → Overlay).</summary>
    private void ShowByItself()
    {
        if (_overlay.IsVisible || _windowVisibility.Hidden) return;
        _overlay.Show();
        _autoShown = true;
        _lastEngagedTick = Environment.TickCount64;
    }

    private CaptureState _lastCaptureState = CaptureState.Stopped;
    private int[] _gameShownFor = [];

    /// <summary>
    /// The overlay comes up when the meter starts seeing the game's traffic, once per launch of the game: a reconnect
    /// after a loading screen is the same game process and does not count.
    /// </summary>
    private void CheckGameStart()
    {
        var state = _meter.CaptureStatus.State;
        var connected = state == CaptureState.Capturing && _lastCaptureState != CaptureState.Capturing;
        _lastCaptureState = state;
        if (!connected || !_meter.Settings.ShowOnStart || _meter.DemoRunning) return;
        var game = GameProcessLocator.FindGameProcessIds().Order().ToArray();
        if (game.Length == 0 || game.SequenceEqual(_gameShownFor)) return;
        _gameShownFor = game;
        ShowByItself();
    }

    private bool _hotkeysSuspended;

    public void SuspendHotkeys()
    {
        _hotkeysSuspended = true;
        _hotkeys?.UnregisterAll();
    }

    public void ResumeHotkeys()
    {
        if (_shutdownStarted) return;
        if (!_hotkeysSuspended) return;
        _hotkeysSuspended = false;
        RegisterHotkeys();
    }

    private void RegisterHotkeys()
    {
        if (_hotkeys is null || _hotkeysSuspended) return;
        _hotkeys.UnregisterAll();
        var s = _meter.Settings;
        _hotkeys.Register(s.HotkeyToggleOverlay, () => SetOverlayVisible(!_overlay.IsVisible));
        _hotkeys.Register(s.HotkeyToggleAll,()=>_windowVisibility.Toggle(Windows.Cast<Window>(),_overlay));
        _hotkeys.Register(s.HotkeyReset, RestartMeter);
        _overlay.ApplyHotkeyTips();
        if (!_hotkeys.Register(s.HotkeyClickThrough, ToggleClickThrough))
        {
            var reason = _hotkeys.LastRegistrationError == 5
                ? "Windows refused to register a global hotkey."
                : "The shortcut is invalid or already used by another application.";
            Dispatcher.BeginInvoke(() => MessageBox.Show(
                $"Could not register {s.HotkeyClickThrough}. {reason}\n\nChoose another shortcut in Settings > HOTKEYS. You can also toggle click-through from the system tray icon.",
                "Hotkey registration failed", MessageBoxButton.OK, MessageBoxImage.Information));
        }
        _hotkeys.Register(s.HotkeyTimers, ToggleBossTimers);
        _hotkeys.Register(s.HotkeyPersonal,_overlay.TogglePersonalCombat);
        _hotkeys.Register(s.HotkeyEvents,ToggleEventModule);
        _hotkeys.Register(s.HotkeyPets,TogglePetModule);
    }

    /// <summary>
    /// The overlay's restart button, the tray item and its hotkey: when the numbers look wrong, start over without
    /// closing the app — the fight so far is saved, capture restarts, and the players and bosses the meter knows stay
    /// known (a restart of the whole app mid-fight could not learn them again until the next loading screen).
    /// </summary>
    public void RestartMeter()
    {
        Log.Info("Meter restarted by the user");
        _meter.Restart();
        _overlay.ShowLive();
    }

    // ------------------------------------------------------------ window management (used by windows via AppHost)

    public void SetOverlayVisible(bool visible)
    {
        if(visible && _windowVisibility.Hidden)_windowVisibility.Restore();
        _autoShown = false; // the user decided: automatic hiding no longer applies
        if (visible)
        {
            _overlay.Show();
            _overlay.Refresh();
        }
        else
        {
            _overlay.Hide();
            _suppressedFight = _meter.Tracker.LiveFight()?.Id;
            _suppressedPartyRevision = _meter.PartyPreview.Snapshot().Revision;
        }
        _meter.Settings.OverlayVisible = visible;
        _meter.Settings.Save();
    }

    public void ToggleClickThrough()
    {
        var on = !_meter.Settings.ClickThrough;
        _overlay.SetClickThrough(on);
        _overlay.Refresh();
        _tray?.Update(_overlay.IsVisible, on, _meter.CaptureStatus.Message);
    }

    public void ShowBreakdown(Guid segmentId, uint actorId) =>
        ShowBreakdown(segmentId, new LiveFightView(_meter.Tracker, segmentId), actorId);

    public void ShowBreakdown(IFightView view, uint actorId) => ShowBreakdown(view, view, actorId);

    private void ShowBreakdown(object key, IFightView view, uint actorId)
    {
        if (_breakdowns.TryGetValue(key, out var existing))
        {
            existing.Select(actorId);
            Activate(existing);
            return;
        }
        var w = new BreakdownWindow(view, actorId, _meter.Data, _meter.Icons, _meter.Portraits);
        // Detailed statistics are interactive tools, independent of the meter's display lock and opacity.
        _breakdowns[key] = w;
        w.Closed += (_, _) => _breakdowns.Remove(key);
        w.Show();
    }

    /// <summary>All supported languages, live: names reload and the meter re-binds.</summary>
    public void SetLanguage(string language)
    {
        _meter.SetLanguage(language);
        var reopenPets=_petWindow?.IsVisible==true;
        _petWindow?.Close();
        foreach (var w in _breakdowns.Values.ToList()) w.Close();
        _history?.Close();
        _timersWindow?.Close();
        _updateWindow?.Close();
        _soundWindow?.Close();
        var reopenEvents = _timerHub?.IsVisible == true;
        _timerHub?.Close();
        if (reopenEvents) ShowTimerHub();
        _tray?.ApplyTexts();
        _overlay.OnLanguageChanged();
        if(reopenPets)TogglePetModule();
    }

    public void ToggleLanguage() => SetLanguage(UiText.Languages[(Array.IndexOf(UiText.Languages, UiText.Current.Code) + 1) % UiText.Languages.Length]);

    public void ShowSavedInOverlay(Core.Storage.HistoryEntry entry)
    {
        if (!_overlay.IsVisible) SetOverlayVisible(true);
        _overlay.ShowSaved(entry);
    }

    public void ShowHistory()
    {
        if (_history is { IsLoaded: true })
        {
            _history.Reload();
            Activate(_history);
            return;
        }
        _history = new HistoryWindow(_meter);
        // Fight history is an interactive tool, independent of meter locking and opacity.
        _history.Closed += (_, _) => _history = null;
        _history.Show();
    }

    /// <summary>Hotkey: open the boss timers, or close them when they are already up.</summary>
    private void ToggleBossTimers()
    {
        if (_timersWindow is { IsLoaded: true }) _timersWindow.Close();
        else ShowBossTimers();
    }

    public void ShowBossTimers()
    {
        if (_timersWindow is { IsLoaded: true })
        {
            Activate(_timersWindow);
            return;
        }
        _timersWindow = new BossTimersWindow(_meter);
        WindowClickThrough.Attach(_timersWindow, () => _meter.Settings.ClickThrough, () => _meter.Settings.WindowOpacity);
        _timersWindow.Closed += (_, _) => _timersWindow = null;
        _timersWindow.Show();
        _windowVisibility.Track(_timersWindow);
    }

    private void ToggleEventModule()
    {
        if(_timerHub is {IsLoaded:true})_timerHub.Close();
        else OpenTimerHub(false);
    }
    public void ShowTimerHub()=>OpenTimerHub(true);
    private void OpenTimerHub(bool interactive)
    {
        // Opening the event controls is an explicit request to interact with the displays.
        if (interactive && _meter.Settings.ClickThrough) ToggleClickThrough();
        if (_timerHub is { IsLoaded: true }) { Activate(_timerHub); return; }
        _timerHub = new TimerHubWindow(_timerCards!);
        WindowClickThrough.Attach(_timerHub, () => _meter.Settings.ClickThrough, () => _timerCards!.Saved.EventOpacity);
        _timerHub.Closed += (_, _) => _timerHub = null;
        _timerHub.Show();
        _windowVisibility.Track(_timerHub);
    }

    /// <summary>Tray notification (and a chime) shortly before a tracked boss respawns.</summary>
    private void CheckBossAlerts()
    {
        var t = UiText.Current;
        foreach (var timer in _meter.Timers.TakeDueAlerts(DateTimeOffset.Now, _meter.Settings.BossAlertMinutes))
        {
            var next = timer.NextSpawn!.Value;
            var minutes = (int)Math.Ceiling((next - DateTimeOffset.Now).TotalMinutes);
            var name = _meter.Data.NpcName(timer.NpcCode);
            var text = minutes > 0
                ? string.Format(t.AlertSoon, name, minutes, next.ToString("HH:mm"))
                : string.Format(t.AlertNow, name, next.ToString("HH:mm"));
            _tray?.ShowBalloon(t.TimersTitle, text);
            if (_timerCards?.Saved.Sounds.TryGetValue("tracked-boss", out var sound) == true && sound.Enabled)
                (_eventSoundPlayer ??= new()).Play(sound, _timerCards.Saved.SoundVolume);
        }
    }

    private void CheckEventSounds()
    {
        if (_timerCards is not { } cards || cards.Saved.Sounds.Count == 0) return;
        var enabled = cards.Saved.Sounds.Where(p => p.Value.Enabled && cards.Saved.SelectedEvents.Contains(p.Key)).Select(p => p.Key);
        foreach (var due in _eventAlerts.TakeDue(DateTimeOffset.UtcNow, cards.Saved.EventRegion, enabled, cards.Saved.SoundLeadMinutes))
            (_eventSoundPlayer ??= new()).Play(cards.Saved.Sounds[due.Id], cards.Saved.SoundVolume);
    }

    public void ShowEventSounds()
    {
        if (_soundWindow is { IsLoaded: true }) { Activate(_soundWindow); return; }
        _soundWindow = new EventSoundsWindow(_timerCards!, (sound, volume) => (_eventSoundPlayer ??= new()).Play(sound, volume, preview: true));
        _soundWindow.Closed += (_, _) => _soundWindow = null;
        _soundWindow.Show();
    }

    public void ShowPatchNotes()
    {
        if (_patchNotes is { IsLoaded: true }) { Activate(_patchNotes); return; }
        _patchNotes = new PatchNotesWindow(_meter.Updates);
        _patchNotes.Closed += (_, _) => _patchNotes = null;
        _patchNotes.Show();
    }
    public void TogglePetModule()
    {
        if(_petWindow is not null){_petWindow.Close();return;}
        _petWindow=new PetTrackerWindow(_meter);

        _meter.Settings.ShowPetTrackerModule=true;_meter.Settings.Save();
        WindowClickThrough.Attach(_petWindow,()=>_meter.Settings.ClickThrough,()=>_meter.Settings.WindowOpacity);
        _petWindow.Closed+=(_,_)=>{_petWindow=null;if(!IsExiting){_meter.Settings.ShowPetTrackerModule=false;_meter.Settings.Save();}};
        _petWindow.Show();
        _windowVisibility.Track(_petWindow);
    }

    public void ShowSettings()
    {
        if (_settingsWindow is { IsLoaded: true })
        {
            Activate(_settingsWindow);
            return;
        }
        _settingsWindow = new SettingsWindow(_meter);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    // ------------------------------------------------------------ updates

    private UpdateWindow? _updateWindow;
    private Version? _announcedUpdate;
    private bool _checkingByHand;

    /// <summary>A check finished or a version was skipped: the tray item follows; a new version is announced once.</summary>
    private void OnUpdatesChanged()
    {
        if (_shutdownStarted || Dispatcher.HasShutdownStarted) return;
        var updates = _meter.Updates;
        var available = updates.Available;
        _tray?.SetUpdate(available?.Version.ToString(3));
        if (available is null || available.Version == _announcedUpdate) return;
        var t = UiText.Current;
        _announcedUpdate = available.Version;
        if (_checkingByHand) return; // the one who asked gets the update window instead
        _tray?.ShowBalloon(t.UpdateTitle, string.Format(t.UpdateBalloon, available.Version.ToString(3)), () => ShowUpdate());
    }

    /// <summary>The update window for <paramref name="release"/>, by default the available update.</summary>
    public void ShowUpdate(Core.Updates.ReleaseInfo? release = null)
    {
        release ??= _meter.Updates.Available;
        if (release is null) return;
        if (_updateWindow is { IsLoaded: true } open && open.Release.Version == release.Version)
        {
            Activate(open);
            return;
        }
        _updateWindow?.Close();
        var w = new UpdateWindow(_meter.Updates, release, Updater.IsInstalled);
        w.Closed += (_, _) =>
        {
            if (_updateWindow == w) _updateWindow = null;
        };
        _updateWindow = w;
        w.Show();
    }

    /// <summary>"Check for updates" by hand (tray or settings): asks GitHub now; a newer version opens the update window.</summary>
    public async Task<UpdateCheckResult> CheckUpdatesNowAsync()
    {
        UpdateCheckResult result;
        _checkingByHand = true;
        try
        {
            result = await _meter.Updates.CheckAsync();
        }
        finally
        {
            _checkingByHand = false;
        }
        if (result == UpdateCheckResult.Available) ShowUpdate(_meter.Updates.Latest);
        return result;
    }

    public async Task CheckUpdatesFromTrayAsync()
    {
        var t = UiText.Current;
        switch (await CheckUpdatesNowAsync())
        {
            case UpdateCheckResult.UpToDate:
                _tray?.ShowBalloon(AppInfo.Name, t.UpToDate);
                break;
            case UpdateCheckResult.Failed:
                _tray?.ShowBalloon(AppInfo.Name, string.Format(t.UpdateCheckFailed, _meter.Updates.LastError));
                break;
        }
    }

    public void OnSettingsChanged()
    {
        RegisterHotkeys();
        _overlay.ApplyAppearance();
        _overlay.Refresh();
    }

    /// <summary>The Settings window's row size slider, shown on the overlay before it is saved.</summary>
    public void PreviewRowSize(int percent) => _overlay?.ApplyRowSize(percent);

    private void PickReplay()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Replay an AION 2 capture",
            Filter = "Packet captures (*.pcap;*.pcapng)|*.pcap;*.pcapng|All files|*.*",
            InitialDirectory = Path.Combine(AppSettings.AppDataDir, "captures"),
        };
        if (dialog.ShowDialog() == true) _meter.Replay(dialog.FileName);
    }

    private static void Activate(Window w)
    {
        if (w.WindowState == WindowState.Minimized) w.WindowState = WindowState.Normal;
        w.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _shutdownStarted = true;
        // Only this process: a stuck capture driver must not leave the meter in Task Manager.
        // This background timer does not keep a normally closed process alive.
        _exitDeadline ??= new System.Threading.Timer(_ => Environment.Exit(e.ApplicationExitCode), null,
            TimeSpan.FromSeconds(10), Timeout.InfiniteTimeSpan);
        if (_meter is not null) Log.Info("Exiting");
        _timer?.Stop();
        _timerCards?.Dispose();
        _showWait?.Unregister(null);
        _showRequest?.Dispose();
        _hotkeys?.Dispose();
        _tray?.Dispose();
        _eventSoundPlayer?.Dispose();
        if (_meter is not null)
        {
            try { _meter.Settings.Save(); }
            finally { _meter.Dispose(); }
        }
        _singleInstance?.Dispose();
        if (_shutdownCheckPath is not null)
            File.WriteAllText(_shutdownCheckPath, $"Windows remaining: {Windows.Count}; pinned card retained: {_timerCards?.Saved.Positions.GetValueOrDefault("auto:shugo")?.Open == true}");
        base.OnExit(e);
    }
}
