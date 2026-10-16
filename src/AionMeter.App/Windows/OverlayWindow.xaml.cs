using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using AionMeter.App.Controls;
using AionMeter.App.Services;
using AionMeter.App.ViewModels;
using AionMeter.Core;
using AionMeter.Core.Capture;
using AionMeter.Core.Combat;
using AionMeter.Core.Storage;

namespace AionMeter.App.Windows;

public partial class OverlayWindow : Window, IDisplayAppearance
{
    private readonly MeterService _meter;
    private readonly AppSettings _settings;
    private readonly OverlayViewModel _vm = new();
    private nint _hwnd;
    private readonly DragAnywhere _drag;
    private long _npcapCheckedAt = long.MinValue;
    private bool _npcapInstalled;
    private static readonly Brush ClickableCursorBrush = CreateCursorBrush();
    private static Brush CreateCursorBrush()
    {
        var brush = ThemeColors.Brush("#C49AF5");
        return brush;
    }


    /// <summary>The render tool's fight clock for animation frames (Unix ms); null = real time.</summary>
    internal static Func<long>? ClockOverride { get; set; }

    public OverlayWindow(MeterService meter, bool exitOnClose = true)
    {
        _meter = meter;
        _settings = meter.Settings;
        var opacity = _settings.BackgroundOpacity; // the slider's coercion during InitializeComponent would overwrite it
        InitializeComponent();
        AppIcon.Apply(this); ShellPaw.Source=AppIcon.Image; ShellPin.Content=WindowPinning.CreateButton(this,_settings,"meter");
        _settings.BackgroundOpacity = opacity;
        DataContext = _vm;
        CreatePersonalRows();
        WindowClickThrough.Attach(this, () => _settings.ClickThrough, () => _settings.WindowOpacity);
        MinWidth = 260;
        Closing += (_, _) => { if (exitOnClose) AppHost.Current.ExitProgram(); };

        Left = _settings.OverlayLeft;
        Top = _settings.OverlayTop;
        Width = Math.Max(MinWidth, _settings.OverlayWidth);
        Height = Math.Max(MinHeight, _settings.OverlayHeight);
        EnsureOnScreen();

        OpacitySlider.Value = _settings.BackgroundOpacity;
        FrameBackground.Opacity = _settings.BackgroundOpacity;
        ApplyLock();
        ApplyRowSize(_settings.RowSize);
        ApplyCompactLayout();
        UpdateModeLabel();
        ApplyHotkeyTips();
        LayoutUpdated += (_, _) => FitNames();
        SizeChanged += (_, _) => { ApplyNarrowLayout(); if (IsLoaded && WindowState == WindowState.Normal) SavePlacement(); };
        ApplyNarrowLayout();
        _meter.Updates.Changed += ShowUpdateBanner;
        ShowUpdateBanner();

        // Drag the card by any part of it (unless locked); a click without movement still opens a breakdown.
        _drag = DragAnywhere.Attach(this, canDrag: () => !_settings.Locked, dropped: SavePlacement);

        // The card stays clean: its buttons only appear while the mouse is over it, in place of the title and the state
        // label (the timer stays).
        MouseEnter += (_, _) =>
        {
            if (_settings.CompactMode) return;
            TitleText.Visibility = Visibility.Collapsed;
            LabelPanel.Visibility = Visibility.Collapsed;
            Toolbar.Visibility = Visibility.Visible;
        };
        MouseLeave += (_, _) =>
        {
            Toolbar.Visibility = Visibility.Collapsed;
            TitleText.Visibility = Visibility.Visible;
            LabelPanel.Visibility = _settings.CompactMode ? Visibility.Collapsed : Visibility.Visible;
        };

        SourceInitialized += (_, _) =>
        {
            _hwnd = new WindowInteropHelper(this).Handle;
            // Clicking the overlay must never pull keyboard focus away from the game window.
            NativeMethods.SetExStyle(_hwnd, 0, NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOOLWINDOW);
            ApplyClickThrough();
            FitToMonitor();
        };
    }

    public nint Handle => _hwnd;

    private void DeathRecap_Click(object sender, RoutedEventArgs e) => ToggleDeathRecap();
    internal void ToggleDeathRecap()
    {
        _settings.ShowDeathRecapModule = !_settings.ShowDeathRecapModule;
        _settings.Save(); Refresh();
    }

    private bool _displayOnly;
    internal bool DisplayOnly=>_displayOnly;
    private Brush? _shellBackground, _shellBorder, _frameBorder;
    private System.Windows.Media.Effects.Effect? _frameEffect;
    private Visibility _toolbarVisibility;
    private long _partyPreviewRevision = -1;
    private bool _showingPartyPreview;
    public void SetDisplayOnly(bool enabled)
    {
        if (!enabled && !_displayOnly) return;
        if (enabled && !_displayOnly)
        {
            _shellBackground=OuterShell.Background; _shellBorder=OuterShell.BorderBrush;
            _frameBorder=Frame.BorderBrush; _frameEffect=Frame.Effect; _toolbarVisibility=Toolbar.Visibility;
        }
        _displayOnly=enabled;
        _vm.IsDisplayOnly = enabled;
        PersonalDivider.Visibility = enabled ? Visibility.Hidden : Visibility.Visible;
        PersonalCollapseButton.Visibility = enabled ? Visibility.Hidden : Visibility.Visible;
        foreach (var row in _personalRows) row.Surface.Background = enabled ? PersonalLockedSurface : PersonalSurface;
        OuterShell.Background=enabled ? Brushes.Transparent : _shellBackground;
        OuterShell.BorderBrush=enabled ? Brushes.Transparent : _shellBorder;
        Frame.Background=enabled ? Brushes.Transparent : FrameBackground;
        Frame.BorderBrush=enabled ? Brushes.Transparent : _frameBorder;
        Frame.Effect=enabled ? null : _frameEffect;
        var controls=enabled ? Visibility.Hidden : Visibility.Visible;
        ShellCaption.Visibility=Visibility.Visible; ShellTitle.Visibility=controls;
        ShellPaw.Visibility=enabled?Visibility.Hidden:(Width<300?Visibility.Collapsed:Visibility.Visible);
        PersonalCombatButton.Visibility=controls;PetTrackerButton.Visibility=controls;ShellClickButton.Visibility=controls;
        ShellSettingsButton.Visibility=controls; ShellHideButton.Visibility=controls; ShellPin.Visibility=controls;
        FooterControls.Visibility=controls; ResizeGrip.Visibility=controls;
        PlayerScroll.VerticalScrollBarVisibility=enabled ? ScrollBarVisibility.Hidden : ScrollBarVisibility.Auto;
        Toolbar.Visibility=enabled ? Visibility.Collapsed : _toolbarVisibility;
        MarioPartyBackdrop.Refresh(this);
    }

    /// <summary>Called by the app's UI timer (≈5 Hz).</summary>
    private void PersonalCombat_Click(object sender, RoutedEventArgs e)=>TogglePersonalCombat();
    internal void TogglePersonalCombat()
    {
        _settings.ShowPersonalCombatModule = !_settings.ShowPersonalCombatModule;
        _settings.Save();
        Refresh();
    }
    private void PetTracker_Click(object sender,RoutedEventArgs e)=>AppHost.Current.TogglePetModule();

    private static readonly Brush PersonalSurface = FrozenBrush("#D0191D23");
    private static readonly Brush PersonalLockedSurface = FrozenBrush("#B8101822");
    private readonly List<(Border Surface, TextBlock Value, TextBlock Label, string English)> _personalRows = [];
    private static Brush FrozenBrush(string color)
    {
        return ThemeColors.Brush(color);
    }
    private void CreatePersonalRows()
    {
        void Add(string label, string color, string shape)
        {
            var accent = FrozenBrush(color);
            var surface = new Border { Background = PersonalSurface, CornerRadius = new(5), Margin = new(0,0,0,2) };
            var grid = new Grid { Height = 24 };
            grid.ColumnDefinitions.Add(new() { Width = new(3) });
            grid.ColumnDefinitions.Add(new() { Width = new(34) });
            grid.ColumnDefinitions.Add(new() { Width = new(1,GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
            grid.Children.Add(new Border { Background = accent, CornerRadius = new(3,0,0,3) });
            var icon = new System.Windows.Shapes.Path { Data = Geometry.Parse(shape), Stroke = accent, StrokeThickness = 1.7, Width = 16, Height = 16, Stretch = Stretch.Uniform, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            Grid.SetColumn(icon,1); grid.Children.Add(icon);
            var text = new TextBlock { Text = UiText.Current.Localize(label), Foreground = Brushes.White, FontSize = 12, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
            Grid.SetColumn(text,2); grid.Children.Add(text);
            var value = new TextBlock { Foreground = Brushes.White, FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new(8,0,9,0) };
            Grid.SetColumn(value,3); grid.Children.Add(value);
            surface.Child = grid; PersonalMetrics.Children.Add(surface); _personalRows.Add((surface,value,text,label));
        }
        Add("Self healing", "#65DFA5", "M8,15 C8,15 1,10 1,5 C1,0 6,0 8,4 C10,0 15,0 15,5 C15,10 8,15 8,15 Z");
        Add("Healing others", "#75C8ED", "M8,15 C8,15 1,10 1,5 C1,0 6,0 8,4 C10,0 15,0 15,5 C15,10 8,15 8,15 Z M8,5 L8,11 M5,8 L11,8");
    }
    private void PersonalCollapse_Click(object sender, RoutedEventArgs e)
    {
        _settings.PersonalCombatCollapsed = !_settings.PersonalCombatCollapsed;
        _settings.Save(); Refresh();
    }
    private void RefreshPersonal(EncounterSnapshot? snapshot)
    {
        PersonalSection.Visibility = _settings.ShowPersonalCombatModule ? Visibility.Visible : Visibility.Collapsed;
        PersonalMetrics.Visibility = _settings.PersonalCombatCollapsed ? Visibility.Collapsed : Visibility.Visible;
        PersonalCollapseButton.Content = _settings.PersonalCombatCollapsed ? "\uE70D" : "\uE70E";
        PersonalCollapseButton.ToolTip = UiText.Current.Localize(_settings.PersonalCombatCollapsed ? "Expand personal combat" : "Collapse personal combat");
        var own = snapshot?.Combatants.FirstOrDefault(c => c.IsSelf);
        PersonalName.Text = StreamNames.Name(own?.Name ?? _meter.LiveIdentity.Current?.Name ?? "");
        _personalRows[0].Surface.Visibility = _personalRows[1].Surface.Visibility = _settings.ShowHealingDone ? Visibility.Visible : Visibility.Collapsed;
        _personalRows[0].Value.Text = own is null ? "—" : Format.Grouped(own.SelfHealing);
        _personalRows[1].Value.Text = own is null ? "—" : Format.Grouped(Math.Max(0,own.Healing-own.SelfHealing));
    }

    private async void SaveLastFight_Click(object sender, RoutedEventArgs e)
    {
        SaveLastFightButton.IsEnabled = false;
        try
        {
            await Task.Run(() => _meter.SaveLastFight());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, string.Format(UiText.Current.Localize("Could not save fight: {0}"),ex.Message), "CatScratch", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { Refresh(); }
    }

    private (bool Gs, bool Cp) _partyGearVisibility;
    public void Refresh()
    {
        DeathRecapButton.ToolTip = UiText.Current.Localize(_settings.ShowDeathRecapModule ? "Disable death recap" : "Enable death recap")
            + " · " + _settings.HotkeyDeathRecap;
        System.Windows.Automation.AutomationProperties.SetName(DeathRecapButton, UiText.Current.ToggleDeathRecapLabel);
        DeathRecapButton.Foreground = _settings.ShowDeathRecapModule ? ClickableCursorBrush : Brushes.Gray;
        ApplyNarrowLayout();
        SaveLastFightButton.IsEnabled = _meter.CanSaveLastFight;
        SetDisplayOnly(_settings.ClickThrough);
        _vm.WallClock = TimerCards.Berlin(DateTimeOffset.UtcNow).ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture);
        var uptime = Environment.TickCount64;
        if (_npcapCheckedAt == long.MinValue || uptime - _npcapCheckedAt >= 2_000)
        {
            _npcapInstalled = LiveCapture.IsNpcapInstalled();
            _npcapCheckedAt = uptime;
        }
        var npcInstalled = _npcapInstalled;
        NpcapDownloadButton.Visibility = npcInstalled || _meter.Settings.Transient || _meter.DemoRunning ? Visibility.Collapsed : Visibility.Visible;
        IdentityStatusText.Text = _meter.Settings.Transient || _meter.DemoRunning
            ? UiText.Current.Localize("PREVIEW / DEMO – no confirmed live data")
            : !npcInstalled
            ? UiText.Current.Localize("Npcap is missing. Open the download page, install it yourself and restart the meter. Then teleport once in game.")
            : _meter.LiveIdentity.Current is { } identity
            ? string.Format(UiText.Current.Localize(_meter.Tracker.PartyConfirmed ? "Party confirmed · {0}" : "Solo tracking · {0}"),StreamNames.Name(identity.Name))
            : _meter.LiveIdentity.KnownCharacter is { } known
            ? string.Format(UiText.Current.Localize("Character recognized · {0}. Waiting for fresh actor data from the game…"),StreamNames.Name(known.Name))
            : UiText.Current.Localize("Character not confirmed yet. Teleport once or change zones through a loading screen. Waiting for fresh game data…");
        IdentityStatusText.Foreground = _meter.LiveIdentity.Current is not null && !_meter.DemoRunning
            ? (Brush)FindResource("Green") : (Brush)FindResource("Amber");
        ClickModeSlash.Visibility = _settings.ClickThrough ? Visibility.Visible : Visibility.Collapsed;
        ClickModeCursor.Fill = _settings.ClickThrough ? Brushes.Gray : ClickableCursorBrush;
        SetupPanel.Visibility = _settings.CompactMode && (_settings.Transient || _meter.DemoRunning || _meter.LiveIdentity.ActorId is not null) ? Visibility.Collapsed : Visibility.Visible;
        if (_settings.ClickThrough) SetupPanel.Visibility = Visibility.Collapsed;
        Title = AppInfo.Name;
        ShellTitle.Text = _meter.DemoRunning ? "CatScratch · TEST" : "CatScratch";
        _vm.ShowGear = _settings.ShowGear;
        _vm.ShowCombatPower = _settings.ShowCombatPower ?? _settings.ShowGear;
        {
            var now = ClockOverride?.Invoke() ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var snap = _meter.Tracker.Snapshot(null, now);
            var party = _meter.PartyPreview.Snapshot();
            if (!_meter.PartyPreview.InDungeon && party.Members.Count > 0 &&
                (snap is null || (party.InRoom && !snap.IsActive)))
            {
                if (!_showingPartyPreview || _partyPreviewRevision != party.Revision || _partyGearVisibility != (_vm.ShowGear, _vm.ShowCombatPower))
                    _vm.ApplyParty(party.Members, _meter.LiveIdentity.Current?.Name ?? "", party.InRoom);
                _partyPreviewRevision = party.Revision;
                _partyGearVisibility = (_vm.ShowGear, _vm.ShowCombatPower);
                _showingPartyPreview = true;
                _vm.SetSegment("live");
            }
            else
            {
                _showingPartyPreview = false;
                _vm.Apply(snap, _settings.MaxRows, _settings.BarsRelativeToTop);
                _vm.SetSegment("live", snap?.StartedAt.ToString("HH:mm"));
            }
            RefreshPersonal(_showingPartyPreview ? null : snap);
        }
        if (!_settings.Transient && !_meter.DemoRunning && _meter.LiveIdentity.ActorId is null)
            foreach (var row in _vm.Rows) row.IsSelf = false;
        if (!_settings.ShowBossPanel) _vm.HasBoss = false;
        // The instance's time limit (or the wait after entering), whatever fight is shown: it is about where you are now.
        var wall = ClockOverride?.Invoke() ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _vm.SetLimit(_meter.Tracker.NextTimer(wall), wall);
        if (Environment.TickCount64 - _timersLabelAt >= 1_000) UpdateTimersLabel();

        var status = _meter.DemoRunning ? new CaptureStatus(CaptureState.Capturing, UiText.Current.Localize("Test mode · sample data")) : _meter.CaptureStatus;
        LatencyText.Text = _meter.LatencyMs is { } latency ? latency<1 ? "RTT <1 ms" : $"RTT {latency:0} ms" : "RTT —";
        // Names are only sent on a loading screen: players already around when the meter started show as #id.
        var unnamed = _vm.Rows.Any(r => r.Name.Contains('#')); // "Templar #10388": the server has not sent this name yet
        _vm.Status = status.State != CaptureState.Capturing ? UiText.Current.CaptureMessage(status.Message)
            : _vm.MostDamageUnsent && _vm.HasData ? UiText.Current.DamageUnsentTip // the footer says it, its tooltip says why
            : unnamed ? UiText.Current.Localize("Names appear after the next loading screen")
            : UiText.Current.CaptureMessage(status.Message);
        // Footer reads like the card's caption; capture problems take its place so they are never missed.
        FooterText.Text = status.State is CaptureState.Capturing && _vm.HasData
            ? (unnamed ? _vm.Footer + " · " + UiText.Current.NamesLater : _vm.Footer)
            : _vm.Status;
        _vm.StatusBrush = status.State switch
        {
            CaptureState.Capturing => (Brush)FindResource("Green"),
            CaptureState.Error => (Brush)FindResource("Red"),
            CaptureState.WaitingForGame or CaptureState.Starting => (Brush)FindResource("Amber"),
            _ => (Brush)FindResource("TextMute"),
        };
    }

    public void OnLanguageChanged()
    {
        _vm.LanguageChanged();
        _partyPreviewRevision=-1;
        foreach(var row in _personalRows) row.Label.Text=UiText.Current.Localize(row.English);
        ShowUpdateBanner();
        ApplyHotkeyTips();
        Refresh();
    }

    private string? _bannerClosedFor;

    /// <summary>The green "Version 0.2.0 is out" banner above the footer, while an update is available (not skipped or closed).</summary>
    private void ShowUpdateBanner() => ShowUpdateBanner(_meter.Updates);

    internal void ShowUpdateBanner(Updater updates)
    {
        var available = updates.Ready;
        var show = available is not null && available.Tag != _bannerClosedFor;
        UpdateBanner.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (show)
        {
            var t = UiText.Current;
            UpdateBannerText.Text = t.Localize("New update available") + " · " + available!.DisplayVersion;
            UpdateActionText.Text = t.Localize("Restart to update") + " · " + t.Localize("Please restart");
        }
    }

    private void Update_Click(object sender, RoutedEventArgs e)
    {
        if (_drag.JustDragged) return;
        if (_meter.Updates.Ready is { } ready && _meter.Updates.RestartPrepared(ready)) AppHost.Current.ExitProgram();
        else AppHost.Current.ShowUpdate();
    }

    private void UpdateBannerClose_Click(object sender, RoutedEventArgs e)
    {
        _bannerClosedFor = _meter.Updates.Available?.Tag;
        ShowUpdateBanner();
    }

    private void Language_Click(object sender, RoutedEventArgs e) => AppHost.Current.ToggleLanguage();

    /// <summary>Re-reads appearance settings changed in the Settings window.</summary>
    public void ApplyAppearance()
    {
        var desiredWidth = _settings.OverlayWidth;
        var desiredHeight = _settings.OverlayHeight;
        MinWidth = 260;
        Width = Math.Max(MinWidth, desiredWidth);
        Height = Math.Max(MinHeight, desiredHeight);
        WindowClickThrough.SetOpacity(_settings.WindowOpacity);
        OpacitySlider.Value = _settings.BackgroundOpacity;
        FrameBackground.Opacity = _settings.BackgroundOpacity;
        ApplyRowSize(_settings.RowSize);
        ApplyCompactLayout();
        UpdateModeLabel();
    }

    /// <summary>
    /// Row size in percent of the original. The height follows it exactly; text, emblems and the number columns shrink
    /// more gently (square root), so the smallest rows stay readable and the names get the room the numbers give up.
    /// </summary>
    public void ApplyRowSize(int percent)
    {
        var k = Math.Clamp(percent, AppSettings.MinRowSize, AppSettings.MaxRowSize) / 100.0;
        var f = Math.Sqrt(k);
        var height = Math.Max(34, Math.Round((_settings.CompactMode ? 38 : 44) * k));
        Resources["RowHeight"] = height;
        Resources["CompactRowHeight"] = height;
        Resources["CompactNameFont"] = Math.Max(12, Math.Round(13 * f, 1));
        Resources["CompactSubFont"] = Math.Max(11, Math.Round(11 * f, 1));
        Resources["CompactNumberFont"] = Math.Max(12, Math.Round(12.5 * f, 1));
        Resources["RowMargin"] = new Thickness(0, 0, 0, Math.Max(1, Math.Round(3 * k)));
        Resources["RowNameFont"] = Math.Round(13.5 * f, 1);
        Resources["RowGearFont"] = Math.Round(12 * f, 1);
        Resources["RowNumberFont"] = Math.Round(13.5 * f, 1);
        Resources["RowSubFont"] = Math.Round(12 * f, 1);
        Resources["RowGlyphSize"] = Math.Min(Math.Round(24 * f), height - 10);
        Resources["RowRankFont"] = Math.Round(11 * f, 1);
        Resources["RowAccentWidth"] = Math.Max(2, Math.Round(3 * f));
        Resources["ColRankWidth"] = new GridLength(Math.Round(20 * f));
        Resources["ColGlyphWidth"] = new GridLength(Math.Round(30 * f));
        Resources["ColGap"] = new Thickness(Math.Round(10 * f), 0, 0, 0);
    }

    private const int NameColumn = 2, GsColumn = 3, CpColumn = 4, SpacerColumn = 5; // in the row and label grids



    /// <summary>
    /// The name column is as wide as the longest name, which may take all the room the other columns leave but no more,
    /// so the fight's numbers always show (a long name is cut instead). A little is kept back for the rows' scrollbar.
    /// When even that leaves the names too little, the GS / CP columns step aside until the overlay is wide enough again.
    /// </summary>
    private void FitNames()
    {
        if (ColumnLabels.ActualWidth <= 0) return;
        var columns = ColumnLabels.ColumnDefinitions;
        double others = 0;
        for (var i = 0; i < columns.Count; i++)
            if (i is not (NameColumn or SpacerColumn)) others += columns[i].ActualWidth;
        var room = ColumnLabels.ActualWidth - others - 16; // 4 name margin + 12 for the scrollbar

        var max = Math.Max(30, Math.Floor(room));
        if (Resources["NameMaxWidth"] is double current && Math.Abs(current - max) < 1) return;
        Resources["NameMaxWidth"] = max;
    }

    /// <summary>Ctrl + mouse wheel over the rows: row size in 5 % steps, kept right away.</summary>
    private void Rows_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!NativeMethods.IsCtrlDown()) return;
        e.Handled = true;
        var size = Math.Clamp(_settings.RowSize + Math.Sign(e.Delta) * 5, AppSettings.MinRowSize, AppSettings.MaxRowSize);
        if (size == _settings.RowSize) return;
        _settings.RowSize = size;
        ApplyRowSize(size);
        _settings.Save();
    }

    public void SetClickThrough(bool on)
    {
        var previous = _settings.ClickThrough;
        _settings.ClickThrough = on;
        try { ApplyClickThrough(); }
        catch { _settings.ClickThrough = previous; throw; }
        _settings.Save();
    }

    private void ApplyClickThrough()
    {
        if (_hwnd == 0) return;
        WindowClickThrough.ApplyAll(_settings.ClickThrough);
        Frame.BorderBrush = _settings.ClickThrough ? (Brush)FindResource("Line") : (Brush)FindResource("CardFrame");
    }

    private void ApplyLock()
    {
        LockButton.IsChecked = _settings.Locked;
        LockGlyph.Text = _settings.Locked ? "" : "";
        ResizeGrip.Visibility = _settings.Locked ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UpdateModeLabel() =>
        _vm.ModeLabel = _settings.TargetMode == TargetMode.BossOnly ? "BOSS" : "ALL";

    private void EnsureOnScreen()
    {
        var vw = SystemParameters.VirtualScreenWidth;
        var vh = SystemParameters.VirtualScreenHeight;
        var vl = SystemParameters.VirtualScreenLeft;
        var vt = SystemParameters.VirtualScreenTop;
        if (Left + 60 > vl + vw || Left + Width - 60 < vl) Left = vl + 80;
        if (Top + 40 > vt + vh || Top < vt) Top = vt + 160;
    }

    /// <summary>
    /// Keeps the whole card inside the work area of the monitor it sits on (a taller layout must not hang off the
    /// bottom edge). Runs once the window has a handle, when the monitor and its DPI are known.
    /// </summary>
    private void FitToMonitor()
    {
        if (_hwnd == 0) return;
        var area = System.Windows.Forms.Screen.FromHandle(_hwnd).WorkingArea; // physical pixels
        var dpi = VisualTreeHelper.GetDpi(this);
        var left = area.Left / dpi.DpiScaleX;
        var top = area.Top / dpi.DpiScaleY;
        var right = area.Right / dpi.DpiScaleX;
        var bottom = area.Bottom / dpi.DpiScaleY;
        if (Height > bottom - top) Height = Math.Max(MinHeight, bottom - top);
        Left = Math.Clamp(Left, left, Math.Max(left, right - Width));
        Top = Math.Clamp(Top, top, Math.Max(top, bottom - Height));
        Log.Info($"Overlay placed at {Left:0},{Top:0} {Width:0}x{Height:0} DIP; monitor work area {left:0},{top:0}-{right:0},{bottom:0} DIP (scale {dpi.DpiScaleX})");
    }

    private void SavePlacement()
    {
        _settings.OverlayLeft = Left;
        _settings.OverlayTop = Top;
        _settings.OverlayWidth = Width;
        _settings.OverlayHeight = Height;
        _settings.Save();
    }

    // ------------------------------------------------------------ handlers
    private void PersonalBests_Click(object sender, RoutedEventArgs e) => AppHost.Current.ShowPersonalBests();
    private void LastPlayedWith_Click(object sender, RoutedEventArgs e) => AppHost.Current.ShowLastPlayedWith();

    private void Resize_DragDelta(object sender, DragDeltaEventArgs e)
    {
        Width = Math.Max(MinWidth, Width + e.HorizontalChange);
        Height = Math.Max(MinHeight, Height + e.VerticalChange);
    }

    private void Resize_DragCompleted(object sender, DragCompletedEventArgs e) => SavePlacement();

    private void OpenBreakdown(uint actorId)
    {
        if (_vm.SegmentId is { } seg) AppHost.Current.ShowBreakdown(seg, actorId);
    }

    private EncounterSnapshot? ShownSummary() =>
        _vm.SegmentId is { } seg ? _meter.Tracker.Snapshot(seg, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) : null;

    private void Row_Click(object sender, MouseButtonEventArgs e)
    {
        if (_drag.JustDragged) return; // the press moved the window, it was not a click
        if ((sender as FrameworkElement)?.DataContext is RowViewModel row) OpenBreakdown(row.ActorId);
    }

    private void Row_RightClick(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not RowViewModel row) return;
        var menu = new ContextMenu { PlacementTarget = (UIElement)sender };
        var open = new MenuItem { Header = UiText.Current.OpenBreakdown };
        open.Click += (_, _) => OpenBreakdown(row.ActorId);
        menu.Items.Add(open);
        menu.Items.Add(new Separator());
        var player = row.ActorId == Combatant.UnknownSummonsId ? null : (uint?)row.ActorId;
        foreach (var item in ChatCopy.Items(CardBody, ShownSummary, player, row.Name)) menu.Items.Add(item);
        menu.IsOpen = true;
        e.Handled = true;
    }

    /// <summary>The toolbar's chat button: your result (when you took part), the party ranking or the Discord table.</summary>
    private void Chat_Click(object sender, RoutedEventArgs e)
    {
        if (ShownSummary() is null) return;
        var self = _vm.Rows.FirstOrDefault(r => r.IsSelf);
        ChatCopy.ShowMenu((FrameworkElement)sender, CardBody, ShownSummary, self?.ActorId, self?.Name);
    }

    private void Details_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.Rows.Count == 0) return;
        var target = _vm.Rows.FirstOrDefault(r => r.IsSelf) ?? _vm.Rows[0];
        OpenBreakdown(target.ActorId);
    }

    private void Mode_Click(object sender, RoutedEventArgs e)
    {
        _settings.TargetMode = _settings.TargetMode == TargetMode.BossOnly ? TargetMode.All : TargetMode.BossOnly;
        _meter.ApplySettings();
        UpdateModeLabel();
    }

    // ------------------------------------------------------------ fight picker

    /// <summary>One selectable fight: a segment of this session or a fight saved on disk.</summary>
    private sealed record PickItem(
        string Title, DateTimeOffset Start, long CombatMs, EncounterEndReason Reason, bool Active,
        double SelfDps, int Place, double PartyDps, Guid? Segment, HistoryEntry? Saved, ImageSource? Portrait = null);

    private const int SavedInMenu = 40;

    /// <summary>Newest first: this session's segments, then saved fights not already in the session list.</summary>
    private List<PickItem> BuildFightList(int maxSaved)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var list = new List<PickItem>();
        var sessionStarts = new HashSet<long>();
        foreach (var s in _meter.Tracker.Segments())
        {
            var snap = _meter.Tracker.Snapshot(s.Id, now);
            var selfIndex = snap?.Combatants.ToList().FindIndex(c => c.IsSelf) ?? -1;
            var self = selfIndex >= 0 ? snap!.Combatants[selfIndex] : null;
            list.Add(new PickItem(s.Title, s.StartedAt, s.CombatMs, s.Reason, s.IsActive,
                self?.Dps ?? 0, selfIndex + 1, s.TotalDamage / Math.Max(1.0, s.CombatMs / 1000.0), s.Id, null,
                snap is null ? null : _meter.PortraitOf(snap)));
            sessionStarts.Add(s.StartedAt.ToUnixTimeSeconds());
        }
        foreach (var h in _meter.History.List(maxSaved + sessionStarts.Count))
        {
            if (sessionStarts.Contains(h.StartedAt.ToUnixTimeSeconds())) continue;
            list.Add(new PickItem(_meter.DisplayTitle(h), h.StartedAt, h.CombatMs, h.Reason, false,
                h.SelfDps, h.SelfPlace, h.PartyDps, null, h, _meter.PortraitOf(h)));
            if (list.Count(i => i.Saved is not null) >= maxSaved) break;
        }
        return list;
    }

    private void OpenStatistics(PickItem item)
    {
        IFightView view;
        object key;
        if (item.Saved is { } h)
        {
            if (_meter.History.Load(h.Path) is not { } record)
            {
                AppHost.Current.ShowHistory(); // file vanished or is unreadable: let the history window refresh
                return;
            }
            view=new RecordFightView(record);
            key=h.Path;
        }
        else if(item.Segment is { } segment)
        {
            view=new LiveFightView(_meter.Tracker,segment);
            key=segment;
        }
        else return;
        var summary=view.Summary();
        var player=summary?.Combatants.FirstOrDefault(c=>c.IsSelf) ?? summary?.Combatants.FirstOrDefault();
        if(player is not null) AppHost.Current.ShowBreakdown(key,view,player.ActorId,_meter);
        Refresh();
    }

    private void Segments_Click(object sender, RoutedEventArgs e)
    {
        CreateFightMenu((UIElement)sender).IsOpen = true;
    }

    internal ContextMenu CreateFightMenu(UIElement anchor)
    {
        var menu = new ContextMenu { PlacementTarget = anchor, Placement = PlacementMode.Bottom, MinWidth = 260, MaxWidth = 340 };
        menu.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/CatScratch;component/Theme/FightMenu.xaml", UriKind.Relative) });
        var live = new MenuItem
        {
            Header = PickHeader(UiText.Current.Live, UiText.Current.LiveHint, null, 0, EncounterEndReason.None, active: true, partyDps: 0, portrait: null),
            IsCheckable = true,
            IsChecked = true,
        };
        live.Click += (_, _) => Refresh();
        menu.Items.Add(live);

        var items = BuildFightList(SavedInMenu);
        string? section = null;
        foreach (var item in items)
        {
            var group = item.Saved is null ? UiText.Current.SessionSection : DayLabel(item.Start);
            if (group != section)
            {
                section = group;
                menu.Items.Add(new Separator());
                menu.Items.Add(new MenuItem
                {
                    Header = new TextBlock { Text = group, Style = (Style)menu.FindResource("SectionLabel") },
                    IsEnabled = false,
                });
            }
            var meta = $"{item.Start:HH:mm} · {Format.Clock(item.CombatMs)}";
            var row = new MenuItem
            {
                Header = PickHeader(item.Title, meta, item.SelfDps > 0 ? item.SelfDps : null, item.Place, item.Reason, item.Active, item.PartyDps, item.Portrait),
                IsCheckable = true,
                IsChecked = false,
            };
            var pick = item;
            row.Click += (_, _) => OpenStatistics(pick);
            menu.Items.Add(row);
        }
        if (items.Count == 0)
            menu.Items.Add(new MenuItem { Header = new TextBlock { Text = UiText.Current.NoFights, TextWrapping = TextWrapping.Wrap, MaxWidth = 245, FontSize = 11 }, IsEnabled = false });

        menu.Items.Add(new Separator());
        var total = _meter.History.List().Count;
        var history = new MenuItem { Header = string.Format(UiText.Current.AllSaved, total) };
        history.Click += (_, _) => AppHost.Current.ShowHistory();
        menu.Items.Add(history);
        return menu;
    }

    private static string DayLabel(DateTimeOffset t)
    {
        var day = t.LocalDateTime.Date;
        if (day == DateTime.Today) return UiText.Current.SavedToday;
        if (day == DateTime.Today.AddDays(-1)) return UiText.Current.SavedYesterday;
        return string.Format(UiText.Current.SavedOn, day.ToString("dd MMM yyyy", UiText.Current.Culture).ToUpperInvariant());
    }

    /// <summary>Two-line menu row: boss portrait, result mark, title, time · length, and your DPS with your place badge.</summary>
    private FrameworkElement PickHeader(string title, string meta, double? selfDps, int place, EncounterEndReason reason, bool active, double partyDps,
        ImageSource? portrait)
    {
        var grid = new Grid { MinWidth = 230 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(27) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(13) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        grid.Children.Add(new BossPortrait { Source = portrait, Width = 21, Height = 21, Accent = ThemeColors.Brush("#C49AF5"), HorizontalAlignment = HorizontalAlignment.Left });

        var (mark, markBrush) = active ? ("●", "Red") : reason switch
        {
            EncounterEndReason.Kill => ("✔", "Green"),
            EncounterEndReason.Wipe => ("✖", "Red"),
            _ => ("•", "TextMute"),
        };
        var markText = new TextBlock { Text = mark, Foreground = (Brush)FindResource(markBrush), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(markText, 1);
        grid.Children.Add(markText);

        var text = new StackPanel { Margin = new Thickness(0, 0, 8, 0), HorizontalAlignment = HorizontalAlignment.Left };
        text.Children.Add(new TextBlock
        {
            Text = title, FontWeight = FontWeights.SemiBold, FontSize = 12, MaxWidth = 160,
            TextTrimming = TextTrimming.CharacterEllipsis, Foreground = ThemeColors.Brush("#EDF2F7"),
        });
        text.Children.Add(new TextBlock
        {
            Text = meta, FontSize = 10.5, FontFamily = (FontFamily)FindResource("NumberFont"), Foreground = ThemeColors.Brush("#A9B8C7"),
        });
        Grid.SetColumn(text, 2);
        grid.Children.Add(text);

        var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        if (selfDps is { } dps)
        {
            right.Children.Add(new TextBlock
            {
                Text = Format.Compact(dps) + "/s", FontFamily = (FontFamily)FindResource("NumberFont"), FontSize = 12.5,
                FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("GoldBright"), VerticalAlignment = VerticalAlignment.Center,
            });
            if (place > 0)
            {
                var (bg, fg, border) = RankBrushes.For(place);
                right.Children.Add(new Border
                {
                    Width = 20, Height = 20, Margin = new Thickness(7, 0, 0, 0), CornerRadius = new CornerRadius(5),
                    Background = bg, BorderBrush = border, BorderThickness = new Thickness(1),
                    Child = new TextBlock
                    {
                        Text = place.ToString(), FontFamily = (FontFamily)FindResource("NumberFont"), FontSize = 11,
                        FontWeight = FontWeights.Bold, Foreground = fg,
                        HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                    },
                });
            }
        }
        else if (partyDps > 0)
        {
            right.Children.Add(new TextBlock
            {
                Text = UiText.Current.Party + " " + Format.Compact(partyDps) + "/s", FontFamily = (FontFamily)FindResource("NumberFont"),
                FontSize = 11.5, Foreground = (Brush)FindResource("TextDim"), VerticalAlignment = VerticalAlignment.Center,
            });
        }
        Grid.SetColumn(right, 3);
        grid.Children.Add(right);
        return grid;
    }

    /// <summary>Browse the history menu without changing the live meter.</summary>
    private void Title_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        CreateFightMenu((UIElement)sender).IsOpen=true;
        e.Handled = true;
    }

    /// <summary>Open saved statistics while the meter continues following live combat.</summary>
    public void OpenSavedStatistics(HistoryEntry entry)
    {
        OpenStatistics(new PickItem(entry.Title, entry.StartedAt, entry.CombatMs, entry.Reason, false,
            entry.SelfDps, entry.SelfPlace, entry.PartyDps, null, entry));
    }

    private void Timers_Click(object sender, RoutedEventArgs e)
    {
        if (!_drag.JustDragged) AppHost.Current.ShowBossTimers();
    }

    private long _timersLabelAt;

    /// <summary>The next respawn among watched bosses (or all, when none is watched): "Gartua 12:34".</summary>
    private void UpdateTimersLabel()
    {
        _timersLabelAt = Environment.TickCount64;
        var now = DateTimeOffset.Now;
        var timers = _meter.Timers.ForServer(_meter.Timers.CurrentServer).Where(t => t.NpcCode > 0).ToList();
        var pool = timers.Any(t => t.Watch) ? timers.Where(t => t.Watch) : timers;
        var next = pool.Where(t => !t.AliveNow && t.NextSpawn > now).MinBy(t => t.NextSpawn);
        if (next?.NextSpawn is not { } at)
        {
            _vm.TimersLabel = UiText.Current.TimersButton;
            return;
        }
        var name = next.NpcCode > 0 ? _meter.Data.NpcName(next.NpcCode) : string.Format(UiText.Current.UnknownSlotBoss, -next.NpcCode % 100);
        var shortName = name.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? name;
        var left = at - now;
        _vm.TimersLabel = $"{shortName} {(left.TotalHours >= 1 ? left.ToString(@"h\:mm\:ss") : left.ToString(@"mm\:ss"))}";
    }

    private void Restart_Click(object sender, RoutedEventArgs e) => AppHost.Current.RestartMeter();

    /// <summary>Back to the live view (after a restart of the meter: empty until the next hit).</summary>
    public void ShowLive()
    {
        Refresh();
    }

    /// <summary>Tooltips that name a hotkey the user can change.</summary>
    public void ApplyHotkeyTips() =>
        RestartButton.ToolTip = string.Format(UiText.Current.TipRestart, _settings.HotkeyReset);

    private void Lock_Click(object sender, RoutedEventArgs e)
    {
        _settings.Locked = LockButton.IsChecked == true;
        _settings.Save();
        ApplyLock();
    }

    private void ClickMode_Click(object sender, RoutedEventArgs e) => AppHost.Current.ToggleClickThrough();

    private void Settings_Click(object sender, RoutedEventArgs e) => AppHost.Current.ShowSettings();

    private void Hide_Click(object sender, RoutedEventArgs e) => Close();
    private void PatchNotes_Click(object sender, RoutedEventArgs e) => AppHost.Current.ShowPatchNotes();

    private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        CheckUpdatesButton.IsEnabled = false;
        CheckUpdatesButton.ToolTip = UiText.Current.Localize("Checking for updates…");
        try { await AppHost.Current.CheckUpdatesFromTrayAsync(); }
        finally
        {
            CheckUpdatesButton.IsEnabled = true;
            CheckUpdatesButton.ToolTip = UiText.Current.Localize("Check for updates");
        }
    }
    private void NpcapDownload_Click(object sender, RoutedEventArgs e) => App.ShowNpcapHelp();

    private void ApplyCompactLayout()
    {
        TitleText.FontSize = _settings.CompactMode ? 15 : 17;
        HeaderClock.Visibility = _settings.CompactMode ? Visibility.Collapsed : Visibility.Visible;
        CompactTotal.Visibility = _settings.CompactMode ? Visibility.Visible : Visibility.Collapsed;
        CompactHistory.Visibility = CompactTotal.Visibility;
        if (_settings.CompactMode)
        {
            Toolbar.Visibility = Visibility.Collapsed;
            TitleText.Visibility = Visibility.Visible;
            LabelPanel.Visibility = _settings.CompactMode ? Visibility.Collapsed : Visibility.Visible;
        }
        PlayerRows.ItemTemplate = (DataTemplate)FindResource("CompactRowTemplate");
        var visible = _settings.CompactMode ? Visibility.Collapsed : Visibility.Visible;
        BossTimersLink.Visibility = visible;
        DetailsLink.Visibility = visible;
        HistoryLink.Visibility = visible;
        FooterText.Visibility = visible;
        _narrowLayoutWidth = double.NaN;
        ApplyNarrowLayout();
    }

    private double _narrowLayoutWidth = double.NaN;
    private void ApplyNarrowLayout()
    {
        if (FooterControls is null) return;
        if (_narrowLayoutWidth == Width) return;
        _narrowLayoutWidth = Width;
        var narrow = Width < 360;
        ShellPaw.Visibility = Width < 300 ? Visibility.Collapsed : Visibility.Visible;
        ShellTitle.FontSize = Width < 320 ? 12 : 16;
        Resources["DpsColumnWidth"] = new GridLength(Width < 320 ? 64 : 76);
        Resources["ShareColumnWidth"] = new GridLength(Width < 320 ? 44 : 57);
        PersonalName.MaxWidth = Math.Clamp(Width - 220,40,120);
        Grid.SetRow(FooterControls,1);
        Grid.SetColumn(FooterControls,0);
        Grid.SetColumnSpan(FooterControls,2);
        ShellClockPanel.Visibility=Width<360?Visibility.Collapsed:Visibility.Visible;
        FooterControls.HorizontalAlignment = HorizontalAlignment.Left;
        FooterControls.Margin = new Thickness(0,narrow ? 3 : 0,10,0);
        if (narrow)
        {
            BossTimersLink.Visibility = Visibility.Collapsed;
            DetailsLink.Visibility = Visibility.Collapsed;
            HistoryLink.Visibility = Visibility.Collapsed;
            FooterText.Visibility = Visibility.Collapsed;
            CompactTotal.Visibility = Visibility.Visible;
            CompactHistory.Visibility = Visibility.Visible;
        }
        else
        {
            var visibility = _settings.CompactMode ? Visibility.Collapsed : Visibility.Visible;
            BossTimersLink.Visibility = FooterText.Visibility = visibility;
            DetailsLink.Visibility = HistoryLink.Visibility = !_settings.CompactMode && Width >= 560 ? Visibility.Visible : Visibility.Collapsed;
            CompactTotal.Visibility = CompactHistory.Visibility = _settings.CompactMode ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void Appearance_Click(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu { PlacementTarget = (UIElement)sender, Placement = PlacementMode.Top, MinWidth = 250 };
        void Item(string label, Action action)
        {
            var item = new MenuItem { Header = label }; item.Click += (_, _) => action(); menu.Items.Add(item);
        }
        void Preset(bool compact, double width, double height)
        {
            _settings.CompactMode = compact;
            _settings.OverlayWidth = width; _settings.OverlayHeight = height;
            ApplyAppearance(); _settings.Save(); Refresh();
        }
        var toggle = new MenuItem { Header = UiText.Current.Localize("Compact mode"), IsCheckable = true, IsChecked = _settings.CompactMode };
        toggle.Click += (_, _) => Preset(toggle.IsChecked, toggle.IsChecked ? 420 : 760, toggle.IsChecked ? 540 : 610);
        menu.Items.Add(toggle);
        Item("Mini · 320 × 360", () => Preset(true, 320, 360));
        Item("Compact · 420 × 540", () => Preset(true, 420, 540));
        Item("Medium · 480 × 610", () => Preset(true, 480, 610));
        Item("Large · 760 × 680", () => Preset(false, 760, 680));
        menu.Items.Add(new Separator());
        void SliderItem(string label, double min, double max, double value, Action<double> change)
        {
            var panel = new StackPanel { Margin = new Thickness(8), Width = 220 };
            var caption = new TextBlock { Text = label };
            var slider = new Slider { Minimum = min, Maximum = max, Value = value, Margin = new Thickness(0, 8, 0, 0) };
            slider.ValueChanged += (_, args) => { change(args.NewValue); caption.Text = $"{label}: {args.NewValue:0}"; };
            panel.Children.Add(caption); panel.Children.Add(slider);
            menu.Items.Add(new MenuItem { Header = panel, StaysOpenOnClick = true });
        }
        SliderItem("Width", MinWidth, 1600, Width, value => { Width = Math.Round(value); SavePlacement(); });
        SliderItem("Height", MinHeight, 1200, Height, value => { Height = Math.Round(value); SavePlacement(); });
        SliderItem("Opacity (%)", 40, 100, _settings.WindowOpacity * 100, value => { _settings.WindowOpacity = value / 100; WindowClickThrough.SetOpacity(_settings.WindowOpacity); _settings.Save(); });
        Item("More settings", () => AppHost.Current.ShowSettings());
        menu.IsOpen = true;
    }

    private void Opacity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FrameBackground is null) return;
        FrameBackground.Opacity = e.NewValue;
        _settings.BackgroundOpacity = Math.Round(e.NewValue, 2);
    }

    protected override void OnClosed(EventArgs e)
    {
        SavePlacement();
        base.OnClosed(e);
    }
}
