using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using AionMeter.App.Services;
namespace AionMeter.App.Windows;

public partial class SettingsWindow
{
    internal void SetMapFilters(IReadOnlyList<string> selected)
    {
        _moduleDraft.MapCategories=selected.ToList();
        if(Tab("map").Content is ScrollViewer scroll&&scroll.Content is Border border&&border.Child is MapSettingsPanel panel)panel.RefreshCategories(selected);
        ApplyLivePreview();
    }
    private SettingsPreviewSession? _livePreview;
    private readonly DispatcherTimer _previewTimer=new(){Interval=TimeSpan.FromMilliseconds(100)};
    private bool _previewReady,_previewSaving,_previewApplying,_previewClosed;
    private string? _lastEventPreview;
    private bool _previewDpsInitial,_previewOverlayInitial;
    private Dictionary<Window,Point> _previewPositions=[];
    internal static readonly string[] PreviewProperties=("DownloadIcons Language OverlayVisible Theme CompactMode OverlayWidth OverlayHeight WindowOpacity BackgroundOpacity RowSize UiScale StreamMode BarsRelativeToTop ShowBossPanel ShowDpsMeterModule RememberClickThrough MaxRows ShowGear ShowCombatPower DetachBossTiming ShowPersonalCombatModule ShowHealingDone AutoShow AutoHideSeconds ShowOnStart AutomaticFightEnd TargetMode IdleTimeoutSec BossIdleTimeoutSec ControlsLayout ControlsAnchor PreferredDisplay SnapModules ModuleSpacing PetNotify PetOnHitOnly PetNearbyOnly PetSound PetSoundVolume PetModuleWidth ShowPetTrackerModule ShowDeathRecapModule ShowChecklistModule ChecklistWidth ChecklistAutoReset ChecklistResetHourUtc ChecklistWeeklyResetDay ShowDungeonEntriesModule DungeonEntriesLayout DungeonEntriesWidth VisibleDungeonEntries ResourceAlerts ResourceAlertSoundEnabled ResourceAlertSound ResourceAlertVolume ShowSupporterPaw ShowSupporterCat SupporterCatScale ShowResourcesModule VisibleResources ResourcesWidth ResourcesLayout ShowHiddenCubeModule HiddenCubeSoundEnabled HiddenCubeSoundPath HiddenCubeSoundVolume BossAlertMinutes ShowMapOverlay MapDirectDrag MapCategories").Split(' ');
    private void InitializeLivePreview()
    {
        BeginLivePreview();
        void Queue(object? sender,RoutedEventArgs args)
        {
            QueueLivePreview();
        }
        AddHandler(ButtonBase.ClickEvent,new RoutedEventHandler(Queue),true);
        AddHandler(ToggleButton.CheckedEvent,new RoutedEventHandler(Queue),true);
        AddHandler(ToggleButton.UncheckedEvent,new RoutedEventHandler(Queue),true);
        AddHandler(Selector.SelectionChangedEvent,new SelectionChangedEventHandler((sender,args)=>Queue(sender,args)),true);
        AddHandler(TextBox.TextChangedEvent,new TextChangedEventHandler((sender,args)=>Queue(sender,args)),true);
        AddHandler(RangeBase.ValueChangedEvent,new RoutedPropertyChangedEventHandler<double>((sender,args)=>Queue(sender,args)),true);
        _themePanel.Changed+=QueueLivePreview;
        _eventDraft.PreferencesEdited+=QueueLivePreview;
        _previewTimer.Tick+=(_,_)=>{_previewTimer.Stop();ApplyLivePreview();};
        Closed+=(_,_)=>EndLivePreview();
    }
    private void QueueLivePreview()
    {
        if(!_previewReady||_previewSaving||_previewApplying||_previewClosed)return;
        _previewTimer.Stop();_previewTimer.Start();
    }
    private void BeginLivePreview()
    {
        _previewDpsInitial=_meter.Settings.ShowDpsMeterModule;_previewOverlayInitial=_meter.Settings.OverlayVisible;
        UpdateLivePreviewHint();
        _livePreview=new SettingsPreviewSession(_meter.Settings);_cards.BeginPreferencesPreview();
        _lastEventPreview=JsonSerializer.Serialize(_eventDraft.Saved);
        _previewPositions=Application.Current.Windows.Cast<Window>().Where(w=>w!=this&&w.IsLoaded).ToDictionary(w=>w,w=>new Point(w.Left,w.Top));
        _previewReady=true;
    }
    private void UpdateLivePreviewHint()
    {
        LivePreviewHint.Text=T.Localize("Live preview · Save keeps changes; closing discards unsaved changes.");
        LivePreviewHint.ToolTip=T.Localize("Capture, startup integration and hotkeys apply with Save. Explicit file and profile actions remain immediate.");
    }
    private AppSettings ReadLivePreview()
    {
        var draft=_moduleDraft.CreateDraft();
        if(_themePanel.TryGet(out var theme))draft.Theme=theme;else draft.Theme=_meter.Settings.Theme;
        draft.DownloadIcons=Icons.IsChecked==true;
        draft.Language=(LanguageBox.SelectedItem as ComboBoxItem)?.Tag as string??"en";
        draft.ShowDpsMeterModule=ShowDpsMeterCheck.IsChecked==true;
        draft.OverlayVisible=draft.ShowDpsMeterModule!=_previewDpsInitial?draft.ShowDpsMeterModule:_previewOverlayInitial;
        draft.RememberClickThrough=RememberClickThroughCheck.IsChecked==true;
        draft.CompactMode=CompactModeCheck.IsChecked==true;
        draft.OverlayWidth=int.TryParse(WindowWidthText.Text,out var width)?Math.Clamp(width,260,1600):_meter.Settings.OverlayWidth;
        draft.OverlayHeight=int.TryParse(WindowHeightText.Text,out var height)?Math.Clamp(height,180,1200):_meter.Settings.OverlayHeight;
        draft.WindowOpacity=WindowOpacityValue.Value;draft.BackgroundOpacity=Math.Round(OpacityValue.Value,2);
        draft.RowSize=(int)Math.Round(RowSizeValue.Value);draft.UiScale=UiScaleValue.Value/100;
        draft.StreamMode=StreamModeCheck.IsChecked==true;draft.BarsRelativeToTop=BarsRelative.IsChecked==true;
        draft.ShowBossPanel=ShowBoss.IsChecked==true;draft.MaxRows=MaxRows.SelectedItem is int rows?rows:10;
        draft.ShowGear=ShowGear.IsChecked==true;draft.ShowCombatPower=ShowCombatPower.IsChecked==true;
        draft.DetachBossTiming=DetachBossTimingCheck.IsChecked==true;
        draft.ShowPersonalCombatModule=ShowPersonalCombatModule.IsChecked==true;draft.ShowHealingDone=ShowHealingDone.IsChecked==true;
        draft.AutoShow=AutoShow.IsChecked==true;draft.AutoHideSeconds=int.TryParse(AutoHideSeconds.Text,out var hide)?Math.Clamp(hide,0,3600):_meter.Settings.AutoHideSeconds;
        draft.ShowOnStart=ShowOnStart.IsChecked==true;draft.AutomaticFightEnd=AutomaticFightEndCheck.IsChecked==true;
        draft.TargetMode=(Core.Combat.TargetMode)Math.Clamp(Mode.SelectedIndex,0,2);
        draft.IdleTimeoutSec=int.TryParse(Idle.Text,out var idle)?Math.Clamp(idle,3,120):_meter.Settings.IdleTimeoutSec;
        draft.BossIdleTimeoutSec=int.TryParse(BossIdle.Text,out var bossIdle)?Math.Clamp(bossIdle,5,300):_meter.Settings.BossIdleTimeoutSec;
        if(!_controlPanelPreferences.Changed){draft.ControlsLayout=_meter.Settings.ControlsLayout;draft.ControlsAnchor=_meter.Settings.ControlsAnchor;}
        draft.BossAlertMinutes=_eventDraft.Settings.BossAlertMinutes;
        return draft;
    }
    internal void ApplyLivePreview()
    {
        if(!_previewReady||_previewSaving||_previewApplying||_previewClosed)return;
        _previewApplying=true;
        try
        {
            var changed=_livePreview!.Apply(ReadLivePreview(),PreviewProperties);
            var events=JsonSerializer.Serialize(_eventDraft.Saved);
            if(events!=_lastEventPreview){_cards.PreviewPreferences(_eventDraft);_lastEventPreview=events;changed=true;}
            if(changed){AppHost.Current.RefreshSettingsPreview(_meter);UpdateLivePreviewHint();}
        }
        finally{_previewApplying=false;}
    }
    private void CommitLivePreview()
    {
        _previewTimer.Stop();_livePreview?.Commit();_cards.EndPreferencesPreview(commit:true);
    }
    private void EndLivePreview()
    {
        _previewClosed=true;_previewTimer.Stop();_previewReady=false;
        _livePreview?.Dispose();_cards.EndPreferencesPreview(commit:false);
        AppHost.Current.RefreshSettingsPreview(_meter);
        foreach(var pair in _previewPositions)if(pair.Key.IsLoaded){pair.Key.Left=pair.Value.X;pair.Key.Top=pair.Value.Y;}
    }
}
