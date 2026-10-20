using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;

namespace AionMeter.App.Services;

/// <summary>Opt-in verification against our own offscreen windows. No capture, game access or simulated user input.</summary>
internal static class InteractionChecks
{
    public static void Run(string output)
    {
        var passed = new List<string>();
        var windows = new List<Window>();
        bool mode = false;
        Window Create()
        {
            var window = new Window { Title = "Interaction verification", Width = 330, Height = 150, Left = -30000, Top = -30000, ShowInTaskbar = false, ShowActivated = false };
            if (windows.Count % 2 != 0)
            {
                window.WindowStyle = WindowStyle.None;
                window.Content = new System.Windows.Controls.Border { CornerRadius = new CornerRadius(10), Background = System.Windows.Media.Brushes.DarkSlateGray };
            }
            windows.Add(window);
            WindowClickThrough.Attach(window, () => mode);
            window.Show();
            return window;
        }
        long Style(Window window) => (long)NativeMethods.GetWindowLongPtr(new WindowInteropHelper(window).Handle, NativeMethods.GWL_EXSTYLE);
        const long mask = NativeMethods.WS_EX_LAYERED | NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TRANSPARENT;
        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            passed.Add(message);
        }
        try
        {
            MeterRestartChecks.Run(Check);
            static bool HasPurplePaw(System.Drawing.Bitmap bitmap)
            {
                for(var y=0;y<bitmap.Height;y++)for(var x=0;x<bitmap.Width;x++)
                {var pixel=bitmap.GetPixel(x,y);if(pixel.A>120 && pixel.B>pixel.R && pixel.R>pixel.G)return true;}
                return false;
            }
            var gameGate=new GameExitGate();
            var gameTime=DateTimeOffset.UtcNow;
            Check(!new AppSettings().FollowGame && !gameGate.ShouldClose(true,false,gameTime),
                "Game companion is opt-in and an absent game does not close a manually started meter");
            Check(!gameGate.ShouldClose(true,true,gameTime) && !gameGate.ShouldClose(true,false,gameTime.AddSeconds(2)) &&
                !gameGate.ShouldClose(true,false,gameTime.AddSeconds(11)) && gameGate.ShouldClose(true,false,gameTime.AddSeconds(12)),
                "Confirmed game sessions close only after ten seconds of absence");
            Check(!gameGate.ShouldClose(true,true,gameTime.AddSeconds(13)) && !gameGate.ShouldClose(true,false,gameTime.AddSeconds(14)) &&
                !gameGate.ShouldClose(false,false,gameTime.AddSeconds(30)) && !gameGate.ShouldClose(true,false,gameTime.AddSeconds(40)),
                "Game restarts reset the grace period and disabling clears the close gate");
            Check(GameLifetime.IsRunning(Environment.ProcessPath!) && !GameLifetime.IsRunning(""),
                "Game detection matches the exact executable in this Windows session without launching it");
            var taskXml=System.Xml.Linq.XDocument.Parse(GameLifetime.Definition("S-1-5-21-test","C:\\CatScratch\\CatScratch.GameWatcher.exe","C:\\A & B\\watch.ps1"));
            System.Xml.Linq.XNamespace taskNs="http://schemas.microsoft.com/windows/2004/02/mit/task";
            Check(taskXml.Descendants(taskNs+"LogonType").Single().Value=="InteractiveToken" &&
                taskXml.Descendants(taskNs+"RunLevel").Single().Value=="HighestAvailable" &&
                taskXml.Descendants(taskNs+"Arguments").Single().Value=="\"C:\\A & B\\watch.ps1\"" &&
                taskXml.Descendants(taskNs+"Command").Single().Value.EndsWith("CatScratch.GameWatcher.exe") &&
                GameLifetime.TaskName("C:\\Sample")==GameLifetime.TaskName("c:\\sample\\"),
                "Game watcher task uses the interactive user, safe XML paths and a stable per-installation name");
            var schedulerType=Type.GetTypeFromProgID("Schedule.Service")!;
            dynamic scheduler=Activator.CreateInstance(schedulerType)!;
            object? definition=null;
            try
            {
                scheduler.Connect();
                definition=scheduler.NewTask(0);
                using var user=System.Security.Principal.WindowsIdentity.GetCurrent();
                ((dynamic)definition).XmlText=GameLifetime.Definition(user.User!.Value,Environment.ProcessPath!,"C:\\Sample\\watch.ps1");
                Check(((string)((dynamic)definition).XmlText).Contains("HighestAvailable"),
                    "Windows Task Scheduler accepts the companion definition without registering or starting a task");
            }
            finally
            {
                if(definition is not null) System.Runtime.InteropServices.Marshal.FinalReleaseComObject(definition);
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(scheduler);
            }
            using (var appIcon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!))
            using (var bitmap = appIcon!.ToBitmap())
            {
                Check(HasPurplePaw(bitmap), "Published executable contains the outlined purple paw icon");
            }
                        foreach (var gameClass in Enum.GetValues<AionMeter.Core.Events.GameClass>())
            {
                var row = new AionMeter.App.ViewModels.RowViewModel { Class = gameClass };
                var background = (System.Windows.Media.SolidColorBrush)row.CompactContrastBackground;
                static double Linear(byte c) { var s = c / 255.0; return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4); }
                var color = background.Color;
                var luminance = 0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);
                var contrast = row.CompactTextBrush == System.Windows.Media.Brushes.Black ? (luminance + 0.05) / 0.05 : 1.05 / (luminance + 0.05);
                Check(background.Color.A == 255 && background.IsFrozen && contrast >= 4.5,
                    "Compact text contrast accounts for darkened class fill: " + gameClass);
            }
            var clipping=new AionMeter.App.Controls.CompactFillClip();
            var zero=(System.Windows.Media.RectangleGeometry)clipping.Convert([120d,24d,260d,0d],typeof(System.Windows.Media.Geometry),null!,System.Globalization.CultureInfo.InvariantCulture);
            var partial=(System.Windows.Media.RectangleGeometry)clipping.Convert([120d,24d,260d,0.25d],typeof(System.Windows.Media.Geometry),null!,System.Globalization.CultureInfo.InvariantCulture);
            var full=(System.Windows.Media.RectangleGeometry)clipping.Convert([120d,24d,260d,1d],typeof(System.Windows.Media.Geometry),null!,System.Globalization.CultureInfo.InvariantCulture);
            Check(zero.Rect.Width==0 && partial.Rect.Width==36 && full.Rect.Width==120 && partial.IsFrozen,
                "Contrasting text is clipped to zero, partial and full damage bars without painting fixed color blocks");
            var partyView = new AionMeter.App.ViewModels.OverlayViewModel();
            partyView.ApplyParty([
                new("Sample self", 2305, 45, 1894, 85471, AionMeter.Core.Events.GameClass.Cleric),
                new("Sample teammate", 2305, 45, 1132, 52644, AionMeter.Core.Events.GameClass.Elementalist)
            ], "Sample self");
            Check(partyView.Rows.Count == 2 && partyView.Rows[0].IsSelf && partyView.Rows[1].GearScore == "1132",
                "Party preview displays names, classes and received GS before damage");
            Check(partyView.SegmentId is null && partyView.Rows.All(r => r.Dps == "—") && partyView.Rows[1].HasCombatPower,
                "Party preview carries CP without inventing damage or an encounter");
            partyView.ApplyParty([new("Sample self", 2305, 45, 1894, 85471, AionMeter.Core.Events.GameClass.Cleric)], "Sample self", true);
            Check(partyView.Title == "Room stats" && partyView.Rows.Count == 1, "Room view shows received stats even while waiting alone");
            partyView.Apply(null, 10, false);
            Check(!partyView.HasData && partyView.Rows.Count == 0, "Leaving room preview restores the normal waiting view");
            var first = Create(); var second = Create();
            foreach (var tool in new[] { first, second })
            {
                var handle = new WindowInteropHelper(tool).Handle;
                Check(!tool.ShowInTaskbar && (Style(tool) & NativeMethods.WS_EX_TOOLWINDOW) != 0 && (Style(tool) & 0x00040000) == 0,
                    "Display modules are tool windows without separate taskbar or Alt-Tab entries");
                Check(((long)NativeMethods.GetWindowLongPtr(handle, -16) & (0x00040000L | 0x00010000L)) == 0,
                    "Display modules do not advertise native resize or maximize styles to Windows Snap");
            }
                        foreach (var window in new[] { first, second })
            {
                Check(ReferenceEquals(window.Icon, AppIcon.Image), "Window uses the packaged purple paw directly");
                foreach (var size in new[] { 0, 1 })
                {
                    var handle = NativeMethods.SendMessage(new WindowInteropHelper(window).Handle, 0x007F, size, 0);
                    Check(handle != 0, "Native window provides its own small and large taskbar icons");
                    using var nativeIcon = System.Drawing.Icon.FromHandle(handle);
                    using var bitmap = nativeIcon.ToBitmap();
                    Check(HasPurplePaw(bitmap), "Native taskbar icon contains the outlined purple paw");
                }
            }
            using (var trayIcon = AppIcon.CreateTrayIcon())
            using (var bitmap = trayIcon.ToBitmap())
            {
                Check(HasPurplePaw(bitmap), "Tray icon uses the packaged outlined purple paw directly");
            }
            var originals = windows.Select(Style).ToArray();
            var frame = (System.Windows.Controls.Border)first.Content;
            var originalBackground = frame.Background;
            Check((Style(first) & NativeMethods.WS_EX_TRANSPARENT) == 0, "Starts clickable");
            using var keys = new HotkeyManager(new WindowInteropHelper(first).Handle);
            Action toggle = () => { mode = !mode; WindowClickThrough.ApplyAll(mode); };
            var registered = keys.Register("Ctrl+Shift+L", toggle);
            if (!registered)
            {
                passed.Add($"Default hotkey unavailable on test machine (Windows error {keys.LastRegistrationError})");
                registered = keys.Register("Ctrl+Alt+Shift+F24", toggle);
                if (!registered) passed.Add($"Global hotkey registration not verified: Windows error {keys.LastRegistrationError}. Testing the window toggle directly.");
            }
            else passed.Add("Default global hotkey registered");
            // Deliver only our own WM_HOTKEY notification to our own window; never generate keyboard or mouse input.
            void Trigger()
            {
                if (registered) NativeMethods.SendMessage(new WindowInteropHelper(first).Handle, NativeMethods.WM_HOTKEY, keys.LastRegisteredId, 0);
                else toggle();
            }
            Trigger();
            Check(mode && windows.All(w => (Style(w) & mask) == mask), "Toggle enables passthrough for all displays");
            Check(frame.Background == System.Windows.Media.Brushes.Transparent && frame.BorderBrush == System.Windows.Media.Brushes.Transparent, "Display-only mode removes window background and border");
            var third = Create();
            Check((Style(third) & mask) == mask, "New display inherits passthrough immediately");
            Trigger();
            Check(!mode && windows.All(w => (Style(w) & NativeMethods.WS_EX_TRANSPARENT) == 0), "Second toggle restores clickability");
            Check(frame.Background == originalBackground, "Clickable mode restores original window background");
            Check(Style(first) == originals[0] && Style(second) == originals[1], "Original native window styles restored");
            for (var i = 0; i < 4; i++) { mode = !mode; WindowClickThrough.ApplyAll(mode); }
            Check(Style(first) == originals[0], "Repeated toggles preserve original styles");
            Check(!HotkeyManager.TryParse("+++", out _, out _) && !HotkeyManager.TryParse("Ctrl++A",out _,out _) && !HotkeyManager.TryParse("99999",out _,out _), "Invalid hotkey text rejected");
            Check(HotkeyManager.InvalidGestureIndex(new[]{"Ctrl+Shift+A","shift+control+a"})==1 && HotkeyManager.InvalidGestureIndex(new[]{"Shift+F3","Shift+F4"})==-1,"Shortcut conflicts compare actual keys even with reordered or alternate modifier names");
            var grouped=new WindowVisibilityGroup();
            var placement=(first.Left,first.Top,first.Width,first.Height);
            var hidden=Create();hidden.Hide();
            var doomed=Create();
            grouped.Toggle(windows,first);
            Check(grouped.Hidden && windows.All(w=>!w.IsVisible),"All-window toggle hides the visible displays together");
            var later=Create();grouped.Track(later);
            Check(!later.IsVisible,"Newly enabled modules stay hidden while the all-window toggle is active");
            doomed.Close();windows.Remove(doomed);
            grouped.Restore();
            Check(!grouped.Hidden && first.IsVisible && second.IsVisible && third.IsVisible && later.IsVisible && !hidden.IsVisible,"All-window toggle restores only previously visible windows and skips closed windows");
            Check(placement==(first.Left,first.Top,first.Width,first.Height),"All-window visibility preserves window placement and size");
            Check(HotkeyManager.TryParse(new AppSettings().HotkeyToggleAll,out _,out _),"Default all-window shortcut is a valid configurable key combination");
            var disabledGroup=new WindowVisibilityGroup();
            hidden.Hide();
            disabledGroup.Toggle([hidden],hidden,allowFallback:false);
            Check(!hidden.IsVisible && !disabledGroup.Hidden,"Show/hide all never opens a disabled DPS meter as an empty-group fallback");
            disabledGroup.Toggle([first,second],first);
            disabledGroup.Forget(first);
            disabledGroup.Restore();
            Check(!first.IsVisible && second.IsVisible,"Disabling a meter while all modules are hidden removes only that meter from restoration");
            first.Show();
            var defaults=new AppSettings();
            using (var bossMeter = new MeterService(new AppSettings { Transient=true, DownloadIcons=false, CompactMode=true, ShowBossPanel=true }, null))
            {
                bossMeter.Tracker.Process(new Core.Events.SelfIdentifiedEvent(1,1,"HP sample",2305,Core.Events.GameClass.Cleric));
                bossMeter.Tracker.Process(new Core.Events.NpcSeenEvent(2,500,2701250,90000000));
                bossMeter.Tracker.Process(new Core.Events.DamageEvent(3,1,500,17010000,1000000,Core.Events.HitFlags.None));
                bossMeter.Tracker.Process(new Core.Events.NpcHpEvent(4,500,89000000,90000000));
                var bossOverlay = new Windows.OverlayWindow(bossMeter) { Left=-30000, Top=-30000, WindowStartupLocation=WindowStartupLocation.Manual, ShowInTaskbar=false, ShowActivated=false }; windows.Add(bossOverlay); bossOverlay.Show();
                var bossModel = (ViewModels.OverlayViewModel)bossOverlay.DataContext;
                var hpPanel = (System.Windows.Controls.Grid)bossOverlay.FindName("BossHpPanel");
                foreach (var compact in new[] { true, false })
                foreach (var clickThrough in new[] { false, true })
                {
                    bossMeter.Settings.CompactMode=compact;bossMeter.Settings.ClickThrough=clickThrough;bossMeter.Settings.ShowBossPanel=true;bossOverlay.Refresh();hpPanel.GetBindingExpression(UIElement.VisibilityProperty)?.UpdateTarget();
                    Check(bossModel.HasBoss && hpPanel.Visibility==Visibility.Visible && Math.Abs(bossModel.BossHpFraction-89d/90)<0.001,"Boss HP remains visible when enabled: compact="+compact+", click-through="+clickThrough);
                    bossMeter.Settings.ShowBossPanel=false;bossOverlay.Refresh();hpPanel.GetBindingExpression(UIElement.VisibilityProperty)?.UpdateTarget();
                    Check(!bossModel.HasBoss && hpPanel.Visibility==Visibility.Collapsed,"Disabling boss HP hides only the HP panel");
                }
                try
                {
                    Windows.OverlayWindow.ClockOverride=()=>20_000;
                    bossMeter.Settings.ShowBossPanel=true;bossMeter.Settings.ClickThrough=false;
                    bossMeter.Tracker.Process(new Core.Events.InstanceTimerEvent(10_000,600092,2,10_000,600_000));
                    bossOverlay.Refresh();
                    Check(!bossModel.HasBossLimit,"Dungeon time limits are never substituted for boss deadlines");
                    bossMeter.Tracker.Process(new Core.Events.BossTimeLimitEvent(20_000,501,320_000));bossOverlay.Refresh();
                    Check(!bossModel.HasBossLimit,"A nearby different boss cannot populate the current boss deadline bar");
                    bossMeter.Tracker.Process(new Core.Events.BossTimeLimitEvent(20_000,500,320_000));
                    Windows.OverlayWindow.ClockOverride=()=>170_000;bossOverlay.Refresh();
                    Check(bossModel.HasBossLimit && Math.Abs(bossModel.BossLimitFraction-.5)<.001,"Small boss deadline bar uses the matching server timer and counts down its real remaining fraction");
                    foreach(var width in new[]{260d,420d})
                    {
                        bossOverlay.Width=width;bossOverlay.UpdateLayout();
                        var fill=(System.Windows.Controls.Border)bossOverlay.FindName("BossHpFill");
                        var track=(System.Windows.Controls.Grid)bossOverlay.FindName("BossHpTrack");
                        Check(fill.ActualWidth<=track.ActualWidth+.5 && fill.RenderTransform==System.Windows.Media.Transform.Identity,"Boss HP fill remains bounded and keeps unscaled rounded corners at width "+width);
                    }
                    bossMeter.Tracker.Process(new Core.Events.BattleStateEvent(180_000,500,false));bossOverlay.Refresh();
                    Check(!bossModel.HasBossLimit,"Boss deadline bar clears when the boss resets");
                }
                finally{Windows.OverlayWindow.ClockOverride=null;}
            }
            var moduleKeys=new[]{defaults.HotkeyToggleOverlay,defaults.HotkeyPersonal,defaults.HotkeyEvents,defaults.HotkeyTimers,defaults.HotkeyPets,defaults.HotkeyToggleAll,defaults.HotkeyClickThrough,defaults.HotkeyReset,defaults.HotkeyDeathRecap,defaults.HotkeyChecklist};
            Check(moduleKeys.Distinct().Count()==10 && moduleKeys.All(k=>k.StartsWith("Shift+F") && HotkeyManager.TryParse(k,out _,out _)),"Every default shortcut uses Shift and a distinct function key away from WASD");
            var oldDefaults=new AppSettings {HotkeyToggleOverlay="Ctrl+Shift+D",HotkeyTimers="Ctrl+Shift+T",HotkeyPets="Alt+F9"};oldDefaults.MigrateHotkeys();
            Check(oldDefaults.HotkeyToggleOverlay=="Shift+F5" && oldDefaults.HotkeyTimers=="Shift+F8" && oldDefaults.HotkeyPets=="Alt+F9","One-time hotkey migration updates old defaults and preserves custom combinations");
            oldDefaults.HotkeyToggleOverlay="Ctrl+Shift+D";oldDefaults.MigrateHotkeys();
            Check(oldDefaults.HotkeyToggleOverlay=="Ctrl+Shift+D","Hotkey migration does not override later user choices");
            var elevation = AdministratorStartup.StartInfo("C:\\Cat Scratch\\CatScratch.exe", ["--updated", "argument with spaces"]);
            Check(elevation.UseShellExecute && elevation.Verb == "runas" && elevation.ArgumentList.SequenceEqual(new[] { "--updated", "argument with spaces" }),
                "Normal startup requests administrator elevation and preserves arguments");
            using var meter = new MeterService(new AppSettings { Transient = true, DownloadIcons = false, UpdateChannel = "Stable" }, null);
            var exportAnchor = new System.Windows.Controls.Button();
            var disabledExport = ChatCopy.Items(exportAnchor, () => null, null, null).ToArray();
            Check(disabledExport.All(item => !item.IsEnabled), "Chat export is disabled when no real fight is selected");
            meter.Tracker.Process(new AionMeter.Core.Events.SelfIdentifiedEvent(1, 1, "Sample", 2305, AionMeter.Core.Events.GameClass.Cleric));
            meter.Tracker.Process(new AionMeter.Core.Events.DamageEvent(2, 1, 500, 17010000, 1600000, AionMeter.Core.Events.HitFlags.None));
            var export = ChatCopy.Items(exportAnchor, () => meter.Tracker.Snapshot(null, 3), null, null).ToArray();
            Check(export[0].IsEnabled && export[0].Header.ToString()!.Contains("numbers + %"), "Full integer damage export is available for a selected fight");
            export[1].RaiseEvent(new RoutedEventArgs(System.Windows.Controls.MenuItem.SubmenuOpenedEvent, export[1]));
            Check(export[1].Items.Count == 1 && ((System.Windows.Controls.MenuItem)export[1].Items[0]).ToolTip.ToString()!.Contains("1,600,000 dmg (100.0%)"),
                "Chat submenu previews exact damage and share without posting or changing the clipboard");
                        var segment = meter.Tracker.LiveFight()!.Value.Id;
            var breakdown = new AionMeter.App.Windows.BreakdownWindow(new AionMeter.App.ViewModels.LiveFightView(meter.Tracker, segment), 1, meter.Data, meter.Icons, meter.Portraits)
                { Left = -30000, Top = -30000, WindowStartupLocation = WindowStartupLocation.Manual, ShowInTaskbar = false, ShowActivated = false };
            windows.Add(breakdown);
            breakdown.Show();
            var historyWindow = new AionMeter.App.Windows.HistoryWindow(meter)
                { Left = -30000, Top = -30000, WindowStartupLocation = WindowStartupLocation.Manual, ShowInTaskbar = false, ShowActivated = false };
            windows.Add(historyWindow);
            historyWindow.Show();
            foreach (var locked in new[] { true, false, true })
            {
                WindowClickThrough.ApplyAll(locked);
                WindowClickThrough.SetOpacity(0.4);
                Check((Style(historyWindow) & (NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_NOACTIVATE)) == 0 && historyWindow.IsHitTestVisible,
                    "Your fights remains clickable when the meter is locked or unlocked");
                Check(historyWindow.Opacity == 1 && historyWindow.Content is System.Windows.Controls.Border historyShell &&
                    historyShell.Background is System.Windows.Media.SolidColorBrush historyBg && historyBg.Color.A == 255,
                    "Your fights retains a solid background independently of meter opacity");
                Check((Style(breakdown) & (NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_NOACTIVATE)) == 0,
                    "Detailed statistics remain clickable when display windows are locked or unlocked");
                Check(breakdown.Opacity == 1 && breakdown.Content is System.Windows.Controls.Border detailShell &&
                    detailShell.Background is System.Windows.Media.SolidColorBrush detailBg && detailBg.Color.A == 255,
                    "Detailed statistics retain their opaque background independently of display opacity");
                var tab = (System.Windows.Controls.RadioButton)breakdown.FindName(locked ? "TabAccuracy" : "TabDps");
                tab.IsChecked = true;
                Check(tab.IsChecked == true && tab.IsEnabled && tab.IsHitTestVisible,
                    "Detailed-statistics tabs remain interactive with the display locked");
            }
            foreach (var name in new[] { "dps", "accuracy", "rotation", "defense" })
            {
                breakdown.ShowTab(name);
                foreach (var page in new[] { "Dps", "Accuracy", "Rotation", "Defense" })
                    Check(((FrameworkElement)breakdown.FindName("Page" + page)).Visibility ==
                        (page.Equals(name,StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed),
                        "Redesigned fight details navigate to " + name + " without overlapping pages: " + page);
            }
            breakdown.ShowTab("dps");
                        meter.PartyPreview.Observe(new AionMeter.Core.Events.SelfIdentifiedEvent(1,1,"Sample",2305,AionMeter.Core.Events.GameClass.Cleric));
            meter.PartyPreview.Observe(new AionMeter.Core.Events.PartyRosterEvent(2,
                [new("Sample",2305,45,1894,85471,AionMeter.Core.Events.GameClass.Cleric)],42,600001,"Sample room"));
            var activeOverlay = new AionMeter.App.Windows.OverlayWindow(meter); windows.Add(activeOverlay);
            activeOverlay.Refresh();
            var activeModel=(AionMeter.App.ViewModels.OverlayViewModel)activeOverlay.DataContext;
            Check(activeModel.Rows.Count > 0 && activeModel.Rows[0].Dps != "—" && activeModel.SegmentId is not null,
                "Active fight damage takes priority over retained dungeon-room stats");
            var cards = new TimerCards(meter, persistent: false);
            cards.Saved.SelectedEvents = ["shugo", "rift"];
            var hub = new AionMeter.App.Windows.TimerHubWindow(cards)
            {
                ClockOverride = () => new DateTimeOffset(2026, 10, 13, 13, 42, 0, TimeSpan.FromHours(2))
            };
            hub.Reload();
            var scheduleTime = hub.ClockOverride();
            Check(ReferenceEquals(cards.NextEvents(scheduleTime), cards.NextEvents(scheduleTime.AddMilliseconds(100))),
                "Event windows share the schedule calculation within a refresh interval");
            cards.Saved.SelectedEvents = ["shugo"];
            Check(cards.NextEvents(scheduleTime).Count == 1, "Event selection invalidates the shared schedule immediately");
            cards.Saved.SelectedEvents = ["shugo", "rift"];
            hub.Reload();
            windows.Add(hub);
            WindowClickThrough.Attach(hub, () => mode);
            var shell = (System.Windows.Controls.Border)hub.Content;
            var root = (System.Windows.Controls.DockPanel)shell.Child;
            var header = (System.Windows.Controls.DockPanel)root.Children[0];
            var list = (System.Windows.Controls.WrapPanel)root.Children[1];
            hub.SetDisplayOnly(true);
            Check(header.Visibility == Visibility.Hidden, "Locked events hide the shared header");
            Check(list.Children.OfType<System.Windows.Controls.Border>().Count() == 2 &&
                list.Children.OfType<System.Windows.Controls.Border>().All(b =>
                    b.Background is System.Windows.Media.SolidColorBrush bg && bg.Color.A > 0 &&
                    b.BorderBrush is System.Windows.Media.SolidColorBrush border && border.Color.A > 0),
                "Locked simultaneous events retain separate visible cards");
            var colors = list.Children.OfType<System.Windows.Controls.Border>().Select(tile =>
                ((System.Windows.Controls.Border)((System.Windows.Controls.Grid)tile.Child).Children[0]).BorderBrush).Distinct().Count();
            Check(colors == 2, "Simultaneous EVENT and PVP cards use different type colors");
            hub.Reload();
            Check(list.Children.OfType<System.Windows.Controls.Border>().All(b =>
                b.Background is System.Windows.Media.SolidColorBrush bg && bg.Color.A > 0),
                "Countdown refresh preserves locked event backgrounds");
            hub.SetDisplayOnly(false);
            Check(header.Visibility == Visibility.Visible, "Unlock restores the event header");
            hub.ClockOverride=()=>new DateTimeOffset(2026,10,13,14,0,30,TimeSpan.FromHours(2)); hub.Reload();
            Check(!list.Children.OfType<System.Windows.Controls.Border>().Any(),
                "Started events leave Upcoming immediately without duplicate names from later occurrences");
            cards.Saved.Positions["started-events"]=new(){Left=310,Top=220,Width=320};
            var startedHub = new AionMeter.App.Windows.TimerHubWindow(cards,startedOnly:true) { ClockOverride=hub.ClockOverride };
            windows.Add(startedHub); startedHub.Reload();
            Check(startedHub.Left==310 && startedHub.Top==220 && startedHub.Width==320 && hub.Left!=startedHub.Left,
                "Started events restore an independent movable module placement");
            var startedRoot=(System.Windows.Controls.DockPanel)((System.Windows.Controls.Border)startedHub.Content).Child;
            var startedList=(System.Windows.Controls.WrapPanel)startedRoot.Children[1];
            string[] ActiveLabels()=>startedList.Children.OfType<System.Windows.Controls.Border>()
                .Select(b=>((System.Windows.Controls.TextBlock)((System.Windows.Controls.Grid)b.Child).Children[2]).Text).ToArray();
            Check(ActiveLabels().Order().SequenceEqual(new[]{"Open! · 02:30","Open! · 09:30"}),
                "Started-only module shows event and Rift countdowns with their own durations");
            Check(startedList.Children.OfType<System.Windows.Controls.Border>().All(b=>b.BorderBrush.HasAnimatedProperties),
                "Started-only cards keep the existing pulsing style");
            hub.SetDisplayOnly(true);
            startedHub.SetDisplayOnly(true);
            Check(((FrameworkElement)startedRoot.Children[0]).Visibility==Visibility.Hidden && ActiveLabels().Length==2,
                "Started events support the meter's click-through appearance");
            hub.ClockOverride=()=>new DateTimeOffset(2026,10,13,14,3,0,TimeSpan.FromHours(2)); hub.Reload();
            startedHub.ClockOverride=hub.ClockOverride; startedHub.Reload();
            Check(ActiveLabels().SequenceEqual(new[]{"Open! · 07:00"}),"Normal events expire at three minutes while Rift stays open");
            Check(list.Children.OfType<System.Windows.Controls.Border>().Count()==1,
                "The next recurring event returns to Upcoming after its active occurrence expires");
            startedHub.ClockOverride=()=>new DateTimeOffset(2026,10,13,14,10,0,TimeSpan.FromHours(2)); startedHub.Reload();
            Check(ActiveLabels().Length==0,"Rift disappears at exactly ten minutes");
            cards.Saved.SelectedEvents=["shugo","kaira"];
            startedHub.ClockOverride=()=>new DateTimeOffset(2026,10,13,13,0,30,TimeSpan.FromHours(2)); startedHub.Reload();
            Check(ActiveLabels().Contains("Spawned") && ActiveLabels().Contains("Open! · 02:30"),
                "Started bosses show Spawned without a countdown alongside normal event countdowns");
            startedHub.ClockOverride=()=>new DateTimeOffset(2026,10,13,13,2,0,TimeSpan.FromHours(2)); startedHub.Reload();
            Check(ActiveLabels().SequenceEqual(new[]{"Open! · 01:00"}),"Boss cards disappear at exactly two minutes");
            cards.Saved.SelectedEvents=["shugo","rift","invasion"];
            hub.ClockOverride=()=>new DateTimeOffset(2026,10,13,13,10,0,TimeSpan.FromHours(2)); hub.Reload();
            Check(list.Children.OfType<System.Windows.Controls.Border>().Count()==3 && cards.NextEvents(hub.ClockOverride()).Select(e=>e.StartsAt).Distinct().Count()==2,
                "Upcoming events display all selected start times within the hour, not just the earliest");
            cards.Saved.SelectedEvents=["shugo","rift"];
            var eventPreferences=new AionMeter.App.Windows.EventPreferencesWindow(cards,()=>480,_=>{},_=>{},()=>{}); windows.Add(eventPreferences);
            Check(eventPreferences.Background is System.Windows.Media.SolidColorBrush eventBg && eventBg.Color.A==255 && eventPreferences.Opacity==1,
                "Event settings use a solid interactive meter-style surface");
            var settings=new AionMeter.App.Windows.SettingsWindow(meter); windows.Add(settings);
            foreach(var (field,gesture) in new[]{("HkPersonal",meter.Settings.HotkeyPersonal),("HkEvents",meter.Settings.HotkeyEvents),("HkPets",meter.Settings.HotkeyPets),("HkDeathRecap",meter.Settings.HotkeyDeathRecap),("HkChecklist",meter.Settings.HotkeyChecklist)})
            {
                var box=(System.Windows.Controls.TextBox)settings.FindName(field);
                Check(box.IsReadOnly && box.Text==gesture && HotkeyManager.TryParse(gesture,out _,out _),"Module shortcut can be recorded by pressing keys: "+field);
            }
            var categories=((System.Windows.Controls.TabControl)settings.FindName("SettingsTabs")).Items.OfType<System.Windows.Controls.TabItem>().ToArray();
            Check(categories.Length==17 && categories.Count(t=>Equals(t.Tag,"patreon"))==1 && categories.Count(t=>Equals(t.Tag,"resources"))==1 && categories.Count(t=>Equals(t.Tag,"checklist"))==1 && categories.Count(t=>Equals(t.Tag,"cubes"))==1 && categories.Count(t=>Equals(t.Tag,"help"))==1 && categories.Count(t=>Equals(t.Tag,"updates"))==1 && categories.Count(t=>Equals(t.Tag,"feedback"))==1 && ((System.Windows.Controls.TextBox)settings.FindName("HkClick")).IsReadOnly,
                "Central settings include separate Updates, Patreon and Feedback tabs, Events, Pets and profiles while retaining recorded hotkeys");
            var headerPin=(System.Windows.Controls.Button)((System.Windows.Controls.ContentControl)activeOverlay.FindName("ShellPin")).Content;
            var beforePin=activeOverlay.Left;var clickBefore=meter.Settings.ClickThrough;
            headerPin.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            Check(WindowPinning.IsPinned(activeOverlay) && WindowPinning.BlocksMove(activeOverlay,0x0112,0xF012),"Pinning independently blocks native window-move commands");
            NativeMethods.SendMessage(new WindowInteropHelper(activeOverlay).Handle,0x0112,0xF012,0);
            Check(activeOverlay.Left==beforePin && meter.Settings.ClickThrough==clickBefore,"Pinned meter stays in place without changing click-through");
            headerPin.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            Check(!WindowPinning.BlocksMove(activeOverlay,0x0112,0xF012),"Unpinning restores movement without disabling controls");
            Check(((System.Windows.Controls.Panel)activeOverlay.FindName("FooterControls")).Children[0]==activeOverlay.FindName("DeathRecapButton"),"Death recap toggle is the leftmost footer button");
            Check(Descendants(activeOverlay).OfType<System.Windows.Controls.Button>().Count(b=>b.Content is string glyph && glyph=="\uE713")==1,"Only one settings gear remains in the DPS meter");
            Check(activeOverlay.FindName("ViewMenuButton") is null,"Unused View button is removed from the DPS footer");
            var detached = new AionMeter.App.Windows.TimerCardWindow(cards, "auto:shugo", new CardPlacement());
            windows.Add(detached);
            detached.SetDisplayOnly(true);
            Check(detached.Height==28 && detached.Content is System.Windows.Controls.Border single && single.Child is System.Windows.Controls.Grid singleRow && singleRow.ColumnDefinitions.Count==4,
                "Explicitly detached events retain a single-line layout with a compact position pin");
            Check(detached.Content is System.Windows.Controls.Border panel &&
                panel.Background is System.Windows.Media.SolidColorBrush cardBackground && cardBackground.Color.A > 0,
                "Detached locked events retain their card background");
            var beforeMidnight = new DateTimeOffset(2026, 10, 13, 23, 40, 0, TimeSpan.FromHours(2));
            Check(TimerCards.StartLabel(beforeMidnight.AddMinutes(30), beforeMidnight) == "Tomorrow 00:10",
                "Event start label handles midnight correctly");
            Check(!cards.Saved.Sounds.Values.Any(s => s.Enabled), "Event sounds are opt-in");
            var bossWindow = new AionMeter.App.Windows.BossTimersWindow(meter);
            windows.Add(bossWindow);
            WindowClickThrough.Attach(bossWindow, () => true);
            var bossShell = (System.Windows.Controls.Border)bossWindow.FindName("BossShell");
            Check(bossShell.Background is System.Windows.Media.SolidColorBrush bossBg && bossBg.Color.A == 255,
                "Boss timers retain a solid background independently of the overlay lock");
            bossWindow.Left=-30000;bossWindow.Top=-30000;bossWindow.WindowStartupLocation=WindowStartupLocation.Manual;bossWindow.ShowActivated=false;
            bossWindow.Show();bossWindow.UpdateLayout();
            var bossClose=(System.Windows.Controls.Button)bossWindow.FindName("CloseBossButton");
            var bossHandle=new WindowInteropHelper(bossWindow).Handle;
            foreach(var locked in new[]{true,false,true})
            {
                WindowClickThrough.ApplyAll(locked);WindowClickThrough.SetOpacity(.4);
                NativeMethods.SendMessage(bossHandle,0x0006,0,0); // WA_INACTIVE, delivered only to our own test window.
                Check((Style(bossWindow)&(NativeMethods.WS_EX_TRANSPARENT|NativeMethods.WS_EX_NOACTIVATE))==0 && bossWindow.Opacity==1 && bossClose.IsEnabled,
                    "Boss timer controls remain interactive after deactivation, independently of click-through and opacity: "+locked);
                var closePoint=bossClose.PointToScreen(new Point(bossClose.ActualWidth/2,bossClose.ActualHeight/2));
                var packed=(nint)(((int)closePoint.Y&0xffff)<<16|((int)closePoint.X&0xffff));
                Check(NativeMethods.SendMessage(bossHandle,0x0084,0,packed)==1,
                    "Windows still treats the inactive Boss timers X as clickable client content: "+locked);
            }
            var bossClosed=false;bossWindow.Closed+=(_,_)=>bossClosed=true;
            bossClose.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            Check(bossClosed && !bossWindow.IsVisible,"The Boss timers X closes its window while the global overlay lock is active");
            var savedCustom = new EventSoundPreference { Sound = "Custom WAV", FilePath = Path.Combine(AppContext.BaseDirectory,"sounds","Chime.wav") };
            cards.Saved.Sounds["shugo"] = savedCustom;
            Check(Windows.EventSoundsWindow.HasCustomFile(savedCustom) && Windows.EventSoundsWindow.CustomSoundLabel(savedCustom)=="Chime.wav", "Custom sound displays its filename and recognizes an existing saved file");
            Check(!Windows.EventSoundsWindow.HasCustomFile(new EventSoundPreference { Sound="Custom audio" }), "Custom selection without a file requests the picker instead of previewing an empty path");
            var soundWindow = new AionMeter.App.Windows.EventSoundsWindow(cards, (_, _) => { }, () => null); windows.Add(soundWindow);
            var soundRoot=(System.Windows.Controls.DockPanel)((System.Windows.Controls.Border)soundWindow.Content).Child;
            var soundBody=(System.Windows.Controls.StackPanel)((System.Windows.Controls.ScrollViewer)soundRoot.Children[1]).Content;
            var soundRow=(System.Windows.Controls.StackPanel)((System.Windows.Controls.Border)soundBody.Children[1]).Child;
            var soundControls=(System.Windows.Controls.StackPanel)soundRow.Children[1];
            Check(((System.Windows.Controls.ComboBox)soundControls.Children[0]).SelectedItem?.ToString()=="Chime.wav" && savedCustom.Sound=="Custom audio", "Sound window preserves legacy preference and renders the chosen filename in the dropdown");
            Check(((System.Windows.Controls.Button)soundControls.Children[1]).Content?.ToString()=="Change audio…", "Selected custom audio has a change-file button");
            var secondSoundRow=(System.Windows.Controls.StackPanel)((System.Windows.Controls.Border)soundBody.Children[2]).Child;
            var secondSoundControls=(System.Windows.Controls.StackPanel)secondSoundRow.Children[1];
            var secondSound=(System.Windows.Controls.ComboBox)secondSoundControls.Children[0];
            var previousSound=cards.Saved.Sounds["rift"].Sound; var previousIndex=secondSound.SelectedIndex;
            secondSound.SelectedIndex=3;
            Check(secondSound.SelectedIndex==previousIndex && cards.Saved.Sounds["rift"].Sound==previousSound && cards.Saved.Sounds["rift"].FilePath is null,"Cancelling custom file selection restores the previous sound and never saves an empty custom preference");
            Check(soundWindow.Background is System.Windows.Media.SolidColorBrush soundsBg && soundsBg.Color.A == 255,
                "Sound preferences remain opaque and interactive");
            Check(cards.Saved.Sounds.Count == 8 && !cards.Saved.Sounds.Values.Any(s => s.Enabled), "All automatic events and tracked bosses have disabled sound preferences");
            var notes = new AionMeter.App.Windows.PatchNotesWindow(meter.Updates); windows.Add(notes);
            Check(notes.Content is not null, "Patch notes can open without a network request");
            Check(notes.ReleaseTabs.Count == 10 && notes.ReleaseTabs.All(tag => AionMeter.Core.Updates.ReleaseVersion.Parse(tag)?.Channel == AionMeter.Core.Updates.UpdateChannel.Stable),
                "Patch notes offer the latest ten stable releases in descending version order offline");
            var oldestNotes = notes.ReleaseTabs[^1];
            notes.SelectRelease(oldestNotes);
            Check(notes.SelectedRelease == oldestNotes, "Older patch notes can be selected independently");
            notes.SetReleases(Enumerable.Range(1, 11).Select(n => new AionMeter.Core.Updates.ReleaseNotePage($"v99.0.{n}", $"### Release {n}\n- Updated notes")), AionMeter.Core.Updates.UpdateChannel.Stable);
            Check(notes.ReleaseTabs.Count == 10 && notes.ReleaseTabs[0] == "v99.0.11" && notes.SelectedRelease == "v99.0.11",
                "Refreshing notes keeps ten tabs and selects the newest release when the old selection drops out");
            Check(notes.ContentsCount == 1, "Each selected release builds its own clickable contents above the detailed notes");
            Check(notes.WindowStyle == WindowStyle.None && notes.Background is System.Windows.Media.SolidColorBrush notesBg && notesBg.Color.A == 255,
                "Patch notes use the custom CatScratch header and remain opaque");
            var fightMenu = activeOverlay.CreateFightMenu(activeOverlay);
            var picks = fightMenu.Items.OfType<System.Windows.Controls.MenuItem>().Where(item => item.IsCheckable).ToArray();
            Check(picks.Length > 1 && picks.Any(item => item.IsChecked), "Compact fight menu retains live and encounter selections");
            picks[1].RaiseEvent(new RoutedEventArgs(System.Windows.Controls.MenuItem.ClickEvent));
            var sessionStats=Application.Current.Windows.OfType<Windows.BreakdownWindow>().Single(w=>!windows.Contains(w));
            windows.Add(sessionStats); sessionStats.ReloadNow();
            Check(sessionStats.IsVisible && ((System.Windows.Controls.TextBlock)sessionStats.FindName("PlayerName")).Text=="Sample" &&
                activeOverlay.CreateFightMenu(activeOverlay).Items.OfType<System.Windows.Controls.MenuItem>().First().IsChecked,
                "Selecting an earlier fight opens its statistics immediately while the meter stays Live");
            var sessionDamage=((System.Windows.Controls.TextBlock)sessionStats.FindName("StatDamage")).Text;
            picks[0].RaiseEvent(new RoutedEventArgs(System.Windows.Controls.MenuItem.ClickEvent));
            Check(activeOverlay.CreateFightMenu(activeOverlay).Items.OfType<System.Windows.Controls.MenuItem>().First().IsChecked,
                "Compact fight menu returns to live tracking");
            Check(!UiText.Current.NoFights.Contains("automatically", StringComparison.OrdinalIgnoreCase), "Empty fight menu describes manual saving");
            Check(DirectUpdate.Root == Path.Combine(AppContext.BaseDirectory, "userdata", "temp", "updates"),
                "Temporary update downloads and staging stay inside the installation");
            Check(!new AppSettings().SaveHistory, "Automatic fight saving is disabled by default");
            var historyBefore = meter.History.List().Count;
            Check(!meter.CanSaveLastFight, "Manual saving waits for a completed fight");
            meter.Tracker.Tick(1_000_000);
            activeOverlay.Refresh();
            Check(activeModel.Title == "Room stats" && activeModel.SegmentId is null && activeModel.Rows.Count == 1 &&
                activeModel.Rows[0].GearScore == "1894" && activeModel.Rows[0].HasCombatPower,
                "Current group-search stats replace a completed fight in the live view");
            var previousFight = activeOverlay.CreateFightMenu(activeOverlay).Items.OfType<System.Windows.Controls.MenuItem>()
                .Where(item => item.IsCheckable).ElementAt(1);
            previousFight.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.MenuItem.ClickEvent));
            Check(activeModel.Title=="Room stats" && activeModel.SegmentId is null && sessionStats.IsVisible &&
                Application.Current.Windows.OfType<Windows.BreakdownWindow>().All(w=>windows.Contains(w)),
                "Historical statistics stay separate from room stats and repeated clicks reuse their window");
            activeOverlay.ShowLive();
            Check(activeModel.Title == "Room stats", "Returning to live restores the current group-search stats");
            meter.PartyPreview.Observe(new AionMeter.Core.Events.ZoneChangedEvent(1_000_000,600001,"Dungeon",true,true));
            activeOverlay.Refresh();
            Check(activeModel.SegmentId==segment && activeModel.Rows[0].Damage!="—" && activeModel.Title!="Room stats" && activeModel.Title!="Party stats",
                "Finished boss damage stays visible inside the dungeon instead of being replaced by CP/GS preview");
            Check(meter.CanSaveLastFight && meter.History.List().Count == historyBefore,
                "Completing a fight makes it saveable without automatically writing statistics");
            var manuallySaved = meter.SaveLastFight();
            Check(manuallySaved is not null && meter.History.Load(manuallySaved) is { } && !meter.CanSaveLastFight,
                "Manual save writes a readable fight and disables duplicate saving");
            Check(meter.SaveLastFight() is null, "Repeated Save last fight does not duplicate statistics");
            var savedEntry=meter.History.List().Single(e=>e.Path==manuallySaved);
            activeOverlay.OpenSavedStatistics(savedEntry);
            var savedStats=Application.Current.Windows.OfType<Windows.BreakdownWindow>().Single(w=>!windows.Contains(w));
            windows.Add(savedStats); savedStats.ReloadNow();
            Check(savedStats.IsVisible && ((System.Windows.Controls.TextBlock)savedStats.FindName("StatDamage")).Text==sessionDamage &&
                activeOverlay.CreateFightMenu(activeOverlay).Items.OfType<System.Windows.Controls.MenuItem>().First().IsChecked,
                "Saved fight statistics open directly without replacing the live meter");
            if (historyBefore == 0)
            {
                meter.DeleteSavedFights();
                Check(meter.History.List().Count == 0 && !File.Exists(manuallySaved) && meter.CanSaveLastFight,
                    "Deleting saved fights removes statistics and allows saving the last fight again");
            }
            else meter.History.Delete(manuallySaved!);
            meter.Tracker.Process(new AionMeter.Core.Events.DamageEvent(1_000_001,1,501,17010000,250,Core.Events.HitFlags.None));
            activeOverlay.Refresh(); sessionStats.ReloadNow();
            Check(activeModel.SegmentId!=segment && activeModel.HasData &&
                ((System.Windows.Controls.TextBlock)sessionStats.FindName("StatDamage")).Text==sessionDamage,
                "A new live fight updates the meter while the historical statistics retain their original values");
            var gearPreview = new AionMeter.App.ViewModels.OverlayViewModel();
            foreach (var gsVisible in new[] { true, false })
            foreach (var cpVisible in new[] { true, false })
            {
                gearPreview.ShowGear = gsVisible; gearPreview.ShowCombatPower = cpVisible;
                gearPreview.ApplyParty([new("Sample", 2305, 1, 1900, 85000, AionMeter.Core.Events.GameClass.Cleric)], "Sample");
                var gearRow = gearPreview.Rows.Single();
                Check(gearRow.CompactGear.Contains("GS") == gsVisible && gearRow.CompactGear.Contains("CP") == cpVisible &&
                    gearRow.HasGearScore == gsVisible && gearRow.HasCombatPower == cpVisible,
                    "GS and CP visibility are independent in compact and normal party rows");
            }
            var bossCards = new TimerCards(meter, persistent: false);
            bossCards.Saved.SelectedEvents = [];
            var bossTime = new DateTimeOffset(2026, 10, 13, 12, 40, 0, TimeSpan.FromHours(2));
            Check(bossCards.NextEvents(bossTime).Count == 0, "Disabled world-boss option does not add scheduled bosses");
            bossCards.Saved.ShowAllWorldBosses = true;
            Check(bossCards.NextEvents(bossTime).Any(e => e.Id == "kaira") && bossCards.NextEvents(bossTime).All(e => e.StartsAt <= bossTime.AddHours(1)),
                "All world bosses enables scheduled bosses while preserving the one-hour upcoming limit");
            Check(AionMeter.Core.Events.EventKinds.For("boss:2305:123") == "BOSS", "Live world-boss cards retain the boss color category");
            Check(activeOverlay.FindName("EventsButton") is null, "Unused meter Events button is removed");
            meter.Settings.ShowPersonalCombatModule = false;
            activeOverlay.TogglePersonalCombat();
            Check(meter.Settings.ShowPersonalCombatModule,"Personal-combat shortcut enables its integrated section");
            activeOverlay.TogglePersonalCombat();
            Check(!meter.Settings.ShowPersonalCombatModule,"Personal-combat shortcut independently disables its section");
            meter.Settings.ShowPersonalCombatModule = true;
            meter.Tracker.Process(new AionMeter.Core.Events.DamageEvent(1_000_001,1,500,17010000,100,AionMeter.Core.Events.HitFlags.None));
            meter.Tracker.Process(new Core.Events.NpcHpEvent(1_000_001,1,5000,10000,false));
            meter.Tracker.Process(new AionMeter.Core.Events.HealEvent(1_000_002,1,1,17010000,78500,AionMeter.Core.Events.HitFlags.None));
            meter.Tracker.Process(new Core.Events.NpcHpEvent(1_000_002,1,10000,10000,false));
            meter.Tracker.Process(new AionMeter.Core.Events.DamageEvent(1_000_003,500,1,2000000,42850,AionMeter.Core.Events.HitFlags.None));
            activeOverlay.Refresh();
            Check(activeModel.Rows.First(r=>r.IsSelf).DamageTaken==Core.Format.Compact(42850),"The own player row displays received damage separately from outgoing damage");
            Check(activeModel.SegmentId!=segment,"A new fight replaces the finished fight automatically");
            var personalMetrics=(System.Windows.Controls.StackPanel)activeOverlay.FindName("PersonalMetrics");
            var healingGrid=(System.Windows.Controls.Grid)((System.Windows.Controls.Border)personalMetrics.Children[0]).Child;
            Check(healingGrid.Children.OfType<System.Windows.Controls.TextBlock>().Last().Text == "5,000",
                "Self healing displays only restored HP and excludes the overhealing portion");
            meter.Tracker.Process(new Core.Events.PlayerSeenEvent(1_000_004,777777,"Healed ally",2305,Core.Events.GameClass.Gladiator));
            meter.Tracker.Process(new Core.Events.NpcHpEvent(1_000_005,777777,5726,5790,false));
            meter.Tracker.Process(new Core.Events.HealEvent(1_000_006,1,777777,17800000,959,Core.Events.HitFlags.None));
            meter.Tracker.Process(new Core.Events.NpcHpEvent(1_000_007,777777,5790,5790,false));
            activeOverlay.Refresh();
            var othersGrid=(System.Windows.Controls.Grid)((System.Windows.Controls.Border)personalMetrics.Children[1]).Child;
            Check(othersGrid.Children.OfType<System.Windows.Controls.TextBlock>().Last().Text=="64","Healing others displays the HP-confirmed gain instead of the full raw heal");
            meter.Tracker.Process(new Core.Events.HealEvent(1_000_008,1,777777,17800000,500,Core.Events.HitFlags.None));
            activeOverlay.Refresh();
            Check(othersGrid.Children.OfType<System.Windows.Controls.TextBlock>().Last().Text=="64*","Unverified healing is excluded and marks the visible total as incomplete");
            meter.Tracker.Process(new Core.Events.HealEvent(1_000_009,1,1,17010000,1000,Core.Events.HitFlags.None));activeOverlay.Refresh();
            Check(healingGrid.Children.OfType<System.Windows.Controls.TextBlock>().Last().Text=="5,000*","Self healing excludes unconfirmed amounts and marks only the confirmed subtotal");
            foreach (var showHealing in new[] { true, false })
            {
                meter.Settings.ShowHealingDone=showHealing;
                activeOverlay.Refresh();
                var metrics=(System.Windows.Controls.StackPanel)activeOverlay.FindName("PersonalMetrics");
                Check(metrics.Children.Count==2 && ((System.Windows.Controls.Border)metrics.Children[0]).Visibility == (showHealing ? Visibility.Visible : Visibility.Collapsed) &&
                    ((System.Windows.Controls.Border)metrics.Children[1]).Visibility == (showHealing ? Visibility.Visible : Visibility.Collapsed),
                    "Personal combat contains only the two healing rows");
            }
            var collapse=(System.Windows.Controls.Button)activeOverlay.FindName("PersonalCollapseButton");
            collapse.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            Check(((System.Windows.Controls.StackPanel)activeOverlay.FindName("PersonalMetrics")).Visibility==Visibility.Collapsed,
                "Personal combat collapses within the DPS window");
            collapse.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            Check(((System.Windows.Controls.StackPanel)activeOverlay.FindName("PersonalMetrics")).Visibility==Visibility.Visible,
                "Personal combat expands without creating another window");
            activeOverlay.SetDisplayOnly(true);
            var firstPersonal=(System.Windows.Controls.Border)((System.Windows.Controls.StackPanel)activeOverlay.FindName("PersonalMetrics")).Children[0];
            Check(firstPersonal.Background is System.Windows.Media.SolidColorBrush lockedPersonal && lockedPersonal.Color.A is > 0 and < 255 && collapse.Visibility==Visibility.Hidden,
                "Locked personal stats retain translucent row backgrounds while controls remain hidden");
            activeOverlay.SetDisplayOnly(false);
            Check(firstPersonal.Background is System.Windows.Media.SolidColorBrush personalBackground && personalBackground.Color.A>0,
                "Unlocking restores integrated personal row surfaces");
            Check(activeOverlay.FindName("PersonalCombatButton") is System.Windows.Controls.Button,
                "Personal module toggle is available beside the CatScratch title");
            Check(activeOverlay.FindName("PetTrackerButton") is System.Windows.Controls.Button,"Separate Pet tracker has a meter button");
            var versionText=(System.Windows.Controls.TextBlock)activeOverlay.FindName("InstalledVersionText");
            activeOverlay.UpdateLayout();
            Check(versionText.Text==activeModel.Version && versionText.Parent==((System.Windows.Controls.TextBlock)activeOverlay.FindName("ShellTitle")).Parent,"Running version sits below the CatScratch title");
            activeOverlay.SetDisplayOnly(true);
            Check(versionText.Visibility==Visibility.Visible,"Installed version remains visible in click-through mode");
            activeOverlay.SetDisplayOnly(false);
            meter.Settings.PetGoal=2;
            var petCatalog=PetCatalog.Load(MeterService.DataDirectory);
            Check(petCatalog.Count(p=>p.Sources.Length>0)==165,"Offline pet catalog includes 165 Global monster-source mappings");
            Check(petCatalog.Single(p=>p.Id==1193).Sources.Any(f=>f.NpcCode==2400455 && f.Zone=="Altgard"),"Expanded farm data preserves the captured Shulak source");
            Check(petCatalog.Single(p=>p.Id==1176).Sources.Any(f=>f.NpcCode==2400707 && f.Zone=="Altgard"),"Expanded farm data preserves the confirmed Mudthorn source");
            Check(petCatalog.All(p=>p.Sources.All(f=>f.NpcCode>0 && f.Faction is "Elyos" or "Asmodians" or "Other")),"Pet farm IDs and faction values are valid");
            var petWindow=new Windows.PetTrackerWindow(meter){Left=-30000,Top=-30000,ShowActivated=false};
            windows.Add(petWindow);petWindow.Show();
            var petList=Descendants(petWindow).OfType<System.Windows.Controls.ListBox>().First();
            Check(petList.Items.Count==0,"Pet tracker waits for a confirmed own baseline");
            Check(Descendants(petWindow).OfType<System.Windows.Controls.TextBlock>().Any(t=>t.Text.StartsWith("Pet progress data not received yet.") && t.Text.Contains("character selection")),"Empty tracker explains character selection or relog without inventing missing pets");
            meter.Pets.Observe(new Core.Events.SelfIdentifiedEvent(1,1,"Sample self",2305,Core.Events.GameClass.Cleric));
            meter.Pets.Observe(new Core.Game.PetCollectionEvent(2,new Dictionary<int,Core.Game.PetState>{{1193,new(1193,1,10)}}));
            meter.Pets.Observe(new Core.Events.NpcSeenEvent(2,7,2400455));
            meter.Pets.Observe(new Core.Events.DamageEvent(2,1,7,17010000,100,Core.Events.HitFlags.None));
            petWindow.Refresh();
            Check(petList.Items.OfType<Windows.PetTrackerWindow.Row>().Single(x=>x.Definition.Id==1193).Progress=="10/25","Pet window renders the confirmed Global 10/25 baseline");
            var farmRow=petList.Items.OfType<Windows.PetTrackerWindow.Row>().Single(x=>x.Definition.Id==1193);
            Check(farmRow.Name==meter.Data.NpcName(2400455) && farmRow.Tooltip==farmRow.Definition.Name,"Pet rows show the attacked farming monster and only the pet name in their tooltip");
            meter.Pets.Observe(new Core.Game.PetSoulEvent(3,new Dictionary<int,int>{{1193,1}}));petWindow.Refresh();
            Check(petList.Items.OfType<Windows.PetTrackerWindow.Row>().Single(x=>x.Definition.Id==1193).Progress=="11/25","Pet window updates to 11/25 from the verified soul delta");
            Check(petList.Items.Count==1 && petList.Items.OfType<Windows.PetTrackerWindow.Row>().Single().Fill==11d/25,"Only the personally attacked pet is shown with exact next-level progress");
            meter.Pets.Observe(new Core.Game.PetCollectionEvent(4,new Dictionary<int,Core.Game.PetState>{{1193,new(1193,1,11)},{1176,new(1176,3,0)},{1047,new(1047,1,3)},{1040,new(1040,1,2)}}));
            foreach(var (actor,code) in new[]{(8u,2400707),(9u,2100453),(10u,2100950)})
            {meter.Pets.Observe(new Core.Events.NpcSeenEvent(5,actor,code));meter.Pets.Observe(new Core.Events.DamageEvent(6,1,actor,17010000,100,Core.Events.HitFlags.None));}
            petWindow.Refresh();
            var latestPets=petList.Items.OfType<Windows.PetTrackerWindow.Row>().ToArray();
            Check(latestPets.Length==3 && latestPets.Select(p=>p.Definition.Id).SequenceEqual(new[]{1040,1047,1193}),"Pet panel keeps the three most recent incomplete attacked pets in order");
            Check(latestPets.All(p=>p.Definition.Id!=1176 && p.Progress!="MAX"),"Completed pets are hidden even in the attacked-target fallback");
            petWindow.SetDisplayOnly(true);
            Check(petWindow.MinHeight==52,"Locked Pet tracker preserves its minimum layout height");
            Check(((System.Windows.Controls.Border)petWindow.Content).Background==System.Windows.Media.Brushes.Transparent,"Locked Pet tracker hides its window background");
            petWindow.SetDisplayOnly(false);
            Check(((System.Windows.Controls.Border)petWindow.Content).Background!=System.Windows.Media.Brushes.Transparent,"Unlocking restores the themed Pet tracker surface");
            var cached=meter.Pets.SavedProgress()!;
            meter.Pets.Observe(new Core.Events.SelfIdentifiedEvent(20,1,"Other",2305,Core.Events.GameClass.Cleric));
            meter.Pets.Observe(new Core.Events.SelfIdentifiedEvent(21,1,cached.Name,cached.ServerId,Core.Events.GameClass.Cleric));
            meter.Pets.RestoreProgress(cached);
            meter.Pets.Observe(new Core.Events.NpcSeenEvent(22,7,2400455));meter.Pets.Observe(new Core.Events.DamageEvent(23,1,7,17010000,100,Core.Events.HitFlags.None));
            petWindow.Refresh();
            Check(petList.Items.OfType<Windows.PetTrackerWindow.Row>().Single().Progress=="~11/25","Restored counters are visibly marked last-known until fresh login data arrives");
            meter.Pets.Observe(new Core.Game.PetCollectionEvent(24,new Dictionary<int,Core.Game.PetState>{{1193,new(1193,1,12)}}));petWindow.Refresh();
            Check(petList.Items.OfType<Windows.PetTrackerWindow.Row>().Single().Progress=="12/25","Fresh login counters replace restored values and remove the cache marker");
            meter.Pets.Observe(new Core.Game.PetSoulEvent(25,new Dictionary<int,int>{{1193,88}}));petWindow.Refresh();
            Check(petList.Items.Count==0,"A pet reaching level three disappears immediately after its final soul loot");
            meter.Pets.Observe(new Core.Game.PetCollectionEvent(26,new Dictionary<int,Core.Game.PetState>{{1193,new(1193,1,12)}}));
            var petLocations=PetCatalog.Locations(MeterService.DataDirectory);
            Check(petLocations.Select(l=>l.PetId).Distinct().Count()==156 && petLocations.All(l=>petCatalog.Any(p=>p.Id==l.PetId) && l.MapId is 1010 or 1110 or 20
                && l.Points.All(p=>double.IsFinite(p.X)&&double.IsFinite(p.Y))),"Offline pet locations contain only matched known pets and valid source-map coordinates");
            meter.Pets.Observe(new Core.Events.ZoneChangedEvent(25,1110,"Altgard",false));petWindow.Refresh();
            var earth=petCatalog.Single(p=>p.Id==1165);
            Check(PetFarmNames.Resolve(earth,meter.Data,petLocations,1110,[],[],[])==meter.Data.NpcName(2400823),"Regional farming names use the current map rather than another faction's source");
            Check(PetFarmNames.Resolve(earth,meter.Data,petLocations,1110,[2100653],[],[])==meter.Data.NpcName(2100653),"A freshly seen farming monster takes precedence over the regional fallback");
            Check(petList.Items.Count is >0 and <=5 && meter.Pets.AttackedTargetId==0
                && petList.Items.OfType<Windows.PetTrackerWindow.Row>().All(p=>p.Progress!="MAX"),"The Pet tracker recommends incomplete current-region pets without any attack");
            meter.Pets.Observe(new Core.Events.NpcSeenEvent(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),77,2400455));petWindow.Refresh();
            Check(petList.Items.OfType<Windows.PetTrackerWindow.Row>().First().Definition.Id==1193,"Freshly seen pet monsters move their pet to the top without an attack");
            meter.Settings.PetOnHitOnly=true;petWindow.Refresh();
            Check(petList.Items.Count==0,"Pet on hit does not populate rows from region or sightings alone");
            meter.Pets.Observe(new Core.Events.DamageEvent(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),1,77,17010000,100,Core.Events.HitFlags.None));petWindow.Refresh();
            Check(petList.Items.OfType<Windows.PetTrackerWindow.Row>().Single().Name==meter.Data.NpcName(2400455),"Pet on hit displays the personally attacked source inside a supported region");
            meter.Pets.Observe(new Core.Game.PetCollectionEvent(27,new Dictionary<int,Core.Game.PetState>{{1193,new(1193,3,0)}}));petWindow.Refresh();
            Check(petList.Items.Count==0,"Pet on hit excludes completed pets just like region mode");
            meter.Settings.PetOnHitOnly=false;petWindow.Refresh();
            Check(petList.Items.Count is >0 and <=5,"Switching back to region mode restores recommendations without restarting capture");
            meter.Pets.InvalidateLocation();petWindow.Refresh();
            Check(petList.Items.Count==0,"Disconnecting clears regional and nearby recommendations instead of retaining the old location");
            petWindow.Close();
            Check(CustomEventAudio.IsCustom("Custom WAV") && CustomEventAudio.IsCustom("Custom audio"),"Legacy custom WAV preferences remain compatible with custom audio");
            Check(CustomEventAudio.AllowedDuration(new Duration(TimeSpan.FromSeconds(10))) && !CustomEventAudio.AllowedDuration(new Duration(TimeSpan.FromSeconds(10.001))) && !CustomEventAudio.AllowedDuration(Duration.Automatic),"Audio duration gate accepts exactly ten seconds and rejects longer or unknown duration before playback");
            foreach(var soundName in new[]{"Chime","Bell","Ping"})
            {
                using var sound=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"sounds",soundName+".wav"));
                Check(Core.Audio.WaveSound.Validate(sound)<=TimeSpan.FromSeconds(10),"Bundled sound fits the WAV duration limit: "+soundName);
            }
            meter.SetLanguage("de");
            Check(UiText.Current.Code=="de" && UiText.Current.WaitingTitle=="Warte auf Kampf" && UiText.Current.Localize("Event settings")=="Event-Einstellungen",
                "German is selectable and interface literals translate without changing stored identifiers");
            foreach(var property in typeof(UiText).GetProperties().Where(p=>p.PropertyType==typeof(string) && p.SetMethod is not null))
            {
                var english=(string)property.GetValue(new UiText())!; var german=(string)property.GetValue(UiText.Current)!;
                var fields=System.Text.RegularExpressions.Regex.Matches(english,@"\{[^}]+\}").Select(m=>m.Value).Order().ToArray();
                var translatedFields=System.Text.RegularExpressions.Regex.Matches(german,@"\{[^}]+\}").Select(m=>m.Value).Order().ToArray();
                Check(fields.SequenceEqual(translatedFields),"German retains formatting placeholders: "+property.Name);
            }
            activeOverlay.OnLanguageChanged();
            Check(((System.Windows.Controls.TextBlock)healingGrid.Children[2]).Text=="Selbstheilung","Personal metric labels switch to German in the live meter");
            var germanSettings=new AionMeter.App.Windows.SettingsWindow(meter); windows.Add(germanSettings);
            var languages=(System.Windows.Controls.ComboBox)germanSettings.FindName("LanguageBox");
            Check(languages.Items.Count==4 && languages.SelectedIndex==1 && new AppSettings().Language=="en" && meter.Data.Language=="de","All four languages are selectable; fresh installs default to English");
            static IEnumerable<System.Windows.DependencyObject> Descendants(System.Windows.DependencyObject node)
            {
                foreach(var child in System.Windows.LogicalTreeHelper.GetChildren(node).OfType<System.Windows.DependencyObject>())
                { yield return child; foreach(var nested in Descendants(child)) yield return nested; }
            }
            var germanSounds=new AionMeter.App.Windows.EventSoundsWindow(cards,(_,_)=>{}); windows.Add(germanSounds);
            var localizedSound=Descendants(germanSounds).OfType<System.Windows.Controls.ComboBox>().Skip(1).First();
            localizedSound.SelectedIndex=1;
            Check(cards.Saved.Sounds[Core.Events.AutomaticEventSchedule.Catalog.First().Id].Sound=="Bell" && Equals(localizedSound.SelectedItem,"Glocke"),
                "German sound choices store stable sound IDs rather than translated labels");
            foreach(var code in new[]{"ru","fr"})
            {
                meter.SetLanguage(code); activeOverlay.OnLanguageChanged();
                var text=UiText.Current;
                Check(text.Code==code && meter.Data.Language==code && UiText.Normalize(code.ToUpperInvariant()+"-XX")==code,"Language and game-name tables switch together: "+code);
                foreach(var property in typeof(UiText).GetProperties().Where(p=>p.PropertyType==typeof(string) && p.SetMethod is not null))
                {
                    var english=(string)property.GetValue(new UiText())!; var translated=(string)property.GetValue(text)!;
                    var fields=System.Text.RegularExpressions.Regex.Matches(english,@"\{[^}]+\}").Select(m=>m.Value).Order();
                    var localFields=System.Text.RegularExpressions.Regex.Matches(translated,@"\{[^}]+\}").Select(m=>m.Value).Order();
                    Check(fields.SequenceEqual(localFields),code+" formatting placeholders: "+property.Name);
                }
                var extra=(IReadOnlyDictionary<string,string>)typeof(UiText).GetField("ExtraGerman",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!.GetValue(null)!;
                var target=(IReadOnlyDictionary<string,string>)typeof(UiText).GetField(code=="ru" ? "ExtraRussian" : "ExtraFrench",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!.GetValue(null)!;
                Check(extra.Keys.All(target.ContainsKey),"Every supplemental interface literal has a translation: "+code);
                var localizedSettings=new AionMeter.App.Windows.SettingsWindow(meter); windows.Add(localizedSettings);
                Check(((System.Windows.Controls.ComboBox)localizedSettings.FindName("LanguageBox")).SelectedIndex==Array.IndexOf(UiText.Languages,code),"Settings retain language selection: "+code);
                var report=(System.Windows.Controls.Button)localizedSettings.FindName("ReportButton");
                Check((string)report.Content==text.ReportButton && text.ReportButton!=new UiText().ReportButton,"Feedback button is translated: "+code);
            }
            Check(new Uri(Feedback.IssueUrl).Host=="github.com" && new Uri(Feedback.IssueUrl).Query.Length==0,"Feedback opens the official issue chooser without sending user data");
            AssetDownloadChecks.Run(output,Check);
            PolishChecks.Run(meter,output,Check);
            meter.SetLanguage("en"); activeOverlay.OnLanguageChanged();
            Check(((System.Windows.Controls.TextBlock)healingGrid.Children[2]).Text=="Self healing","Switching back to English restores the live labels");
            meter.Settings.ShowHealingDone=true;
            using var legacyUpdater = new Updater(new AppSettings { Transient = true, AutoInstallUpdates = true });
            var release = new AionMeter.Core.Updates.ReleaseInfo(new Version(99, 0, 0), "v99.0.0",
                "https://example.invalid/release", "Verification only", null, null,
                new AionMeter.Core.Updates.ReleaseAsset("CatScratch-win-x64.zip", 1, "https://example.invalid/package", new string('a', 64)));
            legacyUpdater.Preview(release);
            var updateWindow = new AionMeter.App.Windows.UpdateWindow(legacyUpdater, release, false); windows.Add(updateWindow);
            var updateButton = (System.Windows.Controls.Button)updateWindow.FindName("UpdateButton");
            var downloadBar = (System.Windows.Controls.ProgressBar)updateWindow.FindName("DownloadBar");
            Check(updateButton.IsEnabled && Equals(updateButton.Content, UiText.Current.Localize("Prepare update")) &&
                downloadBar.Visibility == Visibility.Collapsed,
                "Update preview requires manual preparation and never restarts automatically, including legacy automatic-install settings");
            File.WriteAllText(output, JsonSerializer.Serialize(passed, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally { foreach (var window in windows) window.Close(); }
    }
}
