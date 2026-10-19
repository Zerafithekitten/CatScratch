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
    private DispatcherTimer? _topmostTimer;
    private DispatcherTimer? _gameTimer;
    private GameExitGate _gameExit = new();
    private string? _watchedGame;
    private HistoryWindow? _history;
    private PersonalBestsWindow? _personalBests;
    private LastPlayedWithWindow? _lastPlayedWith;
    private SettingsWindow? _settingsWindow;
    private BossTimingWindow? _bossTimingWindow;
    private BossTimersWindow? _timersWindow;
    private TimerHubWindow? _timerHub;
    private EventScheduleWindow? _eventSchedule;
    private TimerHubWindow? _startedEvents;
    private PetTrackerWindow? _petWindow;
    private DeathRecapWindow? _deathRecapWindow;
    private HiddenCubeWindow? _hiddenCubeWindow;
    private ChecklistWindow? _checklistWindow;
    private EventSoundPlayer? _hiddenCubeSoundPlayer;
    private long _hiddenCubeSoundVersion;
    private long _deathRecapStamp;
    private (string Language, bool Stream) _deathRecapAppearance;
    private TimerCards? _timerCards;
    private readonly Core.Events.EventAlerts _eventAlerts = new();
    private EventSoundPlayer? _eventSoundPlayer;
    private EventSoundsWindow? _soundWindow;
    private PatchNotesWindow? _patchNotes;
    private readonly Dictionary<object, BreakdownWindow> _breakdowns = new();
    private int _ticks;
    private ModulePreviewSession? _modulePreview;
    private bool _previewOverlayWasVisible;
    private Window[] _previewHiddenWindows=[];
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
        if(e.Args.Any(a=>a.StartsWith("--check-",StringComparison.Ordinal) || a=="--render-sample"))
        {
            WindowPresentation.Enabled=false;
            // Test windows may close before assertions finish. Only their explicit result should end the process.
            if(!e.Args.Contains("--check-shutdown"))ShutdownMode=ShutdownMode.OnExplicitShutdown;
        }
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
            _startedEvents = new TimerHubWindow(_timerCards, startedOnly:true);
            _timersWindow = new BossTimersWindow(_meter);
            _soundWindow = new EventSoundsWindow(_timerCards, (_, _) => { });
            _patchNotes = new PatchNotesWindow(_meter.Updates);
            _checklistWindow=new ChecklistWindow(_meter.Settings);
            foreach (var window in new Window[] { _overlay, _timerHub, _startedEvents, _timersWindow, _soundWindow, _patchNotes, _checklistWindow })
            {
                window.Left = -30000; window.Top = -30000;
                window.ShowInTaskbar = false; window.ShowActivated = false; window.Show();
            }
            Dispatcher.BeginInvoke(() => ((System.Windows.Controls.Button)_overlay.FindName("ShellHideButton"))
                .RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent)), DispatcherPriority.ApplicationIdle);
            return;
        }
        if (ArgValue(e.Args,"--check-release250") is { } release250Result)
        {
            Log.Enabled=false;
            Dispatcher.BeginInvoke(async () =>
            {
                try { await Release250Checks.RunAsync(release250Result); Shutdown(); }
                catch(Exception ex) { File.WriteAllText(release250Result,ex.ToString()); Shutdown(1); }
            });
            return;
        }
        if (ArgValue(e.Args, "--check-beta4") is { } beta4Result)
        {
            Log.Enabled = false;
            Dispatcher.BeginInvoke(async () =>
            {
                try { await Beta4Checks.RunAsync(beta4Result); Shutdown(); }
                catch (Exception error) { File.WriteAllText(beta4Result, error.ToString()); Shutdown(1); }
            });
            return;
        }
        if (ArgValue(e.Args, "--check-boss-records") is { } bossRecordsResult)
        {
            Log.Enabled = false;
            try { BossRecordChecks.Run(bossRecordsResult); Shutdown(); }
            catch (Exception error) { File.WriteAllText(bossRecordsResult, error.ToString()); Shutdown(1); }
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
        if(ArgValue(e.Args,"--check-theme-restart") is { } themeCheckPath)
        {
            Log.Enabled=false;
            try{ThemeChecks.CheckRestart(themeCheckPath,e.Args.Contains("--write"));Shutdown();}
            catch(Exception error){File.WriteAllText(themeCheckPath+".error.txt",error.ToString());Shutdown(1);}
            return;
        }
        if(ArgValue(e.Args,"--check-checklist") is { } checklistPath)
        {
            Log.Enabled=false;
            try
            {
                var checks=new List<string>();
                ChecklistChecks.Run(checklistPath,(ok,message)=>{if(!ok)throw new InvalidOperationException(message);checks.Add(message);});
                File.WriteAllText(checklistPath,System.Text.Json.JsonSerializer.Serialize(checks));Shutdown();
            }
            catch(Exception error){File.WriteAllText(checklistPath,error.ToString());Shutdown(1);}
            return;
        }
        if(ArgValue(e.Args,"--check-placement") is { } placementPath)
        {
            Log.Enabled=false;
            try{OverlayPlacementChecks.Run(placementPath);Shutdown();}
            catch(Exception error){File.WriteAllText(placementPath,error.ToString());Shutdown(1);}
            return;
        }
        if(ArgValue(e.Args,"--check-topmost") is { } topmostPath)
        {
            Log.Enabled=false;
            try{TopmostChecks.Run(topmostPath);Shutdown();}
            catch(Exception error){File.WriteAllText(topmostPath,error.ToString());Shutdown(1);}
            return;
        }
        if(ArgValue(e.Args,"--check-world-boss-events") is { } worldBossPath)
        {
            Log.Enabled=false;
            try{WorldBossEventChecks.Run(worldBossPath);Shutdown();}
            catch(Exception error){File.WriteAllText(worldBossPath,error.ToString());Shutdown(1);}
            return;
        }
        if(ArgValue(e.Args,"--check-clickthrough-startup") is { } startupPath)
        {
            Log.Enabled=false;
            try{ClickThroughStartupChecks.Run(startupPath);Shutdown();}
            catch(Exception error){File.WriteAllText(startupPath,error.ToString());Shutdown(1);}
            return;
        }
        if(ArgValue(e.Args,"--check-boss-timing") is { } timingPath)
        {
            Log.Enabled=false;
            try{BossTimingChecks.Run(timingPath);Shutdown();}
            catch(Exception error){File.WriteAllText(timingPath,error.ToString());Shutdown(1);}
            return;
        }
        if(ArgValue(e.Args,"--check-enhancements") is { } enhancementsPath)
        {
            Log.Enabled=false;
            try{EnhancementChecks.Run(enhancementsPath);Shutdown();}
            catch(Exception error){File.WriteAllText(enhancementsPath,error.ToString());Shutdown(1);}
            return;
        }
        if(ArgValue(e.Args,"--check-panel") is { } panelPath)
        {
            Log.Enabled=false;
            try{ControlPanelChecks.Run(panelPath);Shutdown();}
            catch(Exception error){File.WriteAllText(panelPath,error.ToString());Shutdown(1);}
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
                int.TryParse(ArgValue(e.Args, "--row-size"), out var rowSize) ? rowSize : 100,
                double.TryParse(ArgValue(e.Args,"--ui-scale"),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var uiScale)?uiScale:1,
                e.Args.Contains("--stream"),e.Args.Contains("--test-mode"));
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
        if(e.Args.Contains("--remove-game-start-task"))
        {
            Dispatcher.BeginInvoke(async () =>
            {
                try { await GameLifetime.Configure(false, ""); Shutdown(); }
                catch(Exception error) { Log.Error("Could not remove game-start task",error); Shutdown(1); }
            });
            return;
        }
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
        WindowPresentation.ConfigureDisplay(settings.PreferredDisplay);
        ThemeColors.Apply(settings.Theme);
        settings.AutoHideSeconds = 0;
        settings.ApplyStartupClickThrough();
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
        // Persist startup migrations together after the service and release-channel transition below.
        UiText.Use(settings.Language);
        Func<Core.Game.GameData, IEventSource> factory = data => new LiveCapture(data, new LiveCaptureOptions
        {
            DeviceName = settings.CaptureDevice,
            RecentDebugEnabled = settings.RecentDebugEnabled,
            RecordDirectory = settings.RecordPackets ? Path.Combine(AppSettings.AppDataDir, "captures") : null,
        });

        WindowPresentation.SetScale(settings.UiScale);
        StreamNames.Enabled=settings.StreamMode;
        _meter = new MeterService(settings, factory);
        if (settings.ApplyReleaseTransition(_meter.Updates.CurrentIdentity))
            Log.Info("Stable release promotion: update channel changed from Beta to Stable.");
        settings.Save();
        _timerCards = new TimerCards(_meter);
        Log.Info($"Started. Data: {_meter.Data.Skills.Count} skills, {_meter.Data.Npcs.Count} npcs.");
        _meter.StartCapture();

        _overlay = new OverlayWindow(_meter);
        _overlay.ShowInTaskbar=false;
        if(e.Args.Contains("--show"))settings.ShowDpsMeterModule=true; // explicit request re-enables the display
        if (settings.ShowDpsMeterModule && (settings.OverlayVisible || e.Args.Contains("--show"))) _overlay.Show();
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
            Demo: ToggleTestMode,
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
        Dispatcher.BeginInvoke(() =>
        {
            if (!IsExiting && _timerCards!.Saved.EventWindowOpen) ShowTimerHub();
            if (!IsExiting && _timerCards!.Saved.StartedEventWindowOpen) OpenStartedEvents();
        }, DispatcherPriority.ApplicationIdle);

        // A new player's first start: without Npcap nothing can be measured — say so and offer the download.
        Dispatcher.BeginInvoke(() =>
        {
            if (!IsExiting && !LiveCapture.IsNpcapInstalled()) ShowNpcapHelp();
        }, DispatcherPriority.ApplicationIdle);

        // Another start of the app asks this one to show the overlay.
        _showRequest = new EventWaitHandle(false, EventResetMode.AutoReset, ShowRequestName);
        _showWait = ThreadPool.RegisterWaitForSingleObject(_showRequest,
            (_, _) => Dispatcher.BeginInvoke(() => SetOverlayVisible(true)), null, Timeout.Infinite, executeOnlyOnce: false);

        // New versions on GitHub: tray item, the overlay's update button and one notification per version.
        _meter.Updates.Changed += OnUpdatesChanged;
        _meter.Updates.Start();
        _ = Task.Run(Updater.CleanDownloads);
        if (e.Args.Contains("--updated")) // started by the installer after an in-app update
            Dispatcher.BeginInvoke(() => _tray?.ShowBalloon(AppInfo.Name,
                string.Format(UiText.Current.UpdatedBalloon, _meter.Updates.CurrentDisplay)), DispatcherPriority.ApplicationIdle);

        _timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(200) };
        _timer.Tick += (_, _) => OnTick();
        _timer.Start();
        // Independent of combat refresh and Background-priority dispatcher traffic.
        _topmostTimer=new DispatcherTimer(DispatcherPriority.Normal){Interval=TimeSpan.FromMilliseconds(250)};
        _topmostTimer.Tick+=(_,_)=>{if(!_shutdownStarted)WindowPresentation.MaintainTopmost();};
        _topmostTimer.Start();
        _watchedGame=settings.GameExecutable;
        if(e.Args.Contains("--game-session")) _gameExit.ShouldClose(settings.FollowGame,true,DateTimeOffset.UtcNow);
        _gameTimer=new DispatcherTimer(DispatcherPriority.Background) { Interval=TimeSpan.FromSeconds(2) };
        _gameTimer.Tick+=(_,_)=>
        {
            if(IsExiting || settings.Transient) return;
            if(_watchedGame!=settings.GameExecutable) { _gameExit=new(); _watchedGame=settings.GameExecutable; }
            if(_gameExit.ShouldClose(settings.FollowGame,settings.FollowGame && GameLifetime.IsRunning(settings.GameExecutable),DateTimeOffset.UtcNow))
            { Log.Info("AION 2 closed: ending companion session"); ExitProgram(); }
        };
        _gameTimer.Start();
        if(settings.FollowGame && !settings.Transient && GameLifetime.NeedsQuietMigration)
            Dispatcher.BeginInvoke(async () =>
            {
                try { await GameLifetime.Configure(true,settings.GameExecutable); }
                catch(Exception error) { Log.Error("Quiet game watcher migration deferred",error); }
            },DispatcherPriority.ApplicationIdle);
        Dispatcher.BeginInvoke(() =>
        {
            if (!IsExiting && settings.ShowPetTrackerModule) TogglePetModule();
        }, DispatcherPriority.ApplicationIdle);

        // Developer switches: --demo, --replay <file.pcap> (instant), --open-last (breakdown of the newest fight).
        if (e.Args.Contains("--demo")) ToggleTestMode();
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
        if(_modulePreview is null)UpdateAutoVisibility();
        SyncMeterControls();
        SyncDeathRecap();
        SyncHiddenCubes();
        SyncChecklist();
        _overlay.Refresh(); // The permanent panel also needs current fight, save and update state.
        SyncBossTiming();
        if (++_ticks % 5 == 0)
        {
            _tray?.Update(_overlay.IsVisible, _meter.Settings.ClickThrough, _meter.DemoRunning ? "demo" : _meter.CaptureStatus.Message);
            CheckBossAlerts();
            CheckEventSounds();
            CheckGameStart();


        }
    }

    // ------------------------------------------------------------ automatic show / hide

    private MeterControlsWindow? _meterControls;
    private void SyncMeterControls()
    {
        if(_shutdownStarted)return;
        if(_windowVisibility.Hidden)return;
        if(_meterControls is null)
        {
            _meterControls=new MeterControlsWindow(_meter.Settings,ShowSettings,()=>SetOverlayVisible(!_overlay.IsVisible)) {ShowInTaskbar=true};
            _overlay.MoveControlsTo(_meterControls);
            _meterControls.Closing+=(_,_)=>{if(!IsExiting)ExitProgram();};
            _meterControls.Closed+=(_,_)=>_meterControls=null;
        }
        if(!_meterControls.IsVisible)_meterControls.Show();
    }

    private void SyncBossTiming()
    {
        if(_shutdownStarted || _modulePreview is not null)return;
        if(!_meter.Settings.DetachBossTiming){_bossTimingWindow?.Close();return;}
        if(_bossTimingWindow is not null || _windowVisibility.Hidden)return;
        var window=_bossTimingWindow=new BossTimingWindow(_meter.Settings,_overlay.DataContext,()=>_overlay.Refresh());
        WindowClickThrough.Attach(window,()=>_meter.Settings.ClickThrough,()=>_meter.Settings.WindowOpacity);
        window.Closed+=(_,_)=>{if(ReferenceEquals(_bossTimingWindow,window))_bossTimingWindow=null;};
        window.Show();_windowVisibility.Track(window);
    }

    private void SyncChecklist()
    {
        if(_shutdownStarted)return;
        if(!_meter.Settings.ShowChecklistModule){_checklistWindow?.Close();return;}
        if(_checklistWindow is not null){_checklistWindow.ApplyPreferences();return;}
        if(_windowVisibility.Hidden)return;
        var window=_checklistWindow=new ChecklistWindow(_meter.Settings);
        WindowClickThrough.Attach(window,()=>_meter.Settings.ClickThrough,()=>_meter.Settings.WindowOpacity);
        window.Closed+=(_,_)=>{if(ReferenceEquals(_checklistWindow,window))_checklistWindow=null;};
        window.Show();_windowVisibility.Track(window);
    }
    private void SyncHiddenCubes()
    {
        var version=_meter.HiddenCubes.DetectionVersion;
        var newDetection=version!=_hiddenCubeSoundVersion;
        _hiddenCubeSoundVersion=version;
        if(_shutdownStarted || !_meter.Settings.ShowHiddenCubeModule || _meter.DemoRunning || _meter.HiddenCubes.Count(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())==0)
        {_hiddenCubeWindow?.Close();_hiddenCubeWindow=null;return;}
        if(newDetection && _meter.Settings.HiddenCubeSoundEnabled && !string.IsNullOrWhiteSpace(_meter.Settings.HiddenCubeSoundPath))
            (_hiddenCubeSoundPlayer??=new()).Play(new EventSoundPreference {Enabled=true,Sound="Custom audio",FilePath=_meter.Settings.HiddenCubeSoundPath},_meter.Settings.HiddenCubeSoundVolume);
        if(_windowVisibility.Hidden || _hiddenCubeWindow is not null)return;
        var window=_hiddenCubeWindow=new HiddenCubeWindow(_meter.Settings);
        window.Closed+=(_,_)=>{if(ReferenceEquals(_hiddenCubeWindow,window))_hiddenCubeWindow=null;};
        WindowClickThrough.Attach(window,()=>_meter.Settings.ClickThrough,()=>_meter.Settings.WindowOpacity);
        window.Show();_windowVisibility.Track(window);
    }

    private void SyncDeathRecap()
    {
        var stamp = _meter.Tracker.DeathRecapAtMs;
        if (!_meter.Settings.ShowDeathRecapModule || stamp == 0)
        {
            _deathRecapWindow?.Close(); _deathRecapWindow = null; _deathRecapStamp = 0;
            return;
        }
        var appearance = (UiText.Current.Code, StreamNames.Enabled);
        if (_deathRecapWindow is not null && stamp == _deathRecapStamp && appearance == _deathRecapAppearance) return;
        var recap = _meter.Tracker.DeathRecap();
        if (recap is null) return;
        _deathRecapStamp = stamp; _deathRecapAppearance = appearance;
        if (_deathRecapWindow is not null) { _deathRecapWindow.Reload(recap); return; }
        var window = _deathRecapWindow = new DeathRecapWindow(recap,_meter.Settings);
        WindowClickThrough.Attach(window, () => _meter.Settings.ClickThrough, () => _meter.Settings.WindowOpacity);
        window.PlaceAboveCenter(_overlay);
        window.Closed += (_, _) => { if (_deathRecapWindow == window) _deathRecapWindow = null; };
        window.Show(); _windowVisibility.Track(window);
    }

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
        if (!s.ShowDpsMeterModule || !s.AutoShow || _windowVisibility.Hidden) return;

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
        if (!_meter.Settings.ShowDpsMeterModule || _overlay.IsVisible || _windowVisibility.Hidden) return;
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
        _hotkeys.Register(s.HotkeyToggleAll,()=>_windowVisibility.Toggle(Windows.Cast<Window>(),_overlay,s.ShowDpsMeterModule));
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
        _hotkeys.Register(s.HotkeyDeathRecap,_overlay.ToggleDeathRecap);
        _hotkeys.Register(s.HotkeyChecklist,_overlay.ToggleChecklist);
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

    public void SetOverlayVisible(bool visible,bool updateModule=true)
    {
        if(updateModule)_meter.Settings.ShowDpsMeterModule=visible;
        visible=visible && _meter.Settings.ShowDpsMeterModule;
        if(!visible)_windowVisibility.Forget(_overlay);
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
        SyncMeterControls();
        _meterControls?.RefreshText();
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

    internal void ShowBreakdown(object key, IFightView view, uint actorId, MeterService? source = null)
    {
        if (_breakdowns.TryGetValue(key, out var existing))
        {
            existing.Select(actorId);
            if(source?.Settings.Transient!=true) Activate(existing);
            return;
        }
        var meter=source ?? _meter;
        var w = new BreakdownWindow(view, actorId, meter.Data, meter.Icons, meter.Portraits);
        if(meter.Settings.Transient)
        { w.Left=-30000; w.Top=-30000; w.WindowStartupLocation=WindowStartupLocation.Manual; w.ShowInTaskbar=false; w.ShowActivated=false; }
        // Detailed statistics are interactive tools, independent of the meter's display lock and opacity.
        _breakdowns[key] = w;
        w.Closed += (_, _) => _breakdowns.Remove(key);
        w.Show();
    }

    /// <summary>All supported languages, live: names reload and the meter re-binds.</summary>
    public void SetLanguage(string language)
    {
        _meter.SetLanguage(language);
        WindowPinning.RefreshAll();
        var reopenPets=_petWindow?.IsVisible==true;
        _petWindow?.Close();
        _hiddenCubeWindow?.Close();
        _bossTimingWindow?.Close();
        foreach (var w in _breakdowns.Values.ToList()) w.Close();
        _history?.Close();
        _personalBests?.Close();
        _lastPlayedWith?.Close();
        _timersWindow?.Close();
        _updateWindow?.Close();
        _soundWindow?.Close();
        var reopenSchedule=_eventSchedule?.IsVisible==true;
        _eventSchedule?.Close();
        if(reopenSchedule)ShowEventSchedule();
        var reopenEvents = _timerHub?.IsVisible == true;
        _timerHub?.Close();
        if (reopenEvents) ShowTimerHub();
        var reopenStarted = _startedEvents?.IsVisible == true;
        _startedEvents?.Close();
        if (reopenStarted) OpenStartedEvents();
        _tray?.ApplyTexts();
        _meterControls?.RefreshText();
        _overlay.OnLanguageChanged();
        if(reopenPets)TogglePetModule();
    }

    public void ToggleTestMode()
    {
        if(_modulePreview is not null)
        {
            _modulePreview.Dispose();_modulePreview=null;_meter.StopDemo();
            foreach(var window in _previewHiddenWindows)if(window.IsLoaded)window.Show();
            _previewHiddenWindows=[];
            if(_previewOverlayWasVisible && _meter.Settings.ShowDpsMeterModule)_overlay.Show();
            _overlay.Refresh();return;
        }
        if(!_meter.DemoRunning)
        {
            var preview=new ModulePreviewSession(_meter.Settings,_timerCards?.Saved);
            try
            {
                _previewOverlayWasVisible=_overlay.IsVisible;
                _previewHiddenWindows=Windows.Cast<Window>().Where(w=>w.IsVisible && w!=_settingsWindow && w!=_meterControls).ToArray();
                foreach(var window in _previewHiddenWindows)window.Hide();
                _meter.StartDemo();_modulePreview=preview;preview.Show(settingsWindow:_settingsWindow,controlsPanel:_meterControls);_overlay.Hide();
            }
            catch{_modulePreview=null;preview.Dispose();_meter.StopDemo();foreach(var window in _previewHiddenWindows)if(window.IsLoaded)window.Show();_previewHiddenWindows=[];throw;}
            return;
        }
        var petsVisible=_petWindow?.IsVisible==true;
        var bossWindow=_timersWindow;
        var bossesVisible=bossWindow?.IsVisible==true;
        var bossPosition=bossWindow is null ? (Left:0d,Top:0d,Width:780d,Height:640d) : (Left:bossWindow.Left,Top:bossWindow.Top,Width:bossWindow.Width,Height:bossWindow.Height);
        _petWindow?.Close();_timersWindow?.Close();
        if(_meter.DemoRunning)_meter.StopDemo();else _meter.StartDemo();
        foreach(var w in _breakdowns.Values.ToList())w.Close();
        _history?.Close();
        if(petsVisible)TogglePetModule();
        if(bossesVisible){ShowBossTimers();_timersWindow!.Left=bossPosition.Left;_timersWindow.Top=bossPosition.Top;_timersWindow.Width=bossPosition.Width;_timersWindow.Height=bossPosition.Height;}
        _timerHub?.Reload();
        _startedEvents?.Reload();
        _overlay.ShowLive();_overlay.Refresh();
        if(_meter.Settings.ShowDpsMeterModule && _meter.DemoRunning && !_overlay.IsVisible && !_windowVisibility.Hidden)_overlay.Show();
    }
    public void RefreshPresentation()
    {
        foreach(var w in _breakdowns.Values.ToList())w.Close();
        _personalBests?.Reload();
        _lastPlayedWith?.Reload();
        _history?.Reload();_overlay.OnLanguageChanged();
    }
    public void ToggleLanguage() => SetLanguage(UiText.Languages[(Array.IndexOf(UiText.Languages, UiText.Current.Code) + 1) % UiText.Languages.Length]);

    public void ShowSavedStatistics(Core.Storage.HistoryEntry entry)
    {
        _overlay.OpenSavedStatistics(entry);
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

    public void ShowPersonalBests()
    {
        if (_personalBests is { IsLoaded: true }) { _personalBests.Reload(); Activate(_personalBests); return; }
        _personalBests = new PersonalBestsWindow(_meter);
        _personalBests.Closed += (_, _) => _personalBests = null;
        _personalBests.Show();
    }

    public void ShowLastPlayedWith()
    {
        if (_lastPlayedWith is { IsLoaded: true }) { _lastPlayedWith.Reload(); Activate(_lastPlayedWith); return; }
        _lastPlayedWith = new LastPlayedWithWindow(_meter);
        _lastPlayedWith.Closed += (_, _) => _lastPlayedWith = null;
        _lastPlayedWith.Show();
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
        _timersWindow.Closed += (_, _) => _timersWindow = null;
        _timersWindow.Show();
        _windowVisibility.Track(_timersWindow);
    }

    private void ToggleEventModule()
    {
        if(_timerHub is {IsLoaded:true})_timerHub.Close();
        else OpenTimerHub();
    }
    public void ShowTimerHub()=>OpenTimerHub();
    public void ShowStartedEvents()=>OpenStartedEvents();
    public void ShowEventSchedule()
    {
        if(_eventSchedule is {IsLoaded:true}){_eventSchedule.Reload();Activate(_eventSchedule);return;}
        _eventSchedule=new EventScheduleWindow(_timerCards!);
        _eventSchedule.Closed+=(_,_)=>_eventSchedule=null;
        _eventSchedule.Show();_windowVisibility.Track(_eventSchedule);
    }
    private void OpenStartedEvents()
    {
        if (_startedEvents is { IsLoaded:true }) { if(!_meter.Settings.ClickThrough)Activate(_startedEvents); return; }
        _startedEvents = TimerHubWindow.CreateOverlay(_timerCards!, startedOnly:true);
        _startedEvents.Closed+=(_,_)=>_startedEvents=null;
        _startedEvents.Show();
        _windowVisibility.Track(_startedEvents);
    }
    private void OpenTimerHub()
    {
        if (_timerHub is { IsLoaded: true }) { if(!_meter.Settings.ClickThrough)Activate(_timerHub); return; }
        _timerHub = TimerHubWindow.CreateOverlay(_timerCards!);
        _timerHub.Closed += (_, _) => _timerHub = null;
        _timerHub.Show();
        _windowVisibility.Track(_timerHub);
    }

    /// <summary>Tray notification (and a chime) shortly before a tracked boss respawns.</summary>
    private void CheckBossAlerts()
    {
        if(_meter.DemoRunning)return;
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
            if (_timerCards?.Saved.Sounds.TryGetValue("tracked-boss", out var sound) == true && sound.Enabled && !_timerCards.HasIndividualBossSound(timer.NpcCode))
                (_eventSoundPlayer ??= new()).Play(sound, _timerCards.Saved.SoundVolume);
        }
    }

    private void CheckEventSounds()
    {
        if(_meter.DemoRunning)return;
        if (_timerCards is not { } cards || cards.Saved.Sounds.Count == 0) return;
        var enabled = cards.Saved.Sounds.Where(p => p.Value.Enabled && cards.EffectiveScheduledIds().Contains(p.Key)).Select(p => p.Key);
        foreach (var due in _eventAlerts.TakeDue(DateTimeOffset.UtcNow, cards.Saved.EventRegion, enabled, cards.Saved.SoundLeadMinutes))
            (_eventSoundPlayer ??= new()).Play(cards.Saved.Sounds[due.Id], cards.Saved.SoundVolume);
        foreach(var due in cards.TakeDueWorldBossSounds(DateTimeOffset.UtcNow))
            (_eventSoundPlayer??=new()).Play(due.Sound,cards.Saved.SoundVolume);
    }

    public void ShowEventSounds()
    {
        ShowSettings();_settingsWindow!.SelectEvents();
    }
    internal void PreviewEventSound(EventSoundPreference sound,double volume)=>(_eventSoundPlayer??=new()).Play(sound,volume,preview:true);
    internal void RefreshPetSettings(){if(_petWindow is { } window){window.Width=Math.Clamp(_meter.Settings.PetModuleWidth,150,1600);window.Refresh();}}
    internal void RefreshEventSettings()
    {
        _eventSchedule?.Reload();
        if(_timerCards is not { } cards)return;
        if(cards.Saved.EventWindowOpen){if(_timerHub is null)OpenTimerHub();}else _timerHub?.Close();
        if(cards.Saved.StartedEventWindowOpen){if(_startedEvents is null)OpenStartedEvents();}else _startedEvents?.Close();
        foreach(var pair in new[]{(Window:_timerHub,Key:"auto-events"),(Window:_startedEvents,Key:"started-events")})
            if(pair.Window is { } window){if(cards.Saved.Positions.TryGetValue(pair.Key,out var position))window.Width=Math.Clamp(position.Width,240,1000);window.Opacity=cards.Saved.EventOpacity;window.Reload();}
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
        _settingsWindow = new SettingsWindow(_meter,_timerCards);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    internal OverlayProfile CaptureOverlayProfile(string name) => OverlayProfiles.Capture(name,_meter.Settings,_timerCards!.Saved) with
    {BossTimers=_timersWindow is {IsLoaded:true} w?new CardPlacement {Left=w.Left,Top=w.Top,Width=w.Width,Height=w.Height,Open=w.IsVisible}:null};
    internal void ApplyOverlayProfile(OverlayProfile profile)
    {
        // Validate first, then close displays before restoring saved placement (close handlers write positions).
        OverlayProfiles.Apply(profile,new AppSettings(),new TimerCards.State());
        _timerHub?.Close();_startedEvents?.Close();_petWindow?.Close();_hiddenCubeWindow?.Close();_bossTimingWindow?.Close();_timersWindow?.Close();_timerCards!.Dispose();
        OverlayProfiles.Apply(profile,_meter.Settings,_timerCards.Saved);
        var placement=new Rect(_meter.Settings.OverlayLeft,_meter.Settings.OverlayTop,_meter.Settings.OverlayWidth,_meter.Settings.OverlayHeight);
        _meter.ApplySettings();
        // Scale/size events persist the old window geometry while applying appearance.
        _meter.Settings.OverlayWidth=placement.Width;_meter.Settings.OverlayHeight=placement.Height;
        _overlay.ApplyAppearance();_overlay.Left=placement.Left;_overlay.Top=placement.Top;
        WindowPresentation.Constrain(_overlay);
        _timerCards.Save();_timerCards.Restore();
        if(profile.Upcoming)OpenTimerHub();
        if(profile.Started)OpenStartedEvents();
        if(_meter.Settings.ShowPetTrackerModule)TogglePetModule();
        if(profile.BossTimers is {Open:true} boss){ShowBossTimers();_timersWindow!.Left=boss.Left;_timersWindow.Top=boss.Top;_timersWindow.Width=Math.Max(_timersWindow.MinWidth,boss.Width);_timersWindow.Height=Math.Max(_timersWindow.MinHeight,boss.Height);WindowPresentation.Constrain(_timersWindow);}
        SetOverlayVisible(_meter.Settings.OverlayVisible,updateModule:false);
        _meterControls?.RestorePosition();
        WindowClickThrough.ApplyAll(_meter.Settings.ClickThrough);
        WindowPinning.RefreshAll();
        _overlay.Refresh();_settingsWindow?.Close();
    }

    internal void ShowBossComparison(Core.Combat.EncounterSnapshot current)
    {
        if(current.Boss is { } boss && _meter.Data.Npcs.ContainsKey(boss.NpcCode))current=current with {Boss=boss with {Name=_meter.Data.NpcName(boss.NpcCode)}};
        var now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var session=_meter.Tracker.Segments().Select(s=>_meter.Tracker.Snapshot(s.Id,now)).OfType<Core.Combat.EncounterSnapshot>();
        var saved=_meter.History.List().Where(e=>e.BossCode==current.Boss?.NpcCode).Select(e=>_meter.History.Load(e.Path)?.Summary).OfType<Core.Combat.EncounterSnapshot>();
        new BossComparisonWindow(current,session.Concat(saved),_meter.Settings).Show();
    }

    // ------------------------------------------------------------ updates

    private UpdateWindow? _updateWindow;
    private string? _announcedUpdate;
    private bool _checkingByHand;

    /// <summary>A check finished or a version was skipped: the tray item follows; a new version is announced once.</summary>
    private void OnUpdatesChanged()
    {
        if (_shutdownStarted || Dispatcher.HasShutdownStarted) return;
        var updates = _meter.Updates;
        var available = updates.Ready;
        _tray?.SetUpdate(available?.DisplayVersion);
        if (available is null || available.Tag == _announcedUpdate) return;
        var t = UiText.Current;
        _announcedUpdate = available.Tag;
        if (_checkingByHand) return; // the one who asked gets the update window instead
        _tray?.ShowBalloon(t.UpdateTitle, t.Localize("New update available") + " · " + available.DisplayVersion + ". " + t.Localize("Please restart"), () => ShowUpdate());
    }

    /// <summary>The update window for <paramref name="release"/>, by default the available update.</summary>
    public void ShowUpdate(Core.Updates.ReleaseInfo? release = null)
    {
        release ??= _meter.Updates.Available;
        if (release is null) return;
        if (_updateWindow is { IsLoaded: true } open && open.Release.Tag == release.Tag)
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

    public void OnSettingsChanged(bool dpsVisibilityChanged=false)
    {
        _meterControls?.RefreshText();
        if(dpsVisibilityChanged)SetOverlayVisible(_meter.Settings.ShowDpsMeterModule);
        RegisterHotkeys();
        _overlay.ApplyAppearance();
        _overlay.Refresh();
        SyncBossTiming();
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
        _modulePreview?.Dispose();_modulePreview=null;
        _timer?.Stop();
        _topmostTimer?.Stop();
        _gameTimer?.Stop();
        _timerCards?.Dispose();
        _showWait?.Unregister(null);
        _showRequest?.Dispose();
        _hotkeys?.Dispose();
        _tray?.Dispose();
        _eventSoundPlayer?.Dispose();
        _hiddenCubeSoundPlayer?.Dispose();
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
