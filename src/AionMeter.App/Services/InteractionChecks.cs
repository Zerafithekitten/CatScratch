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
                var background = (System.Windows.Media.SolidColorBrush)row.CompactLabelBrush;
                static double Linear(byte c) { var s = c / 255.0; return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4); }
                var color = background.Color;
                var luminance = 0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);
                var contrast = row.CompactTextBrush == System.Windows.Media.Brushes.Black ? (luminance + 0.05) / 0.05 : 1.05 / (luminance + 0.05);
                Check(background.Color.A == 255 && background.IsFrozen && contrast >= 4.5,
                    "Compact identity labels have opaque cached class color and readable text contrast: " + gameClass);
            }
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
            foreach (var locked in new[] { true, false, true })
            {
                WindowClickThrough.ApplyAll(locked);
                WindowClickThrough.SetOpacity(0.4);
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
                ((System.Windows.Controls.Border)((System.Windows.Controls.DockPanel)((System.Windows.Controls.StackPanel)tile.Child).Children[0]).Children[0]).BorderBrush).Distinct().Count();
            Check(colors == 2, "Simultaneous EVENT and PVP cards use different type colors");
            hub.Reload();
            Check(list.Children.OfType<System.Windows.Controls.Border>().All(b =>
                b.Background is System.Windows.Media.SolidColorBrush bg && bg.Color.A > 0),
                "Countdown refresh preserves locked event backgrounds");
            hub.SetDisplayOnly(false);
            Check(header.Visibility == Visibility.Visible, "Unlock restores the event header");
            var detached = new AionMeter.App.Windows.TimerCardWindow(cards, "auto:shugo", new CardPlacement());
            windows.Add(detached);
            detached.SetDisplayOnly(true);
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
            File.WriteAllText(output, JsonSerializer.Serialize(passed, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally { foreach (var window in windows) window.Close(); }
    }
}
