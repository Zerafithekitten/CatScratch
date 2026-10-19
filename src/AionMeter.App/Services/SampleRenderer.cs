using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AionMeter.App.Windows;
using AionMeter.Core.Events;
using AionMeter.Core.Game;

namespace AionMeter.App.Services;

/// <summary>
/// Developer tool (<c>--render-sample out.png</c>): draws the overlay or a window with a scripted fight of every class
/// into a PNG, without showing a window, capturing traffic or touching settings and history — safe while the game is
/// running. <c>--animate &lt;seconds&gt;</c> writes the overlay frame by frame instead (into the folder given), for videos.
/// </summary>
public static class SampleRenderer
{
    public static void CheckPerformance(string path)
    {
        using var meter = new MeterService(new AppSettings { Transient = true, DownloadIcons = false, CompactMode = true, ShowPersonalCombatModule = true }, null);
        var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 20_500;
        new SampleFight(start, 20).FeedUntil(meter.Tracker, start + 20_000);
        var overlay = new OverlayWindow(meter);
        var cards = new TimerCards(meter, persistent: false);
        cards.Saved.SelectedEvents = ["shugo", "rift"];
        var hub = new TimerHubWindow(cards) { ClockOverride = () => new DateTimeOffset(2026, 10, 13, 13, 42, 0, TimeSpan.FromHours(2)) };
        OverlayWindow.ClockOverride = () => start + 20_000;
        for (var i = 0; i < 100; i++) { overlay.Refresh(); hub.Reload(); }
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        var clock = System.Diagnostics.Stopwatch.StartNew();
        const int count = 2000;
        for (var i = 0; i < count; i++) { overlay.Refresh(); hub.Reload(); }
        clock.Stop();
        var bytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(new {
            Iterations = count, Milliseconds = clock.Elapsed.TotalMilliseconds,
            AllocatedBytes = bytes, BytesPerRefresh = bytes / count,
            WorkingSetBytes = Environment.WorkingSet,
            Scope = "Synthetic party DPS and event refresh logic, no game capture or GPU rendering"
        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        OverlayWindow.ClockOverride = null;
    }
    /// <param name="Window">overlay (default), overlay-update, overlay-crowd (a world boss whose crowd's damage never
    /// arrives), breakdown, history, timers, settings, update or update-portable.</param>
    /// <param name="Width">Overlay width (default 560): narrow sizes show how the footer copes.</param>
    /// <param name="Tab">Breakdown tab: dps (default), accuracy, rotation or defense.</param>
    /// <param name="Backdrop">Behind the translucent overlay: game (a stand-in colour), dark (a navy gradient) or none (transparent).</param>
    /// <param name="RowSize">Overlay row size in percent (Settings → Overlay → Row size).</param>
    public sealed record Options(string Window = "overlay", int? Width = null, int? Height = null, string? Tab = null,
        string Backdrop = "game", double Scale = 1.5, int RowSize = 100, double UiScale = 1, bool StreamMode = false, bool TestMode = false);

    public static void Render(string path, string language, Options o)
    {
        WindowPresentation.SetScale(o.UiScale);StreamNames.Enabled=o.StreamMode;
        using var meter = CreateMeter(language, o.RowSize);
        meter.Settings.UiScale=o.UiScale;meter.Settings.StreamMode=o.StreamMode;
        if(o.TestMode)meter.StartDemo(animate:false);
        var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 20_500; // a fight that is still going
        var fight = new SampleFight(start, 20);
        if (o.Window is not ("party" or "room" or "personal-waiting" or "fight-menu-empty" or "personal-bests" or "last-played-with")) fight.FeedUntil(meter.Tracker, start + 20_000);
        else
        {
            meter.PartyPreview.Observe(new SelfIdentifiedEvent(start, 1, "Sample self", 2305, Core.Events.GameClass.Cleric));
            meter.PartyPreview.Observe(new PartyRosterEvent(start, [
                new("Sample self", 2305, 45, 1894, 85471, Core.Events.GameClass.Cleric),
                new("Sample mage", 2305, 45, 945, 53718, Core.Events.GameClass.Sorcerer),
                new("Sample fighter", 2305, 45, 864, 42520, Core.Events.GameClass.Gladiator),
                new("Sample spirit", 2305, 45, 1132, 52644, Core.Events.GameClass.Elementalist),
                new("Sample ranger", 2305, 45, 894, 37373, Core.Events.GameClass.Ranger)
            ], o.Window == "room" ? 42u : 0u, o.Window == "room" ? 600001 : 0, "Sample room"));
        }
        // A dungeon's time limit, 12:48 of its hour gone: the countdown in the overlay's top line.
        if (o.Window is not ("party" or "room"))
            meter.Tracker.Process(new InstanceTimerEvent(start, 600092, 2, start - 768_000, start + 2_832_000));
        if (o.Window == "overlay-crowd") fight.FeedCrowd(meter.Tracker, start + 20_000, 124);
        if(o.Window is "overlay-boss-limit" or "overlay-boss-estimate" or "overlay-boss-estimate-locked")meter.Tracker.Process(new BossTimeLimitEvent(start,900001,start+300000));
        if (o.Window == "history")
            foreach (var entry in meter.History.List().Take(20)) meter.PortraitOf(entry);
        WaitForPortraits(meter);

        ModulePreviewSession? workspace=null;
        Window host;
        Action refresh;
        switch (o.Window)
        {
            case "preview-workspace":
                workspace=new ModulePreviewSession(meter.Settings);workspace.Show(offscreen:true);
                var gallery=new WrapPanel {Margin=new(10)};
                foreach(var window in workspace.Windows)
                {
                    window.UpdateLayout();
                    var width=Math.Max(1,(int)Math.Ceiling(window.ActualWidth));var height=Math.Max(1,(int)Math.Ceiling(window.ActualHeight));
                    var bitmap=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);bitmap.Render(window);
                    var card=new StackPanel();
                    card.Children.Add(new TextBlock {Text=window.Title,FontSize=10,Foreground=ThemeColors.Brush("#C49AF5"),Margin=new(0,0,0,5),TextTrimming=TextTrimming.CharacterEllipsis});
                    card.Children.Add(new Image {Source=bitmap,Height=175,Stretch=Stretch.Uniform,HorizontalAlignment=HorizontalAlignment.Center});
                    gallery.Children.Add(new Border {Child=card,Width=245,Height=210,Padding=new(8),Margin=new(4),BorderThickness=new(1),BorderBrush=ThemeColors.Brush("#414A56"),CornerRadius=new(6)});
                }
                host=new Window {Width=1040,Height=Math.Ceiling(workspace.Windows.Count/4.0)*218+20,Content=gallery,Background=ThemeColors.Brush("#191D23")};
                refresh=()=>{};break;
            case "update-notice":
                var badgeResources=new MeterControlsWindow(meter.Settings,()=>{},()=>{});
                host=new Window {Width=72,Height=26,Background=Brushes.Transparent,Content=MeterControlsWindow.CreateInlineUpdateButton(badgeResources)};
                refresh=()=>{};break;
            case "meter-controls-update":
            case "meter-controls-vertical-update":
            case "meter-controls-grid-update":
            case "meter-controls":
            case "meter-controls-vertical":
            case "meter-controls-grid":
            case "meter-controls-transparent":
                if(o.Window is "meter-controls-grid" or "meter-controls-grid-update")meter.Settings.ControlsLayout=ControlsLayout.Grid;
                if(o.Window is "meter-controls-vertical" or "meter-controls-vertical-update")meter.Settings.ControlsLayout=ControlsLayout.Vertical;
                if(o.Window=="meter-controls-transparent"){meter.Settings.WindowOpacity=.7;meter.Settings.BackgroundOpacity=.2;}
                var controlOverlay=new OverlayWindow(meter,exitOnClose:false);
                var controlPanel=new MeterControlsWindow(meter.Settings,()=>{},()=>{});
                controlOverlay.MoveControlsTo(controlPanel);
                controlPanel.SizeToContent=SizeToContent.Height;controlPanel.Height=double.NaN;
                host=controlPanel;refresh=()=>{controlOverlay.Refresh();if(o.Window.EndsWith("-update"))controlPanel.SetUpdateAvailable(true);};break;
            case "color-picker":
                host=new Window{Width=304,Height=198,Background=ThemeColors.Brush("#20242A"),Content=new Border{Padding=new(12),Child=new Controls.HsvColorPicker(Colors.Red)}};
                refresh=()=>{};break;
            case "event-schedule":
                var plannerCards=new TimerCards(meter,persistent:false);
                plannerCards.SetFavorite("shugo",true);
                var planner=new EventScheduleWindow(plannerCards){ClockOverride=()=>new DateTimeOffset(2026,10,16,20,15,0,TimeSpan.FromHours(2)),Width=o.Width??1100,Height=o.Height??540};
                host=planner;refresh=planner.Reload;break;
            case "profiles":
                host=new Window {Width=740,Height=460,Background=new SolidColorBrush(Color.FromRgb(32,36,42)),Foreground=Brushes.WhiteSmoke,Content=new Border {Padding=new(18),Child=new Windows.OverlayProfilesPanel()}};
                host.Resources.MergedDictionaries.Add(new ResourceDictionary {Source=new Uri("/CatScratch;component/Theme/MeterTools.xaml",UriKind.Relative)});
                refresh=()=>{};break;
            case "boss-comparison":
                var comparison=meter.Tracker.Snapshot(null,start+20000)!;
                comparison=comparison with {IsActive=false,Reason=Core.Combat.EncounterEndReason.Kill,MapId=600001,BossStartObserved=true,Combatants=comparison.Combatants.Select(c=>c with {ServerId=2305}).ToArray()};
                var earlier=comparison with {Id=Guid.NewGuid(),StartedAt=comparison.StartedAt.AddDays(-1),CombatMs=comparison.CombatMs+5000,Combatants=comparison.Combatants.Select(c=>c with {Damage=(long)(c.Damage*.9),Dps=c.Dps*.8,Healing=25000,DamageTaken=45000,Deaths=1}).ToArray()};
                host=new BossComparisonWindow(comparison,[earlier]);refresh=()=>{};break;
            case "pets":
            case "pets-locked":
            case "pets-three":
            case "pets-region":
            case "pets-three-locked":
                meter.Settings.PetGoal=2;
                meter.Pets.Observe(new SelfIdentifiedEvent(start,1,"Sample",2305,GameClass.Cleric));
                meter.Pets.Observe(new PetCollectionEvent(start,new Dictionary<int,PetState>{{1193,new(1193,1,11)},{1176,new(1176,3,0)}}));
                meter.Pets.Observe(new NpcSeenEvent(start,7,2400455,X:11500,Y:21000));
                meter.Pets.Observe(new DamageEvent(start,1,7,17010000,100,HitFlags.None));
                if(o.Window.StartsWith("pets-three"))
                {
                    meter.Pets.Observe(new PetCollectionEvent(start,new Dictionary<int,PetState>{{1193,new(1193,1,11)},{1176,new(1176,3,0)},{1047,new(1047,2,41)}}));
                    meter.Pets.Observe(new NpcSeenEvent(start,8,2400707));meter.Pets.Observe(new DamageEvent(start,1,8,17010000,100,HitFlags.None));
                    meter.Pets.Observe(new NpcSeenEvent(start,9,2100453));meter.Pets.Observe(new DamageEvent(start,1,9,17010000,100,HitFlags.None));
                }
                if(o.Window=="pets-region")
                {
                    meter.Pets.Observe(new ZoneChangedEvent(start,1110,"Altgard",false));
                    meter.Pets.Observe(new NpcSeenEvent(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),7,2400455));
                }
                var pets=new PetTrackerWindow(meter){Width=o.Width??300};

                if(o.Window.EndsWith("locked"))pets.SetDisplayOnly(true);
                host=pets;refresh=pets.Refresh;break;
            case "fight-menu":
            case "fight-menu-empty":
                var menuOverlay = new OverlayWindow(meter);
                var fightMenu = menuOverlay.CreateFightMenu(menuOverlay);
                host = new Window { Width = 340, Height = o.Window == "fight-menu-empty" ? 116 : 210, Background = Brushes.Transparent };
                Capture(host, () => { }, path, o, fightMenu);
                menuOverlay.Close();
                return;
            case "personal":
            case "personal-locked":
                var sample = meter.Tracker.Snapshot(null,start+20000)!;
                var sampleSelf = sample.Combatants.First(c=>c.IsSelf).ActorId;
                var sampleOther = sample.Combatants.First(c=>!c.IsSelf && !c.IsUnknownSummons).ActorId;
                meter.Tracker.Process(new NpcHpEvent(start+20000,sampleSelf,1000,79500,IsNpc:false));
                meter.Tracker.Process(new HealEvent(start+20001,sampleSelf,sampleSelf,17010000,78500,HitFlags.None));
                meter.Tracker.Process(new NpcHpEvent(start+20002,sampleSelf,79500,79500,IsNpc:false));
                meter.Tracker.Process(new NpcHpEvent(start+20002,sampleOther,1000,232400,IsNpc:false));
                meter.Tracker.Process(new HealEvent(start+20003,sampleSelf,sampleOther,17010000,231400,HitFlags.None));
                meter.Tracker.Process(new NpcHpEvent(start+20004,sampleOther,232400,232400,IsNpc:false));
                meter.Tracker.Process(new DamageEvent(start+20003,999999,sampleSelf,17010000,42850,HitFlags.None));
                meter.Settings.ClickThrough = o.Window == "personal-locked";
                meter.Settings.ShowPersonalCombatModule = true;
                var personal = new OverlayWindow(meter) { Width=o.Width??420, Height=o.Height??610 };
                host=personal; refresh=personal.Refresh;
                break;
            case "personal-waiting":
                meter.Settings.ShowPersonalCombatModule = true;
                var waiting = new OverlayWindow(meter) { Width=o.Width??420, Height=o.Height??610 };
                host=waiting; refresh=waiting.Refresh;
                break;
            case "events":
            case "events-locked":
            case "events-live":
            case "events-started":
            case "event-settings":
            case "timer-card":
                var cards = new TimerCards(meter, persistent: false);
                cards.Saved.Events.Add(new("sample-event", "Example: Shugo Festival", "EVENT", DateTimeOffset.UtcNow.AddHours(2)));
                cards.Saved.Events.Add(new("sample-pvp", "Example: PvP event", "PVP", DateTimeOffset.UtcNow.AddMinutes(45)));
                if (o.Window == "timer-card")
                {
                    host = new TimerCardWindow(cards, "auto:shugo", new CardPlacement { Width=300 });
                    WindowClickThrough.Attach(host, () => false);
                    refresh = () => { };
                    break;
                }
                cards.Saved.SelectedEvents = ["shugo", "rift", "invasion"];
                if (o.Window == "event-settings")
                {
                    host = new EventPreferencesWindow(cards, () => 480, _ => { }, _ => { }, () => { }) { Height=o.Height??680 };
                    refresh=()=>{}; break;
                }
                var hub = new TimerHubWindow(cards,startedOnly:o.Window=="events-started") { Width = o.Width ?? 480, Height = o.Height??144, ClockOverride = () => o.TestMode ? DateTimeOffset.UtcNow : o.Window is "events-live" or "events-started" ? new DateTimeOffset(2026,10,13,14,0,30,TimeSpan.FromHours(2)) : new DateTimeOffset(2026, 10, 13, 13, 10, 0, TimeSpan.FromHours(2)) };
                WindowClickThrough.Attach(hub, () => o.Window == "events-locked");
                host = hub;
                refresh = hub.Reload;
                break;
            case "breakdown":
                var segment = meter.Tracker.Segments()[0].Id;
                var breakdown = new BreakdownWindow(new ViewModels.LiveFightView(meter.Tracker, segment), 2, meter.Data, meter.Icons, meter.Portraits)
                    { Width = o.Width ?? 1040, Height = o.Height ?? 720 };
                if (o.Tab is { } tab) breakdown.ShowTab(tab);
                host = breakdown;
                refresh = breakdown.ReloadNow;
                break;
            case "history":
                var history = new HistoryWindow(meter) { Width = 900, Height = 600 };
                host = history;
                refresh = history.Reload;
                break;
            case "personal-bests":
                var bests = new PersonalBestsWindow(meter, BossRecordChecks.Sample(meter.Data))
                    { Width = o.Width ?? 920, Height = o.Height ?? 660 };
                host = bests;
                refresh = bests.Reload;
                break;
            case "last-played-with":
                var players = new LastPlayedWithWindow(meter, BossRecordChecks.SamplePlayers())
                    { Width = o.Width ?? 760, Height = o.Height ?? 600 };
                host = players;
                refresh = players.Reload;
                break;
            case "death-recap":
                var died = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var recap = new DeathRecapWindow(new(died, [
                    new(died - 150, 100, "Ferocious Horn Nuakum", false, 1, "Heavy strike", 18420, HitFlags.Critical, 1),
                    new(died - 1800, 101, "Cave guardian", false, 2, "Claw attack", 4820, HitFlags.None, 1),
                    new(died - 2300, 100, "Ferocious Horn Nuakum", false, 3, "Flame burst", 9120, HitFlags.Dot, 3),
                    new(died - 3100, 101, "Cave guardian", false, 2, "Claw attack", 1850, HitFlags.None, 1),
                    new(died - 4300, 100, "Ferocious Horn Nuakum", false, 4, "Wild swing", 2410, HitFlags.None, 1),
                    new(died - 7000, 101, "Cave guardian", false, 2, "Claw attack", 950, HitFlags.None, 1)
                ], false));
                host = recap;
                refresh = () => { };
                break;
            case "timers":
            case "timers-locked":
                FeedTimers(meter);
                WaitForPortraits(meter);
                var timers = new BossTimersWindow(meter) { Width = o.Width ?? 780, Height = o.Height ?? 640 };
                WindowClickThrough.Attach(timers, () => o.Window == "timers-locked");
                host = timers;
                refresh = timers.ReloadNow;
                break;
            case "event-sounds":
            case "event-sounds-error":
                var soundCards = new TimerCards(meter, persistent: false);
                soundCards.Saved.Sounds["shugo"] = new EventSoundPreference { Enabled=true, Sound="Custom audio", FilePath=Path.Combine(AppContext.BaseDirectory,"sounds","Chime.wav") };
                var soundSample = new EventSoundsWindow(soundCards, (_, _) => { }) { Height = o.Height ?? 700 };
                if (o.Window == "event-sounds-error")
                {
                    var soundRoot=(DockPanel)((Border)soundSample.Content).Child;
                    var soundBody=(StackPanel)((ScrollViewer)soundRoot.Children[1]).Content;
                    var soundRow=(StackPanel)((Border)soundBody.Children[1]).Child;
                    var feedback=(TextBlock)soundRow.Children[2];
                    feedback.Text=string.Format(UiText.Current.Localize("This sound is {0:0.00} seconds long. Maximum: 10 seconds. Choose a shorter file."),10.58);
                    feedback.Visibility=Visibility.Visible;
                }
                host = soundSample;
                refresh = () => { };
                break;
            case "patchnotes":
                host = new PatchNotesWindow(meter.Updates);
                refresh = () => { };
                break;
            case "checklist":
            case "checklist-locked":
                ChecklistState.Normalize(meter.Settings);
                meter.Settings.ChecklistItems[0].Completed=true;
                var taskWindow=new ChecklistWindow(meter.Settings);
                if(o.Window=="checklist-locked")taskWindow.SetDisplayOnly(true);
                host=taskWindow;refresh=taskWindow.RefreshResetClock;break;
            case "hidden-cube":
                host=new HiddenCubeWindow(meter.Settings);
                refresh=()=>{};
                break;
            case "settings-only-boss":
                meter.Settings.TargetMode=Core.Combat.TargetMode.StrictBoss;
                goto case "settings";
            case "settings-party":
                meter.Settings.Theme=new(MarioParty:true);ThemeColors.Apply(meter.Settings.Theme);
                goto case "settings";
            case "settings-colors":
                meter.Settings.Theme=new("#7FA9F5","#151A22","#F3F5FA");ThemeColors.Apply(meter.Settings.Theme);
                goto case "settings";
            case "settings":
                var settingsWindow = new SettingsWindow(meter) { Width = o.Width ?? 760, Height = o.Height ?? 660 };
                if(o.Window=="settings-only-boss")settingsWindow.SelectTab("combat");
                // Keep existing numeric screenshot selectors stable when the sidebar is reordered.
                string[] tabIds=["appearance","combat","hotkeys","history","capture","general","updates","feedback","events","pets","profiles","help","cubes","checklist"];
                if (int.TryParse(o.Tab,out var settingsTab) && settingsTab>=0 && settingsTab<tabIds.Length) settingsWindow.SelectTab(tabIds[settingsTab]);
                else if(o.Tab is not null && tabIds.Contains(o.Tab)) settingsWindow.SelectTab(o.Tab);
                host = settingsWindow;
                refresh = () => { };
                break;
            case "update" or "update-portable":
                var release = SampleRelease(meter.Updates.Current);
                meter.Updates.Preview(release);
                host = new UpdateWindow(meter.Updates, release, installed: o.Window == "update") { Width = 580, Height = 560 };
                refresh = () => { };
                break;
            default:
                if (o.Window == "overlay-update") meter.Updates.Preview(SampleRelease(meter.Updates.Current)); // with the footer's update banner
                if (o.Window is "clickthrough" or "overlay-boss-estimate-locked") meter.Settings.ClickThrough = true;
                if (o.Window == "expanded") meter.Settings.CompactMode = false;
                var overlay = new OverlayWindow(meter) { Width = o.Width ?? 560, Height = o.Height ?? 820 };
                host = overlay;
                refresh = overlay.Refresh;
                break;
        }
        if(host is OverlayWindow display && !display.ControlsDetached)
            display.MoveControlsTo(new MeterControlsWindow(meter.Settings,()=>{},()=>{}));
        if(o.Window.StartsWith("overlay-boss-estimate",StringComparison.Ordinal) && host is OverlayWindow estimateDisplay)
        {
            var hp=meter.Tracker.Snapshot(null,start+20000)!.Boss!.Hp;
            for(var i=0;i<=10;i++)
            {
                var at=start+20000+i*1000;OverlayWindow.ClockOverride=()=>at;
                meter.Tracker.Process(new NpcHpEvent(at,900001,hp+(10-i)*1000000,90000000));
                estimateDisplay.Refresh();
            }
        }
        Capture(host, refresh, path, o);
        workspace?.Dispose();
    }

    /// <summary>
    /// The overlay through a scripted boss fight, <paramref name="fps"/> frames a second on the fight's own clock
    /// (frame_000.png …): from "waiting for combat" through the Elementalist taking the lead.
    /// </summary>
    public static void Animate(string folder, string language, Options o, double seconds, int fps)
    {
        WindowPresentation.SetScale(o.UiScale);StreamNames.Enabled=o.StreamMode;
        using var meter = CreateMeter(language, o.RowSize);
        meter.Settings.UiScale=o.UiScale;meter.Settings.StreamMode=o.StreamMode;
        if(o.TestMode)meter.StartDemo(animate:false);
        WaitForPortraits(meter);
        var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 600_000;
        var fight = new SampleFight(start, seconds);
        var clock = start;
        OverlayWindow.ClockOverride = () => clock;
        try
        {
            var overlay = new OverlayWindow(meter) { Width = o.Width ?? 500, Height = o.Height ?? 760 };
            Directory.CreateDirectory(folder);
            var frames = (int)Math.Round(seconds * fps);
            for (var f = 0; f <= frames; f++)
            {
                clock = start + (long)(f * 1000.0 / fps);
                fight.FeedUntil(meter.Tracker, clock);
                Capture(overlay, overlay.Refresh, Path.Combine(folder, $"frame_{f:000}.png"), o);
            }
        }
        finally
        {
            OverlayWindow.ClockOverride = null;
        }
    }

    private static MeterService CreateMeter(string language, int rowSize = 100)
    {
        var settings = new AppSettings
        {
            Transient = true, SaveHistory = false, MaxRows = 10, LayoutVersion = 99, Language = language, RowSize = rowSize,
        };
        UiText.Use(settings.Language);
        var meter = new MeterService(settings, _ => new QuietSource());
        meter.StartCapture(); // the footer reads "Capturing", as it does in the game
        meter.Portraits.Get(SampleBossCode); // first run only; then it comes from the cache
        return meter;
    }

    /// <summary>A capture that reports what a working one does and produces nothing: the script feeds the fight.</summary>
    private sealed class QuietSource : Core.Capture.IEventSource
    {
        public event Action<GameEvent>? EventDecoded { add { } remove { } }
        public event Action<Core.Capture.CaptureStatus>? StatusChanged { add { } remove { } }
        public Core.Capture.CaptureStatus Status { get; } = new(Core.Capture.CaptureState.Capturing, "Capturing · 127.0.0.1");
        public void Start() { }
        public void Stop() { }
        public void Dispose() { }
    }

    private static void WaitForPortraits(MeterService meter)
    {
        for (var i = 0; i < 80 && meter.Portraits.Busy; i++) Thread.Sleep(100);
    }

    private static void Capture(Window host, Action refresh, string path, Options o, FrameworkElement? rootOverride = null)
    {
        refresh();
        var root = rootOverride ?? host.Content as FrameworkElement;
        if (root is null) return;
        if(double.IsNaN(host.Height))
        {
            root.Measure(new Size(host.Width,double.PositiveInfinity));
            host.Height=Math.Clamp(Math.Ceiling(root.DesiredSize.Height),host.MinHeight,double.IsFinite(host.MaxHeight)?host.MaxHeight:1200);
        }
        root.Measure(new Size(host.Width, host.Height));
        root.Arrange(new Rect(0, 0, host.Width, host.Height));
        root.UpdateLayout();
        refresh();
        root.UpdateLayout();

        MarioPartyBackdrop.Attach(host);MarioPartyBackdrop.Refresh(host);
        var bmp = new RenderTargetBitmap((int)(host.Width * o.Scale), (int)(host.Height * o.Scale), 96 * o.Scale, 96 * o.Scale, PixelFormats.Pbgra32);
        if (Backdrop(host, o.Backdrop) is { } backdrop)
        {
            var bg = new DrawingVisual();
            using (var dc = bg.RenderOpen())
                dc.DrawRectangle(backdrop, null, new Rect(0, 0, host.Width, host.Height));
            bmp.Render(bg);
        }
        bmp.Render(root);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bmp));
        using var file = File.Create(path);
        encoder.Save(file);
    }

    /// <summary>What shows through the translucent overlay card; other windows draw their own background.</summary>
    private static Brush? Backdrop(Window host, string backdrop)
    {
        if (host is not OverlayWindow) return host.Background;
        return backdrop switch
        {
            "none" => null,
            "dark" => new LinearGradientBrush(
                new GradientStopCollection { new(Color.FromRgb(0x24, 0x31, 0x52), 0), new(Color.FromRgb(0x11, 0x17, 0x29), 0.55), new(Color.FromRgb(0x07, 0x09, 0x10), 1) },
                new Point(0, 0), new Point(1, 1)),
            _ => new SolidColorBrush(Color.FromRgb(0x2B, 0x3A, 0x2E)), // a stand-in for the game
        };
    }

    internal static Core.Combat.FightRecord SeedWorkspace(MeterService meter)
    {
        meter.StartDemo(animate:false);
        var start=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()-20000;
        new SampleFight(start,20).FeedUntil(meter.Tracker,start+20000);
        meter.Tracker.Process(new BossTimeLimitEvent(start,900001,start+300000));
        FeedTimers(meter);
        return meter.Tracker.Record(null)!;
    }
    private const int SampleBossCode = 2701250; // Balhash, a field boss

    /// <summary>
    /// A scripted boss fight: every class with its own skills, crits, back attacks and perfect hits. The Elementalist
    /// ("you") starts slow and takes the lead after a burst; the Brawler joins late. The same random seed every time,
    /// so screenshots and animation frames repeat exactly.
    /// </summary>
    private sealed class SampleFight
    {
        private const uint Boss = 900_001;
        private const long BossHp = 90_000_000;

        private sealed record Member(uint Id, string Name, GameClass Class, int[] Skills, Func<double, double> Dps, double JoinAt = 0);

        private static readonly Member[] Party =
        [
            new(1, "Borgrim", GameClass.Gladiator, [11010000, 11020000, 11030000, 11040000], t => t < 6 ? 500_000 : 420_000),
            new(2, "Sylvaen", GameClass.Elementalist, [16010000, 16020000, 16030000, 16040000, 16140000], t => t < 4 ? 280_000 : t < 9 ? 1_000_000 : 560_000),
            new(3, "Talwyn", GameClass.Ranger, [14010000, 14020000, 14030000, 14040000, 14050000], _ => 340_000),
            new(4, "Aegisa", GameClass.Templar, [12010000, 12020000, 12030000], _ => 280_000),
            new(5, "Nyxara", GameClass.Assassin, [13010000, 13020000, 13120000], t => t is > 7 and < 11 ? 640_000 : 230_000),
            new(6, "Morwen", GameClass.Sorcerer, [15010000, 15020000, 15030000], _ => 250_000),
            new(7, "Celesta", GameClass.Cleric, [17010000, 17020000, 17030000, 17040000], _ => 150_000),
            new(8, "Hymnar", GameClass.Chanter, [18010000, 18020000, 18030000, 18040000], _ => 130_000),
            new(9, "Grohm", GameClass.Brawler, [19010000, 19020000, 19030000], _ => 190_000, JoinAt: 2.5),
        ];

        private readonly List<GameEvent> _events = new();
        private int _next;

        public SampleFight(long start, double seconds)
        {
            var rnd = new Random(2305);
            _events.Add(new SelfIdentifiedEvent(start, 2, "Sylvaen", 2305, GameClass.Elementalist));
            foreach (var m in Party.Where(m => m.Id != 2)) _events.Add(new PlayerSeenEvent(start, m.Id, m.Name, 2305, m.Class));
            // Your party's gear score and combat power come with the roster; the others (not in your party) have none.
            _events.Add(new PartyRosterEvent(start, Party.Take(6)
                .Select(m => new PartyMemberInfo(m.Name, 2305, 45, 2700 + (int)m.Id * 61 % 400, 45_000 + (int)m.Id * 2_731 % 17_000, m.Class))
                .ToList()));
            _events.Add(new NpcSeenEvent(start, Boss, SampleBossCode, BossHp));

            var hits = new List<DamageEvent>();
            foreach (var m in Party)
            {
                for (var t = m.JoinAt + 0.2 + rnd.NextDouble() * 0.4; t < seconds; t += 0.45 + rnd.NextDouble() * 0.5)
                {
                    var amount = m.Dps(t) * 0.7 * (0.75 + rnd.NextDouble() * 0.5); // one hit every ~0.7 s on average
                    var flags = HitFlags.None;
                    if (rnd.NextDouble() < 0.24)
                    {
                        flags |= HitFlags.Critical;
                        amount *= 1.9;
                    }
                    if (rnd.NextDouble() < 0.18) flags |= HitFlags.Back;
                    else if (rnd.NextDouble() < 0.1) flags |= HitFlags.Perfect;
                    hits.Add(new DamageEvent(start + (long)(t * 1000), m.Id, Boss, m.Skills[rnd.Next(m.Skills.Length)], (long)amount, flags));
                }
            }
            hits.Sort((a, b) => a.TimeMs.CompareTo(b.TimeMs));

            // The boss's HP follows the damage dealt, updated every half second like the game does.
            long dealt = 0;
            var i = 0;
            for (var at = 500L; at <= seconds * 1000 + 500; at += 500)
            {
                while (i < hits.Count && hits[i].TimeMs <= start + at)
                {
                    dealt += hits[i].Damage;
                    _events.Add(hits[i++]);
                }
                _events.Add(new NpcHpEvent(start + at, Boss, Math.Max(1, BossHp - dealt), BossHp));
            }
        }

        /// <summary>Hit notices from <paramref name="players"/> made-up players whose damage the server never sends.</summary>
        public void FeedCrowd(Core.Combat.CombatTracker tracker, long atMs, int players)
        {
            for (var i = 0; i < players; i++) tracker.Process(new HitNoticeEvent(atMs, 910_000 + (uint)i, Boss, 14030120));
        }

        /// <summary>Feeds every event up to <paramref name="untilMs"/> that has not been fed yet.</summary>
        public void FeedUntil(Core.Combat.CombatTracker tracker, long untilMs)
        {
            while (_next < _events.Count && _events[_next].TimeMs <= untilMs) tracker.Process(_events[_next++]);
        }
    }

    /// <summary>A made-up next version (one minor up) for the update window and the overlay's update banner.</summary>
    private static Core.Updates.ReleaseInfo SampleRelease(Version current) => ReleaseOf(new Version(current.Major, current.Minor + 1, 0));

    private static Core.Updates.ReleaseInfo ReleaseOf(Version next) => new(
        next, "v" + next.ToString(3), Core.Updates.UpdateFeed.ReleasesPage,
        """
        ## Что нового

        - **Проверка обновлений**: метр сам сообщает о новой версии
          и обновляется в один клик.
        - Таймеры боссов отдельно по серверам.

        ## What's new (English)

        - **Update check**: the meter tells you about a new version and updates itself in one click.
        """,
        DateTimeOffset.UtcNow.AddDays(-1),
        new Core.Updates.ReleaseAsset($"CatScratch-Setup-v{next.ToString(3)}.exe", 52_000_000, "https://example.invalid/setup.exe", null),
        new Core.Updates.ReleaseAsset($"CatScratch-v{next.ToString(3)}-win-x64.zip", 65_000_000, "https://example.invalid/app.zip", null));

    // Altgard's field boss list from a real capture (00:30:07 on 2026-10-06, UTC+3) — the in-game map's timers.
    private const string AltgardList =
        "00005604000018009de30602729d660ea10100000199e30691939bc7645092c700ecdd467f04f30da1010000009fe3066d013f0ea1010000" +
        "00a0e3066efe360ea1010000009ae306c4e9010ea1010000009be306a54a140ea1010000009ee306bc74360ea1010000009ce30630852c0e" +
        "a101000001a1e30600a57447800a0dc800f0db45095a05d40da101000000a2e3061f365b0ea101000000a3e306d4eb670ea101000001a4e3" +
        "06413f42482dc5e5c700b6974683a4f40da101000000ace30650d2de0ea101000000a5e3068054070ea101000000a6e306dae65e0ea10100" +
        "0000a7e3061737050ea101000000a8e306001166e80ea101000000a9e306ef5f4d0ea101000000aae30668a8020ea101000000abe306c284" +
        "d30ea101000000ade306fbccc10ea101000000aee3061cfdc60ea101000000afe306bf00d70ea101000000b0e3063501d10ea10100000000" +
        "00";

    internal static void FeedTimers(MeterService meter)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var capturedAt = new DateTimeOffset(2026, 10, 5, 21, 30, 7, 231, TimeSpan.Zero).ToUnixTimeMilliseconds();
        var events = new List<GameEvent>();
        new Core.Protocol.PacketParser(meter.Data, events.Add) { TimeMs = capturedAt }
            .Handle(Core.Protocol.Opcodes.FieldBossList, Core.Protocol.Wire.FromHex(AltgardList));
        if (events.FirstOrDefault() is FieldBossListEvent list)
        {
            // Shifted so the capture reads as "just now".
            var shift = now - capturedAt;
            meter.Timers.OnList(list with { TimeMs = now, Slots = list.Slots.Select(s => s with { AtMs = s.AtMs + shift }).ToList(), ServerId = 2305 });
        }
        // One boss of another map, from a kill the meter saw.
        meter.Timers.OnNotice(new Core.Combat.BossNotice(2701250, 220014, null, Core.Combat.BossNoticeKind.Killed, now - 70 * 60_000, ServerId: 2305));
        meter.Timers.SetRespawn(2305, 2701250, 120);
        meter.Timers.SetWatch(2305, 2400800, true);
        foreach (var t in meter.Timers.All().Where(t => t.NpcCode > 0)) meter.Portraits.Get(t.NpcCode);
    }
}
