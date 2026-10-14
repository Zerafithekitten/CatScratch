using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AionMeter.App.Services;
using AionMeter.Core.Capture;
using AionMeter.Core.Combat;

namespace AionMeter.App.Windows;

public partial class SettingsWindow : Window
{
    private void PersonalBests_Click(object sender, RoutedEventArgs e) => AppHost.Current.ShowPersonalBests();
    private void DeleteSavedFights_Click(object sender, RoutedEventArgs e)
    {
        var count = _meter.History.List().Count;
        if (count == 0) { MessageBox.Show(this, T.Localize("No saved fights to delete."), "CatScratch"); return; }
        if (MessageBox.Show(this, string.Format(T.Localize("Delete all {0} saved fights? This cannot be undone."),count), "CatScratch",
            MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        try
        {
            _meter.DeleteSavedFights();
            HistoryInfo.Text = UiText.Current.Localize("No saved fights.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, string.Format(T.Localize("Could not delete saved fights: {0}"),ex.Message), "CatScratch", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void SaveRecentDebug_Click(object sender, RoutedEventArgs e)
    {
        SaveRecentDebug.IsEnabled = false;
        _meter.SetRecentDebug(RecentDebug.IsChecked == true);
        try { RecentDebugResult.Text = await _meter.SaveRecentDebugAsync(); }
        catch (Exception ex) { RecentDebugResult.Text = T.Localize(ex.Message); }
        finally { SaveRecentDebug.IsEnabled = true; }
    }

    private void PatchNotes_Click(object sender, RoutedEventArgs e) => AppHost.Current.ShowPatchNotes();
    private void Report_Click(object sender, RoutedEventArgs e)
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Feedback.IssueUrl) { UseShellExecute = true }); }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        { MessageBox.Show(this, T.Localize("Could not open the browser. Please visit GitHub and choose New issue."), "CatScratch"); }
    }
    private readonly MeterService _meter;
    private readonly List<(string? Name, string Label)> _devices = new();

    public SettingsWindow(MeterService meter)
    {
        _meter = meter;
        InitializeComponent();
        // Keep explanatory text wrapped, while allowing horizontal scrolling at larger UI scales.
        foreach (var tab in SettingsTabs.Items.OfType<TabItem>())
            if (tab.Content is ScrollViewer viewer && viewer.Content is FrameworkElement content)
            {
                void FitContent() => content.Width = Math.Max(540, viewer.ActualWidth - viewer.Padding.Left - viewer.Padding.Right - 18);
                viewer.Loaded += (_, _) => FitContent();
                viewer.SizeChanged += (_, _) => FitContent();
            }
        AppIcon.Apply(this);
        Controls.DragAnywhere.Attach(this);
        Load();
        foreach (var field in new[] { HkToggle, HkAll, HkReset, HkClick, HkTimers, HkPersonal, HkEvents, HkPets })
        {
            field.IsReadOnly = true;
            field.ToolTip = UiText.Current.Localize("Click here and press your shortcut. Applied after saving.");
            field.PreviewKeyDown += RecordHotkey;
            field.GotKeyboardFocus += (_, _) => field.SelectAll();
        }
        Activated += (_, _) => AppHost.Current.SuspendHotkeys();
        Deactivated += (_, _) => AppHost.Current.ResumeHotkeys();
        Closed += (_, _) => AppHost.Current.ResumeHotkeys();
        // The row size slider previews on the overlay; closing without saving puts the saved size back.
        Closed += (_, _) => AppHost.Current.PreviewRowSize(_meter.Settings.RowSize);
    }

    private void Load()
    {
        var s = _meter.Settings;
        CompactModeCheck.IsChecked = s.CompactMode;
        UiScaleLabel.Text=T.Localize("UI scale"); UiScaleValue.Value=Math.Clamp(s.UiScale,.75,1.5)*100;
        StreamModeCheck.Content=T.Localize("Stream mode: hide player names");StreamModeCheck.IsChecked=s.StreamMode;
        StreamModeHint.Text=T.Localize("Only displayed and copied player names are hidden. Local recordings and saved files keep their original data.");
        AutomaticFightEndCheck.Content=T.Localize("Automatic fight end");AutomaticFightEndCheck.IsChecked=s.AutomaticFightEnd;
        AutomaticFightEndHint.Text=T.Localize("After inactivity, keep the finished fight until the next fight. Healing and incoming damage count as activity.");
        TestModeSection.Text=T.Localize("Test mode");
        TestModeButton.Content=T.Localize(_meter.DemoRunning?"Stop test mode":"Start test mode");
        TestModeHint.Text=T.Localize("Use sample fights and pet progress to arrange your modules. Test data is not saved.");
        WindowWidthText.Text = s.OverlayWidth.ToString("0");
        WindowHeightText.Text = s.OverlayHeight.ToString("0");
        WindowOpacityValue.Value = s.WindowOpacity;
        BarsRelative.IsChecked = s.BarsRelativeToTop;
        ShowBoss.IsChecked = s.ShowBossPanel;
        ShowPersonalCombatModule.IsChecked = s.ShowPersonalCombatModule;
        ShowHealingDone.IsChecked = s.ShowHealingDone;
        ShowGear.IsChecked = s.ShowGear;
        ShowCombatPower.IsChecked = s.ShowCombatPower ?? s.ShowGear;
        AutoShow.IsChecked = s.AutoShow;
        AutoHideSeconds.Text = s.AutoHideSeconds.ToString();
        ShowOnStart.IsChecked = s.ShowOnStart;
        foreach (var n in new[] { 5, 8, 10, 12, 15, 20 }) MaxRows.Items.Add(n);
        MaxRows.SelectedItem = MaxRows.Items.Cast<int>().OrderBy(n => Math.Abs(n - s.MaxRows)).First();
        OpacityValue.Value = s.BackgroundOpacity;
        RowSizeValue.Value = s.RowSize;
        Mode.SelectedIndex = s.TargetMode == TargetMode.BossOnly ? 0 : 1;
        Idle.Text = s.IdleTimeoutSec.ToString();
        BossIdle.Text = s.BossIdleTimeoutSec.ToString();
        SaveHistory.IsChecked = s.SaveHistory;
        HistoryModeBox.SelectedIndex = (int)s.HistoryMode;
        HistoryMin.Text = s.HistoryMinSeconds.ToString();
        HistoryInfo.Text = string.Format(T.HistoryInfo, _meter.History.List().Count, _meter.History.Directory);
        Record.IsChecked = s.RecordPackets;
        RecentDebug.IsChecked = s.RecentDebugEnabled;
        RecentDebug.Content = T.Localize("Recent debug buffer (60 seconds / 32 MB, experimental)");
        SaveRecentDebug.Content = T.Localize("Save recent debug");
        RecentDebugResult.Text = T.Localize("Enable and save settings first. High traffic can shorten the recorded minute.");
        HkToggle.Text = s.HotkeyToggleOverlay;
        HkAll.Text = s.HotkeyToggleAll;
        HkReset.Text = s.HotkeyReset;
        HkClick.Text = s.HotkeyClickThrough;
        HkTimers.Text = s.HotkeyTimers;
        HkPersonal.Text=s.HotkeyPersonal;
        HkEvents.Text=s.HotkeyEvents;
        HkPets.Text=s.HotkeyPets;

        _devices.Add((null, T.AdapterAuto));
        try
        {
            foreach (var (name, description) in LiveCapture.ListDevices()) _devices.Add((name, description));
        }
        catch (Exception ex) when (ex is DllNotFoundException or TypeInitializationException)
        {
            _devices.Add((null, T.NpcapMissing));
        }
        foreach (var d in _devices) Device.Items.Add(d.Label);
        var idx = _devices.FindIndex(d => d.Name == s.CaptureDevice);
        Device.SelectedIndex = Math.Max(0, idx);

        LanguageBox.SelectedIndex = Array.IndexOf(UiText.Languages, UiText.Normalize(s.Language));
        Icons.IsChecked = s.DownloadIcons;
        CheckUpdates.IsChecked = s.CheckUpdates;
        UpdateChannelLabel.Text = T.Localize("Update channel");
        UpdateChannelBox.Items.Add(T.Localize("Stable · tested releases"));
        UpdateChannelBox.Items.Add(T.Localize("Beta · early access"));
        UpdateChannelBox.SelectedIndex = _meter.Updates.Channel == AionMeter.Core.Updates.UpdateChannel.Beta ? 1 : 0;
        UpdateChannelHint.Text = T.Localize("Beta releases may contain unfinished features. Changing the channel never installs an update. Returning to Stable can install an older version; your settings and saved fights are kept.");


        VersionText.Text = string.Format(Updater.IsInstalled ? T.VersionInstalled : T.VersionPortable, _meter.Updates.CurrentDisplay);

        CaptureState.Text = T.StatusPrefix + T.CaptureMessage(_meter.CaptureStatus.Message);
        var data = _meter.Data;
        DataInfo.Text = string.Format(T.DataInfo, data.Skills.Count, data.Npcs.Count, data.Npcs.Values.Count(n => n.IsBoss),
            data.DotSkills.Count, data.Maps.Count);
    }

    private static UiText T => UiText.Current;

    private void RecordHotkey(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        var modifiers = Keyboard.Modifiers;
        if (key == Key.Tab && modifiers == ModifierKeys.None) return;
        e.Handled = true;
        if (e.IsRepeat || key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift
            or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin or Key.None) return;
        if (key == Key.Escape && modifiers == ModifierKeys.None)
        {
            Keyboard.ClearFocus();
            return;
        }
        var parts = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        parts.Add(key >= Key.D0 && key <= Key.D9 ? ((int)key - (int)Key.D0).ToString() : key.ToString());
        var gesture = string.Join("+", parts);
        if (!HotkeyManager.TryParse(gesture, out _, out _)) return;
        var field = (TextBox)sender;
        field.Text = gesture;
        field.SelectAll();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var s = _meter.Settings;
        s.CompactMode = CompactModeCheck.IsChecked == true;
        if (int.TryParse(WindowWidthText.Text, out var width)) s.OverlayWidth = Math.Clamp(width, 260, 1600);
        if (int.TryParse(WindowHeightText.Text, out var height)) s.OverlayHeight = Math.Clamp(height, 180, 1200);
        s.WindowOpacity = WindowOpacityValue.Value;
        var captureChanged = false;

        s.BarsRelativeToTop = BarsRelative.IsChecked == true;
        s.ShowBossPanel = ShowBoss.IsChecked == true;
        s.UiScale=UiScaleValue.Value/100; s.StreamMode=StreamModeCheck.IsChecked==true;
        s.AutomaticFightEnd=AutomaticFightEndCheck.IsChecked==true;
        s.ShowPersonalCombatModule = ShowPersonalCombatModule.IsChecked == true;
        s.ShowHealingDone = ShowHealingDone.IsChecked == true;
        s.ShowGear = ShowGear.IsChecked == true;
        s.ShowCombatPower = ShowCombatPower.IsChecked == true;
        s.AutoShow = AutoShow.IsChecked == true;
        if (int.TryParse(AutoHideSeconds.Text, out var autoHide)) s.AutoHideSeconds = Math.Clamp(autoHide, 0, 3600);
        s.ShowOnStart = ShowOnStart.IsChecked == true;
        s.MaxRows = MaxRows.SelectedItem is int rows ? rows : 10;
        s.BackgroundOpacity = Math.Round(OpacityValue.Value, 2);
        s.RowSize = (int)Math.Round(RowSizeValue.Value);
        s.TargetMode = Mode.SelectedIndex == 1 ? TargetMode.All : TargetMode.BossOnly;
        if (int.TryParse(Idle.Text, out var idle)) s.IdleTimeoutSec = Math.Clamp(idle, 3, 120);
        if (int.TryParse(BossIdle.Text, out var bossIdle)) s.BossIdleTimeoutSec = Math.Clamp(bossIdle, 5, 300);
        s.SaveHistory = SaveHistory.IsChecked == true;
        s.HistoryMode = (HistoryMode)Math.Max(0, HistoryModeBox.SelectedIndex);
        if (int.TryParse(HistoryMin.Text, out var minSec)) s.HistoryMinSeconds = Math.Clamp(minSec, 5, 600);

        var device = _devices[Math.Max(0, Device.SelectedIndex)].Name;
        if (device != s.CaptureDevice || (Record.IsChecked == true) != s.RecordPackets) captureChanged = true;
        s.CaptureDevice = device;
        s.RecordPackets = Record.IsChecked == true;
        s.RecentDebugEnabled = RecentDebug.IsChecked == true;
        _meter.SetRecentDebug(s.RecentDebugEnabled);

        var language = (LanguageBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "en";
        s.DownloadIcons = Icons.IsChecked == true;
        s.CheckUpdates = CheckUpdates.IsChecked == true;
        SaveUpdateChannel();
        s.AutoInstallUpdates = false; // Legacy setting: installation now always requires explicit approval.
        s.HotkeyToggleOverlay = HkToggle.Text.Trim();
        s.HotkeyToggleAll = HkAll.Text.Trim();
        s.HotkeyReset = HkReset.Text.Trim();
        s.HotkeyClickThrough = HkClick.Text.Trim();
        s.HotkeyTimers = HkTimers.Text.Trim();
        s.HotkeyPersonal=HkPersonal.Text.Trim();
        s.HotkeyEvents=HkEvents.Text.Trim();
        s.HotkeyPets=HkPets.Text.Trim();

        _meter.ApplySettings();
        if (captureChanged) _meter.RestartCapture();
        Close();
        AppHost.Current.OnSettingsChanged();
        AppHost.Current.RefreshPresentation();
        if (language != UiText.Normalize(s.Language)) AppHost.Current.SetLanguage(language);
    }

    private void UiScale_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if(UiScaleText is not null)UiScaleText.Text=$"{e.NewValue:0}%";
    }
    private void TestMode_Click(object sender, RoutedEventArgs e)
    {
        AppHost.Current.ToggleTestMode();
        TestModeSection.Text=T.Localize("Test mode");
        TestModeButton.Content=T.Localize(_meter.DemoRunning?"Stop test mode":"Start test mode");
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    /// <summary>The overlay follows the slider at once, so the size can be judged on the real rows.</summary>
    private void RowSize_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RowSizeText is null) return; // the slider sets its first value before the rest of the window exists
        var percent = (int)Math.Round(e.NewValue);
        RowSizeText.Text = $"{percent}%";
        AppHost.Current.PreviewRowSize(percent);
    }

    /// <summary>Asks GitHub right away; a newer version also opens the update window.</summary>
    private async void CheckNow_Click(object sender, RoutedEventArgs e)
    {
        SaveUpdateChannel();
        _meter.Settings.Save();
        CheckNowButton.IsEnabled = false;
        UpdateState.Visibility = Visibility.Visible;
        UpdateState.Text = T.UpdateChecking;
        var result = await AppHost.Current.CheckUpdatesNowAsync();
        UpdateState.Text = result switch
        {
            UpdateCheckResult.Available => string.Format(T.UpdateFound, _meter.Updates.Latest!.DisplayVersion),
            UpdateCheckResult.UpToDate => T.UpToDate,
            _ => string.Format(T.UpdateCheckFailed, _meter.Updates.LastError),
        };
        CheckNowButton.IsEnabled = true;
    }

    private void SaveUpdateChannel() => _meter.Updates.SetChannel(UpdateChannelBox.SelectedIndex == 1
        ? AionMeter.Core.Updates.UpdateChannel.Beta : AionMeter.Core.Updates.UpdateChannel.Stable);

    private static void Open(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });
    }

    private void OpenData_Click(object sender, RoutedEventArgs e) => Open(Path.Combine(AppContext.BaseDirectory, "data"));

    private void OpenAppData_Click(object sender, RoutedEventArgs e) => Open(AppSettings.AppDataDir);

    private void OpenLogs_Click(object sender, RoutedEventArgs e) => Open(Log.Directory);
}
