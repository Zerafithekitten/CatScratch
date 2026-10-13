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
            using (var appIcon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!))
            using (var bitmap = appIcon!.ToBitmap())
            {
                var paw = bitmap.GetPixel(bitmap.Width / 2, bitmap.Height * 2 / 3);
                Check(paw.B > paw.R && paw.R > paw.G, "Published executable contains the purple paw icon");
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
                        foreach (var window in new[] { first, second })
            {
                Check(ReferenceEquals(window.Icon, AppIcon.Image), "Window uses the packaged purple paw directly");
                foreach (var size in new[] { 0, 1 })
                {
                    var handle = NativeMethods.SendMessage(new WindowInteropHelper(window).Handle, 0x007F, size, 0);
                    Check(handle != 0, "Native window provides its own small and large taskbar icons");
                    using var nativeIcon = System.Drawing.Icon.FromHandle(handle);
                    using var bitmap = nativeIcon.ToBitmap();
                    var pixel = bitmap.GetPixel(bitmap.Width / 2, bitmap.Height * 2 / 3);
                    Check(pixel.B > pixel.R && pixel.R > pixel.G, "Native taskbar icon contains the purple paw");
                }
            }
            using (var trayIcon = AppIcon.CreateTrayIcon())
            using (var bitmap = trayIcon.ToBitmap())
            {
                var pixel = bitmap.GetPixel(bitmap.Width / 2, bitmap.Height * 2 / 3);
                Check(pixel.B > pixel.R && pixel.R > pixel.G, "Tray icon uses the packaged purple paw directly");
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
            Check(!HotkeyManager.TryParse("+++", out _, out _), "Invalid hotkey text rejected");
            var elevation = AdministratorStartup.StartInfo("C:\\Cat Scratch\\CatScratch.exe", ["--updated", "argument with spaces"]);
            Check(elevation.UseShellExecute && elevation.Verb == "runas" && elevation.ArgumentList.SequenceEqual(new[] { "--updated", "argument with spaces" }),
                "Normal startup requests administrator elevation and preserves arguments");
            using var meter = new MeterService(new AppSettings { Transient = true, DownloadIcons = false }, null);
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
            Check(header.Visibility == Visibility.Collapsed, "Locked events hide the shared header");
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
            Check(list.Children.OfType<System.Windows.Controls.Border>().Count()==3 && list.Children.OfType<System.Windows.Controls.Border>().Count(b=>b.BorderBrush.HasAnimatedProperties)==2,
                "Started events pulse while later events within the hour remain visible without pulsing");
            hub.SetDisplayOnly(true);
            Check(list.Children.OfType<System.Windows.Controls.Border>().Count(b=>b.BorderBrush.HasAnimatedProperties)==2,
                "Locking preserves only active event border animation");
            hub.ClockOverride=()=>new DateTimeOffset(2026,10,13,14,2,0,TimeSpan.FromHours(2)); hub.Reload();
            Check(cards.NextEvents(hub.ClockOverride()).All(e=>e.StartsAt>hub.ClockOverride()) && list.Children.OfType<System.Windows.Controls.Border>().All(b=>!b.BorderBrush.HasAnimatedProperties),
                "Started events expire at two minutes and upcoming borders stop pulsing");
            cards.Saved.SelectedEvents=["shugo","rift","invasion"];
            hub.ClockOverride=()=>new DateTimeOffset(2026,10,13,13,10,0,TimeSpan.FromHours(2)); hub.Reload();
            Check(list.Children.OfType<System.Windows.Controls.Border>().Count()==3 && cards.NextEvents(hub.ClockOverride()).Select(e=>e.StartsAt).Distinct().Count()==2,
                "Upcoming events display all selected start times within the hour, not just the earliest");
            cards.Saved.SelectedEvents=["shugo","rift"];
            var eventPreferences=new AionMeter.App.Windows.EventPreferencesWindow(cards,()=>480,_=>{},_=>{},()=>{}); windows.Add(eventPreferences);
            Check(eventPreferences.Background is System.Windows.Media.SolidColorBrush eventBg && eventBg.Color.A==255 && eventPreferences.Opacity==1,
                "Event settings use a solid interactive meter-style surface");
            var settings=new AionMeter.App.Windows.SettingsWindow(meter); windows.Add(settings);
            Check(((System.Windows.Controls.TabControl)settings.FindName("SettingsTabs")).Items.Count==6 && ((System.Windows.Controls.TextBox)settings.FindName("HkClick")).IsReadOnly,
                "Settings are organized into six categories while retaining recorded hotkeys");
            Check(activeOverlay.FindName("ViewMenuButton") is null,"Unused View button is removed from the DPS footer");
            var detached = new AionMeter.App.Windows.TimerCardWindow(cards, "auto:shugo", new CardPlacement());
            windows.Add(detached);
            detached.SetDisplayOnly(true);
            Check(detached.Height==28 && detached.Content is System.Windows.Controls.Border single && single.Child is System.Windows.Controls.Grid singleRow && singleRow.ColumnDefinitions.Count==3,
                "Explicitly detached events use the same single-line compact layout");
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
                "Boss timers retain a solid background after click-through registration");
            var soundWindow = new AionMeter.App.Windows.EventSoundsWindow(cards, (_, _) => { }); windows.Add(soundWindow);
            Check(soundWindow.Background is System.Windows.Media.SolidColorBrush soundsBg && soundsBg.Color.A == 255,
                "Sound preferences remain opaque and interactive");
            Check(cards.Saved.Sounds.Count == 8 && !cards.Saved.Sounds.Values.Any(s => s.Enabled), "All automatic events and tracked bosses have disabled sound preferences");
            var notes = new AionMeter.App.Windows.PatchNotesWindow(meter.Updates); windows.Add(notes);
            Check(notes.Content is not null, "Patch notes can open without a network request");
            Check(notes.WindowStyle == WindowStyle.None && notes.Background is System.Windows.Media.SolidColorBrush notesBg && notesBg.Color.A == 255,
                "Patch notes use the custom CatScratch header and remain opaque");
            var fightMenu = activeOverlay.CreateFightMenu(activeOverlay);
            var picks = fightMenu.Items.OfType<System.Windows.Controls.MenuItem>().Where(item => item.IsCheckable).ToArray();
            Check(picks.Length > 1 && picks.Any(item => item.IsChecked), "Compact fight menu retains live and encounter selections");
            picks[1].RaiseEvent(new RoutedEventArgs(System.Windows.Controls.MenuItem.ClickEvent));
            Check(activeModel.SegmentId == segment, "Compact fight menu selects the requested encounter");
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
            if (historyBefore == 0)
            {
                meter.DeleteSavedFights();
                Check(meter.History.List().Count == 0 && !File.Exists(manuallySaved) && meter.CanSaveLastFight,
                    "Deleting saved fights removes statistics and allows saving the last fight again");
            }
            else meter.History.Delete(manuallySaved!);
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
            meter.Settings.ShowPersonalCombatModule = true;
            meter.Tracker.Process(new AionMeter.Core.Events.DamageEvent(1_000_001,1,500,17010000,100,AionMeter.Core.Events.HitFlags.None));
            meter.Tracker.Process(new AionMeter.Core.Events.HealEvent(1_000_002,1,1,17010000,78500,AionMeter.Core.Events.HitFlags.None));
            activeOverlay.Refresh();
            Check(activeModel.SegmentId!=segment,"A new fight replaces the finished fight automatically");
            var personalMetrics=(System.Windows.Controls.StackPanel)activeOverlay.FindName("PersonalMetrics");
            var healingGrid=(System.Windows.Controls.Grid)((System.Windows.Controls.Border)personalMetrics.Children[1]).Child;
            Check(healingGrid.Children.OfType<System.Windows.Controls.TextBlock>().Last().Text == "78,500",
                "Integrated personal totals display received self healing for the shown fight");
            foreach (var showTaken in new[] { true, false })
            foreach (var showHealing in new[] { true, false })
            {
                meter.Settings.ShowDamageTaken=showTaken; meter.Settings.ShowHealingDone=showHealing;
                activeOverlay.Refresh();
                var metrics=(System.Windows.Controls.StackPanel)activeOverlay.FindName("PersonalMetrics");
                Check(((System.Windows.Controls.Border)metrics.Children[0]).Visibility == (showTaken ? Visibility.Visible : Visibility.Collapsed) &&
                    ((System.Windows.Controls.Border)metrics.Children[1]).Visibility == (showHealing ? Visibility.Visible : Visibility.Collapsed),
                    "Integrated personal damage and healing sections can be shown independently");
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
            meter.SetLanguage("en"); activeOverlay.OnLanguageChanged();
            Check(((System.Windows.Controls.TextBlock)healingGrid.Children[2]).Text=="Self healing","Switching back to English restores the live labels");
            meter.Settings.ShowDamageTaken=meter.Settings.ShowHealingDone=true;
            using var legacyUpdater = new Updater(new AppSettings { Transient = true, AutoInstallUpdates = true });
            var release = new AionMeter.Core.Updates.ReleaseInfo(new Version(99, 0, 0), "v99.0.0",
                "https://example.invalid/release", "Verification only", null, null,
                new AionMeter.Core.Updates.ReleaseAsset("CatScratch-win-x64.zip", 1, "https://example.invalid/package", new string('a', 64)));
            legacyUpdater.Preview(release);
            var updateWindow = new AionMeter.App.Windows.UpdateWindow(legacyUpdater, release, false); windows.Add(updateWindow);
            var updateButton = (System.Windows.Controls.Button)updateWindow.FindName("UpdateButton");
            var downloadBar = (System.Windows.Controls.ProgressBar)updateWindow.FindName("DownloadBar");
            Check(updateButton.IsEnabled && Equals(updateButton.Content, UiText.Current.UpdateNow) &&
                downloadBar.Visibility == Visibility.Collapsed,
                "Available update waits for Update now without starting download, including legacy automatic-install settings");
            File.WriteAllText(output, JsonSerializer.Serialize(passed, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally { foreach (var window in windows) window.Close(); }
    }
}
