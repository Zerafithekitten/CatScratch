using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using AionMeter.App.Services;
namespace AionMeter.App.Windows;

internal sealed class ChecklistWindow : Window,IDisplayAppearance
{
    private readonly AppSettings _settings;
    private readonly Border _headerFrame;
    private readonly DockPanel _header=new(){Margin=new(4,0,1,0)};
    private readonly ChecklistPlannerPanel _planner;
    private bool _displayOnly;
    private readonly DispatcherTimer _resetClock=new(){Interval=TimeSpan.FromSeconds(1)};
    private readonly ChecklistInputSurface _input;
    internal bool DisplayOnly=>_displayOnly;
    internal ChecklistInputSurface InputSurface=>_input;
    internal IReadOnlyList<Button> PlannerActions=>_planner.Actions;
    internal IReadOnlyList<(CheckBox Check,TextBox Text)> Fields=>_planner.Fields;
    internal string[] SectionHeaders=>ChecklistPlanner.For(_settings).Data.Tasks.Select(t=>t.Section).Distinct().ToArray();
    internal ScrollViewer TasksScroller {get;}
    internal Button CloseButton {get;}
    internal ChecklistWindow(AppSettings settings,Func<AionMeter.Core.Events.SelfIdentifiedEvent?>? identity=null,Func<int,string>? serverName=null)
    {
        _settings=settings;_input=new(this);
        Title="CatScratch · "+UiText.Current.Localize("Checklist");
        Width=settings.ChecklistWidth;MinWidth=220;MaxWidth=650;SizeToContent=SizeToContent.Height;
        WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;Background=Brushes.Transparent;
        Topmost=true;ShowActivated=false;ShowInTaskbar=false;FontFamily=new("Segoe UI");FontSize=12;Foreground=ThemeColors.Brush("#EDF2F7");
        Resources.MergedDictionaries.Add(new(){Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative)});
        AppIcon.Apply(this);
        var root=new DockPanel {Margin=new(2)};
        CloseButton=new Button {Content="\uE711",Style=(Style)FindResource("IconButton"),Width=20,Height=20,ToolTip=UiText.Current.TipClose};
        CloseButton.Click+=(_,_)=>{settings.ShowChecklistModule=false;Close();};
        DockPanel.SetDock(CloseButton,Dock.Right);_header.Children.Add(CloseButton);
        var pin=WindowPinning.CreateButton(this,settings,"checklist");pin.Width=20;pin.Height=20;DockPanel.SetDock(pin,Dock.Right);_header.Children.Add(pin);
        var paw=new Image {Source=AppIcon.Image,Width=12,Height=12,Margin=new(0,0,5,0)};DockPanel.SetDock(paw,Dock.Left);_header.Children.Add(paw);
        _header.Children.Add(new TextBlock {Text=UiText.Current.Localize("Checklist"),FontSize=11,Foreground=ThemeColors.Brush("#C49AF5"),VerticalAlignment=VerticalAlignment.Center});
        _headerFrame=new Border {Background=ThemeColors.Brush("#B8191D23"),CornerRadius=new(5),Child=_header};
        DockPanel.SetDock(_headerFrame,Dock.Top);root.Children.Add(_headerFrame);
        _planner=new ChecklistPlannerPanel(settings,identity,()=>_input.Schedule(),serverName);
        TasksScroller=new ScrollViewer {Content=_planner,MaxHeight=700,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
        root.Children.Add(TasksScroller);Content=root;
        if(settings.ChecklistPositionSaved && double.IsFinite(settings.ChecklistLeft) && double.IsFinite(settings.ChecklistTop)){Left=settings.ChecklistLeft;Top=settings.ChecklistTop;}
        else {Left=settings.OverlayLeft+40;Top=settings.OverlayTop+40;}
        _resetClock.Tick+=(_,_)=>RefreshResetClock();
        IsVisibleChanged+=(_,_)=>{if(IsVisible){RefreshResetClock();_resetClock.Start();}else _resetClock.Stop();};
        Controls.DragAnywhere.Attach(this,dropped:SavePlacement);
        LocationChanged+=(_,_)=>_input.Schedule();SizeChanged+=(_,_)=>_input.Schedule();
        IsVisibleChanged+=(_,_)=>_input.Sync();Loaded+=(_,_)=>_input.Schedule();
        TasksScroller.ScrollChanged+=(_,_)=>_input.Schedule();
        Closed+=(_,_)=>{_input.Dispose();_resetClock.Stop();SavePlacement();AppHost.Current.ResumeHotkeys();};
        ApplyPreferences();
    }
    internal void ApplyPreferences()
    {
        Width=double.IsFinite(_settings.ChecklistWidth)?Math.Clamp(_settings.ChecklistWidth,220,650):320;
        if(_displayOnly)_input.Sync();
    }
    internal void RefreshResetClock()=>_planner.Tick();
    private void SavePlacement()
    {
        _settings.ChecklistLeft=Left;_settings.ChecklistTop=Top;_settings.ChecklistPositionSaved=true;Flush();
    }
    internal void Flush()
    {
        try{_settings.Save();}
        catch(Exception error) when(error is System.IO.IOException or UnauthorizedAccessException)
        {Log.Error("Checklist could not be saved",error);_header.ToolTip=UiText.Current.Localize("Could not save checklist.");}
    }
    public void SetDisplayOnly(bool enabled)
    {
        _displayOnly=enabled;_headerFrame.Visibility=enabled?Visibility.Hidden:Visibility.Visible;
        _planner.SetDisplayOnly(enabled);
        _input.Sync();_input.Schedule();
    }
}

