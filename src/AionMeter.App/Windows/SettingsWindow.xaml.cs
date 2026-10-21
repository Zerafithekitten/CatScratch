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
    private readonly ThemeSettingsPanel _themePanel;
    private readonly ControlPanelSettingsPanel _controlPanelPreferences;
    private readonly ChecklistSettingsPanel _checklistPreferences;
    private readonly MeterService _meter;
    private bool _bossTimingDetachedAtOpen;
    private readonly TimerCards _cards;
    private readonly TimerCards _eventDraft;
    private readonly AppSettings _moduleDraft;
    private readonly List<(string? Name, string Label)> _devices = new();

    public SettingsWindow(MeterService meter,TimerCards? cards=null)
    {
        _meter = meter;
        InitializeComponent();
        BrandIcon.Source=CatBranding.Mascot;
        _themePanel=new ThemeSettingsPanel(meter.Settings.Theme);
        if(Tab("appearance").Content is ScrollViewer {Content:Panel appearance})
            appearance.Children.Insert(1,new Border{Child=_themePanel,Style=(Style)FindResource("Section")});
        foreach(var tab in SettingsTabs.Items.OfType<TabItem>().Where(t=>t.Tag is "updates" or "feedback"))
            tab.Header=T.Localize((string)tab.Header);
        cards??=new TimerCards(meter,persistent:false);
        _cards=cards;
        _eventDraft=cards.CreatePreferencesDraft();
        _moduleDraft=meter.Settings.CreateDraft();
        _controlPanelPreferences=new ControlPanelSettingsPanel(_moduleDraft);
        if(Tab("appearance").Content is ScrollViewer {Content:Panel appearancePanel})
        {
            appearancePanel.Children.Insert(1,new Border {Child=_controlPanelPreferences,Style=(Style)FindResource("Section")});
            appearancePanel.Children.Insert(1,new Border {Child=new DisplaySettingsPanel(_moduleDraft),Style=(Style)FindResource("Section")});
        }
        SettingsTabs.Items.Add(new TabItem {Header=T.Localize("Events"),Tag="events",Padding=new Thickness(12,10,12,10),Content=new ScrollViewer {VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Padding=new Thickness(14,0,0,0),Content=new EventsSettingsPanel(_eventDraft,()=>{},(sound,volume)=>{if(!meter.Settings.Transient)AppHost.Current.PreviewEventSound(sound,volume);})}});
        var modules=new StackPanel();
        modules.Children.Add(new TextBlock {Text=T.Localize("Pet tracker"),FontSize=16,Margin=new Thickness(0,0,0,8)});
        modules.Children.Add(new TextBlock {Text=T.Localize("Live preview · Save keeps changes; closing discards unsaved changes."),Margin=new Thickness(0,0,0,12),TextWrapping=TextWrapping.Wrap});
        void Module(string label,bool value,Action<bool> change){var field=new CheckBox {Content=T.Localize(label),IsChecked=value,Style=(Style)FindResource("DarkCheck"),Margin=new Thickness(0,0,0,10)};field.Click+=(_,_)=>change(field.IsChecked==true);modules.Children.Add(field);}

        Module("Show pet tracker",meter.Settings.ShowPetTrackerModule,v=>_moduleDraft.ShowPetTrackerModule=v);
        modules.Children.Add(new TextBlock {Text=T.Localize("Tracking mode"),Margin=new Thickness(0,4,0,5)});
        var petMode=new ComboBox {Tag="pet-tracking-mode",ItemsSource=new[]{T.Localize("Nearby & region"),T.Localize("Pet on hit")},SelectedIndex=_moduleDraft.PetOnHitOnly?1:0,Margin=new Thickness(0,0,0,5)};
        petMode.SelectionChanged+=(_,_)=>_moduleDraft.PetOnHitOnly=petMode.SelectedIndex==1;
        modules.Children.Add(petMode);
        modules.Children.Add(new TextBlock {Text=T.Localize("Pet on hit shows only your three most recent incomplete attacked sources."),TextWrapping=TextWrapping.Wrap,Foreground=ThemeColors.Brush("#A9B8C7"),FontSize=11,Margin=new Thickness(0,0,0,12)});
        Module("Sound on collection / level up",meter.Settings.PetNotify,v=>_moduleDraft.PetNotify=v);
        modules.Children.Add(new PetSoundSettingsPanel(_moduleDraft,(sound,volume)=>{if(!meter.Settings.Transient)AppHost.Current.PreviewEventSound(sound,volume);}));
        modules.Children.Add(new TextBlock {Text=T.Localize("Window width"),Margin=new Thickness(0,6,0,4)});
        var petWidth=new Slider {Minimum=150,Maximum=1600,Value=Math.Clamp(meter.Settings.PetModuleWidth,150,1600),Style=(Style)FindResource("GoldSlider")};
        petWidth.ValueChanged+=(_,e)=>_moduleDraft.PetModuleWidth=e.NewValue;modules.Children.Add(petWidth);
        var recap=new CheckBox {Content=T.Localize("Show death recap"),IsChecked=meter.Settings.ShowDeathRecapModule,Style=(Style)FindResource("DarkCheck")};
        recap.Click+=(_,_)=>_moduleDraft.ShowDeathRecapModule=recap.IsChecked==true;
        if(Tab("combat").Content is ScrollViewer {Content:Panel combat})combat.Children.Add(new Border {Child=recap,Style=(Style)FindResource("Section")});
        SettingsTabs.Items.Add(new TabItem {Header=T.Localize("Pets"),Tag="pets",Padding=new Thickness(12,10,12,10),Content=new ScrollViewer {VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,Padding=new Thickness(14,0,0,0),Content=new Border {Child=modules,Style=(Style)FindResource("Section"),VerticalAlignment=VerticalAlignment.Top}}});
        _checklistPreferences=new ChecklistSettingsPanel(_moduleDraft);
        SettingsTabs.Items.Add(new TabItem {Header=T.Localize("Checklist"),Tag="checklist",Padding=new(12,10,12,10),Content=new ScrollViewer {VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Padding=new(14,0,0,0),Content=new Border {Child=_checklistPreferences,Style=(Style)FindResource("Section")}}});
        var cubes=new StackPanel();
        cubes.Children.Add(new TextBlock {Text=T.Localize("Hidden cubes"),FontSize=16,Margin=new(0,0,0,10)});
        var cubeEnabled=new CheckBox {Tag="hidden-cube-enabled",Content=T.Localize("Enable hidden cube alerts (experimental)"),IsChecked=_moduleDraft.ShowHiddenCubeModule,Style=(Style)FindResource("DarkCheck")};
        cubeEnabled.Click+=(_,_)=>_moduleDraft.ShowHiddenCubeModule=cubeEnabled.IsChecked==true;cubes.Children.Add(cubeEnabled);
        HiddenCubeWindow? cubePreview=null;
        var previewCube=new Button {Content=T.Localize("Show test alert"),Tag="hidden-cube-preview",Style=(Style)FindResource("GhostButton"),HorizontalAlignment=HorizontalAlignment.Left,Margin=new(0,10,0,4),Padding=new(10,5,10,5)};
        previewCube.Click+=(_,_)=>
        {
            if(cubePreview is not null){cubePreview.Show();return;}
            var preview=cubePreview=new HiddenCubeWindow(meter.Settings,preview:true,keyStock:meter.Resources.ReadCubeKeys);
            WindowClickThrough.Attach(preview,()=>false,()=>meter.Settings.WindowOpacity);
            preview.Closed+=(_,_)=>cubePreview=null;preview.Show();
        };
        cubes.Children.Add(previewCube);
        var cubeSound=new HiddenCubeSoundPanel(_moduleDraft,(sound,volume)=>{if(!meter.Settings.Transient)AppHost.Current.PreviewEventSound(sound,volume);});
        cubeSound.Changed+=QueueLivePreview;cubes.Children.Add(cubeSound);
        Closed+=(_,_)=>cubePreview?.Close();
        cubes.Children.Add(new TextBlock {Text=T.Localize("Live preview · Save keeps changes; closing discards unsaved changes."),TextWrapping=TextWrapping.Wrap,Margin=new(0,8,0,8)});
        cubes.Children.Add(new TextBlock {Text=T.Localize("Shows a movable alert for the cube type confirmed in recordings. Hides after opening, changing maps or 60 seconds without a fresh announcement. Other cube variants are not yet verified. No reliable player position is available for direction, distance or map placement."),TextWrapping=TextWrapping.Wrap,Foreground=ThemeColors.Brush("#A9B8C7"),FontSize=11});
        SettingsTabs.Items.Add(new TabItem {Header=T.Localize("Hidden cubes"),Tag="cubes",Padding=new(12,10,12,10),Content=new ScrollViewer {VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Padding=new(14,0,0,0),Content=new Border {Child=cubes,Style=(Style)FindResource("Section")}}});
        SettingsTabs.Items.Add(new TabItem {Header=T.Localize("Overlay profiles"),Tag="profiles",Padding=new Thickness(12,10,12,10),Content=new ScrollViewer {VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,Padding=new Thickness(14,0,0,0),Content=new OverlayProfilesPanel()}});
        // Fold secondary sections and reflow rows instead of forcing a wide inner canvas.
        SettingsTabs.Items.Add(new TabItem {Header=ControlPanelSettingsPanel.L("Help & symbols","Hilfe & Symbole"),Tag="help",Padding=new Thickness(12,10,12,10),Content=new ScrollViewer {VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Padding=new Thickness(14,0,0,0),Content=new SymbolHelpPanel(meter.Settings)}});
        SettingsTabs.Items.Add(new TabItem{Header=ControlPanelSettingsPanel.L("Resources","Ressourcen"),Tag="resources",Content=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Padding=new Thickness(14,0,0,0),Content=new Border{Child=new ResourcesSettingsPanel(_moduleDraft,meter),Style=(Style)FindResource("Section")}}});
        SettingsTabs.Items.Add(new TabItem{Header=ControlPanelSettingsPanel.L("Dungeon entries","Dungeon-Eintritte"),Tag="dungeons",Content=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Padding=new Thickness(14,0,0,0),Content=new Border{Child=new DungeonEntriesSettingsPanel(_moduleDraft),Style=(Style)FindResource("Section")}}});
        SettingsTabs.Items.Add(new TabItem{Header="Patreon",Tag="patreon",Content=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Padding=new(14,0,0,0),Content=new PatreonSettingsPanel(_moduleDraft,theme=>_themePanel.Set(theme))}});
        var orderedTabs=new[]{"general","appearance","profiles","combat","events","pets","checklist","cubes","resources","dungeons","hotkeys","history","capture","updates","patreon","feedback","help"}.Select(Tab).ToArray();
        SettingsTabs.Items.Clear();
        foreach(var tab in orderedTabs)SettingsTabs.Items.Add(tab);
        var groupStarts=new[]{"general","combat","hotkeys","updates"};
        for(var group=0;group<groupStarts.Length;group++)SettingsNavigation.SetGroupTitle(Tab(groupStarts[group]),SettingsNavigation.Title(group));
        SelectTab("appearance");
        foreach (var tab in SettingsTabs.Items.OfType<TabItem>())
            if (tab.Content is ScrollViewer viewer && viewer.Content is FrameworkElement content)
            {
                SettingsSections.Organize(content);
                void FitContent() => content.Width = Math.Max(0, viewer.ActualWidth - viewer.Padding.Left - viewer.Padding.Right - 18);
                viewer.Loaded += (_, _) => FitContent();
                viewer.SizeChanged += (_, _) => FitContent();
            }
        AppIcon.Apply(this);
        Loaded+=(_,_)=>WindowPresentation.PlaceOnPreferredScreen(this);
        Controls.DragAnywhere.Attach(this);
        Load();
        foreach (var field in new[] { HkToggle, HkAll, HkReset, HkClick, HkTimers, HkPersonal, HkEvents, HkPets, HkDeathRecap, HkChecklist })
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
        Closed += (_, _) => _eventDraft.Dispose();
        InitializeLivePreview();
    }

    private void Load()
    {
        var s = _meter.Settings;
        ModulesTitle.Text=T.Localize("Modules");
        ShowDpsMeterCheck.Content=T.Localize("Show DPS meter");
        ShowDpsMeterCheck.IsChecked=s.ShowDpsMeterModule;
        RememberClickThroughCheck.IsChecked=s.RememberClickThrough;
        RememberClickThroughCheck.Content=T.Localize("Start with click-through enabled");
        RememberClickThroughHint.Text=T.Localize("Always enables click-through at startup, even after a temporary unlock. The control panel stays clickable.");
        ShowDpsMeterHint.Text=T.Localize("Hides combat statistics but keeps a small CatScratch bar with Settings available. Capture and other modules keep running.");
        GameStartTitle.Text=T.Localize("AION 2 companion");
        FollowGameCheck.Content=T.Localize("Start and close CatScratch with AION 2");
        FollowGameCheck.IsChecked=s.FollowGame;
        FollowGameHint.Text=T.Localize("Optional background watcher starts with Windows sign-in. Select the game executable, not the launcher. CatScratch closes 10 seconds after the game exits.");
        ChooseGameButton.Content=T.Localize("Choose game…");
        GameExecutableText.Text=s.GameExecutable;
        GameExecutableText.ToolTip=s.GameExecutable;
        CompactModeCheck.IsChecked = s.CompactMode;
        UiScaleLabel.Text=T.Localize("UI scale"); UiScaleValue.Value=Math.Clamp(s.UiScale,.75,1.5)*100;
        StreamModeCheck.Content=T.Localize("Stream mode: hide player names");StreamModeCheck.IsChecked=s.StreamMode;
        StreamModeHint.Text=T.Localize("Only displayed and copied player names are hidden. Local recordings and saved files keep their original data.");
        AutomaticFightEndCheck.Content=T.Localize("Automatic fight end");AutomaticFightEndCheck.IsChecked=s.AutomaticFightEnd;
        AutomaticFightEndHint.Text=T.Localize("After inactivity, keep the finished fight until the next fight. Healing and incoming damage count as activity.");
        TestModeSection.Text=T.Localize("Test mode");
        TestModeButton.Content=T.Localize(_meter.DemoRunning?"Stop test mode":"Start test mode");
        TestModeHint.Text=T.Localize("Preview screen modules, Statistics and Boss comparison at your existing positions, without click-through. DPS and Settings keep their sizes. Only new modules get a free position. Other menus stay closed.");
        WindowWidthText.Text = s.OverlayWidth.ToString("0");
        WindowHeightText.Text = s.OverlayHeight.ToString("0");
        WindowOpacityValue.Value = s.WindowOpacity;
        BarsRelative.IsChecked = s.BarsRelativeToTop;
        ShowBoss.IsChecked = s.ShowBossPanel;
        DetachBossTimingCheck.IsChecked=s.DetachBossTiming;
        _bossTimingDetachedAtOpen=s.DetachBossTiming;
        DetachBossTimingCheck.Content=T.Localize("Separate boss timing window");
        DetachBossTimingHint.Text=T.Localize("Moves Enrage time and Estimated defeat out of the DPS meter. Turn off to put them back.");
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
        Mode.SelectedIndex = (int)s.TargetMode;
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
        OpenCaptures.Content = T.Localize("Open captures folder");
        RecentDebugResult.Text = T.Localize("Enable and save settings first. High traffic can shorten the recorded minute.");
        HkToggle.Text = s.HotkeyToggleOverlay;
        HkAll.Text = s.HotkeyToggleAll;
        HkReset.Text = s.HotkeyReset;
        HkClick.Text = s.HotkeyClickThrough;
        HkTimers.Text = s.HotkeyTimers;
        HkPersonal.Text=s.HotkeyPersonal;
        HkEvents.Text=s.HotkeyEvents;
        HkPets.Text=s.HotkeyPets;
        HkDeathRecap.Text=s.HotkeyDeathRecap;
        HkChecklist.Text=s.HotkeyChecklist;

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
    private TabItem Tab(string id)=>SettingsTabs.Items.OfType<TabItem>().Single(t=>Equals(t.Tag,id));
    internal void SelectTab(string id)=>SettingsTabs.SelectedItem=Tab(id);
    internal void SelectEvents()=>SelectTab("events");

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

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if(_previewSaving)return;
        _previewSaving=true;_previewTimer.Stop();
        try
        {
        var s = _meter.Settings;
        var hotkeyFields=new[] {HkToggle,HkAll,HkReset,HkClick,HkTimers,HkPersonal,HkEvents,HkPets,HkDeathRecap,HkChecklist};
        var invalidHotkey=HotkeyManager.InvalidGestureIndex(hotkeyFields.Select(field=>field.Text));
        if(invalidHotkey>=0)
        {
            SelectTab("hotkeys");
            MessageBox.Show(this,T.Localize("Choose a valid, different shortcut for each action."),"CatScratch",MessageBoxButton.OK,MessageBoxImage.Warning);
            hotkeyFields[invalidHotkey].Focus();return;
        }
        if(!_themePanel.TryGet(out var theme)){SelectTab("appearance");_themePanel.FocusInvalidColor();return;}
        var follow=FollowGameCheck.IsChecked==true;
        var game=GameExecutableText.Text;
        try
        {
            if(follow) GameLifetime.Validate(game);
            if(!s.Transient && (follow || follow!=s.FollowGame))
            {
                IsEnabled=false;
                try { await GameLifetime.Configure(follow,game); }
                finally { IsEnabled=true; }
            }
        }
        catch(Exception error) when(error is IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            MessageBox.Show(this,T.Localize("Could not configure automatic game start.")+"\n"+T.Localize(error.Message),"CatScratch",MessageBoxButton.OK,MessageBoxImage.Error);
            return;
        }
        var dpsVisibilityChanged=s.ShowDpsMeterModule!=(ShowDpsMeterCheck.IsChecked==true);
        var displayChanged=!string.Equals(s.PreferredDisplay,_moduleDraft.PreferredDisplay,StringComparison.OrdinalIgnoreCase);
        s.PreferredDisplay=_moduleDraft.PreferredDisplay;
        s.ShowDpsMeterModule=ShowDpsMeterCheck.IsChecked==true;
        s.RememberClickThrough=RememberClickThroughCheck.IsChecked==true;
        if(dpsVisibilityChanged)s.OverlayVisible=s.ShowDpsMeterModule;
        s.Theme=theme;ThemeColors.Apply(theme);
        if(_controlPanelPreferences.Changed){s.ControlsLayout=_moduleDraft.ControlsLayout;s.ControlsAnchor=_moduleDraft.ControlsAnchor;}
        s.FollowGame=follow; s.GameExecutable=game;
        s.CompactMode = CompactModeCheck.IsChecked == true;
        if (int.TryParse(WindowWidthText.Text, out var width)) s.OverlayWidth = Math.Clamp(width, 260, 1600);
        if (int.TryParse(WindowHeightText.Text, out var height)) s.OverlayHeight = Math.Clamp(height, 180, 1200);
        s.WindowOpacity = WindowOpacityValue.Value;
        var captureChanged = false;

        s.BarsRelativeToTop = BarsRelative.IsChecked == true;
        s.ShowBossPanel = ShowBoss.IsChecked == true;
        s.UiScale=UiScaleValue.Value/100; s.StreamMode=StreamModeCheck.IsChecked==true;
        s.AutomaticFightEnd=AutomaticFightEndCheck.IsChecked==true;
        // A timing-window X may have returned the rows while this draft was open.
        if((DetachBossTimingCheck.IsChecked==true)!=_bossTimingDetachedAtOpen)
            s.DetachBossTiming=DetachBossTimingCheck.IsChecked==true;
        _bossTimingDetachedAtOpen=s.DetachBossTiming;
        DetachBossTimingCheck.IsChecked=s.DetachBossTiming;
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
        s.TargetMode = (AionMeter.Core.Combat.TargetMode)Math.Clamp(Mode.SelectedIndex,0,2);
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
        s.HotkeyDeathRecap=HkDeathRecap.Text.Trim();
        s.HotkeyChecklist=HkChecklist.Text.Trim();

        _cards.ApplyPreferences(_eventDraft);
        var petVisibilityChanged=s.ShowPetTrackerModule!=_moduleDraft.ShowPetTrackerModule;
        s.PetNotify=_moduleDraft.PetNotify;
        s.PetOnHitOnly=_moduleDraft.PetOnHitOnly;
        _livePreview?.RestoreForCommit("ChecklistAutoReset","ChecklistResetHourUtc","ChecklistWeeklyResetDay");
        _checklistPreferences.ApplyTo(s);
        s.ShowDungeonEntriesModule=_moduleDraft.ShowDungeonEntriesModule;
        s.DungeonEntriesLayout=_moduleDraft.DungeonEntriesLayout;
        s.DungeonEntriesWidth=_moduleDraft.DungeonEntriesWidth;
        s.VisibleDungeonEntries=new(_moduleDraft.VisibleDungeonEntries??[]);
        s.SnapModules=_moduleDraft.SnapModules;s.ModuleSpacing=_moduleDraft.ModuleSpacing;
        s.ResourceAlerts=(_moduleDraft.ResourceAlerts??[]).ToDictionary(p=>p.Key,p=>new ResourceAlertRule{Enabled=p.Value.Enabled,Threshold=p.Value.Threshold});
        s.ResourceAlertSoundEnabled=_moduleDraft.ResourceAlertSoundEnabled;s.ResourceAlertSound=_moduleDraft.ResourceAlertSound;s.ResourceAlertVolume=_moduleDraft.ResourceAlertVolume;
        s.ShowSupporterPaw=_moduleDraft.ShowSupporterPaw;
        s.ShowSupporterCat=_moduleDraft.ShowSupporterCat;
        s.SupporterCatScale=SupporterCatWindow.NormalizeScale(_moduleDraft.SupporterCatScale);
        s.ShowResourcesModule=_moduleDraft.ShowResourcesModule;
        s.VisibleResources=new(_moduleDraft.VisibleResources??[]);
        s.ResourcesWidth=_moduleDraft.ResourcesWidth;
        s.ResourcesLayout=_moduleDraft.ResourcesLayout;
        s.ShowHiddenCubeModule=_moduleDraft.ShowHiddenCubeModule;
        s.HiddenCubeSoundEnabled=_moduleDraft.HiddenCubeSoundEnabled;
        s.HiddenCubeSoundPath=_moduleDraft.HiddenCubeSoundPath;
        s.HiddenCubeSoundVolume=_moduleDraft.HiddenCubeSoundVolume;
        s.PetSound=_moduleDraft.PetSound;
        s.PetSoundVolume=_moduleDraft.PetSoundVolume;
        s.PetModuleWidth=_moduleDraft.PetModuleWidth;
        s.ShowDeathRecapModule=_moduleDraft.ShowDeathRecapModule;
        if(petVisibilityChanged && !s.Transient)AppHost.Current.TogglePetModule();
        s.ShowPetTrackerModule=_moduleDraft.ShowPetTrackerModule;

        CommitLivePreview();
        _meter.ApplySettings();
        if (captureChanged) _meter.RestartCapture();
        _controlPanelPreferences.MarkSaved();
        AppHost.Current.OnSettingsChanged(dpsVisibilityChanged);
        AppHost.Current.RefreshPresentation();
        AppHost.Current.RefreshEventSettings();
        AppHost.Current.RefreshPetSettings();
        if (language != UiText.Normalize(s.Language)) AppHost.Current.SetLanguage(language);
        if(!s.Transient)
        {
            WindowPresentation.ConfigureDisplay(s.PreferredDisplay);
            if(displayChanged){WindowPresentation.MoveOverlaysToPreferredScreen();WindowPresentation.PlaceOnPreferredScreen(this);}
        }
        AppHost.Current.RefreshSettingsPreview(_meter);
        BeginLivePreview();
        }
        finally{_previewSaving=false;}
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

    private void ChooseGame_Click(object sender, RoutedEventArgs e)
    {
        var picker=new Microsoft.Win32.OpenFileDialog { Title=T.Localize("Choose the AION 2 game executable first."), Filter="Windows executable (*.exe)|*.exe", CheckFileExists=true };
        if(picker.ShowDialog(this)==true) { GameExecutableText.Text=picker.FileName; GameExecutableText.ToolTip=picker.FileName; }
    }

    /// <summary>The overlay follows the slider at once, so the size can be judged on the real rows.</summary>
    private void RowSize_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RowSizeText is null) return; // the slider sets its first value before the rest of the window exists
        var percent = (int)Math.Round(e.NewValue);
        RowSizeText.Text = $"{percent}%";
        // The shared transaction applies row size and restores it on close.
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
        var start=new ProcessStartInfo("explorer.exe") { UseShellExecute = true };
        start.ArgumentList.Add(path);
        Process.Start(start);
    }

    private void OpenData_Click(object sender, RoutedEventArgs e) => Open(Path.Combine(AppContext.BaseDirectory, "data"));

    private void OpenAppData_Click(object sender, RoutedEventArgs e) => Open(AppSettings.AppDataDir);

    private void OpenLogs_Click(object sender, RoutedEventArgs e) => Open(Log.Directory);
    private void OpenCaptures_Click(object sender, RoutedEventArgs e) => Open(Path.Combine(AppSettings.AppDataDir,"captures"));
}
