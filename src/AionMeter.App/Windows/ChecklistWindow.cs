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
    private readonly StackPanel _rows=new();
    private readonly DockPanel _header=new(){Margin=new(4,0,1,0)};
    private readonly DispatcherTimer _save=new(){Interval=TimeSpan.FromMilliseconds(450)};
    private readonly List<(CheckBox Check,TextBox Text)> _fields=[];
    private bool _displayOnly;
    private readonly DispatcherTimer _resetClock=new(){Interval=TimeSpan.FromSeconds(1)};
    private readonly Dictionary<bool,TextBlock> _countdowns=[];
    private readonly ChecklistInputSurface _input;
    internal bool DisplayOnly=>_displayOnly;
    internal ChecklistInputSurface InputSurface=>_input;
    internal IReadOnlyList<(CheckBox Check,TextBox Text)> Fields=>_fields;
    internal string[] SectionHeaders=>_rows.Children.OfType<DockPanel>().SelectMany(p=>p.Children.OfType<TextBlock>().Where(t=>!_countdowns.Values.Contains(t))).Select(t=>t.Text).ToArray();
    internal ScrollViewer TasksScroller {get;}
    internal Button CloseButton {get;}
    internal ChecklistWindow(AppSettings settings)
    {
        _settings=settings;ChecklistState.Normalize(settings);_input=new(this);
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
        var headerFrame=new Border {Background=ThemeColors.Brush("#B8191D23"),CornerRadius=new(5),Child=_header};
        DockPanel.SetDock(headerFrame,Dock.Top);root.Children.Add(headerFrame);
        TasksScroller=new ScrollViewer {Content=_rows,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
        root.Children.Add(TasksScroller);Content=root;
        if(settings.ChecklistPositionSaved && double.IsFinite(settings.ChecklistLeft) && double.IsFinite(settings.ChecklistTop)){Left=settings.ChecklistLeft;Top=settings.ChecklistTop;}
        else {Left=settings.OverlayLeft+40;Top=settings.OverlayTop+40;}
        _save.Tick+=(_,_)=>Flush();
        _resetClock.Tick+=(_,_)=>RefreshResetClock();
        IsVisibleChanged+=(_,_)=>{if(IsVisible){RefreshResetClock();_resetClock.Start();}else _resetClock.Stop();};
        Controls.DragAnywhere.Attach(this,dropped:SavePlacement);
        LocationChanged+=(_,_)=>_input.Schedule();SizeChanged+=(_,_)=>_input.Schedule();
        IsVisibleChanged+=(_,_)=>_input.Sync();Loaded+=(_,_)=>_input.Schedule();
        TasksScroller.ScrollChanged+=(_,_)=>_input.Schedule();
        Closed+=(_,_)=>{_input.Dispose();_save.Stop();_resetClock.Stop();SavePlacement();AppHost.Current.ResumeHotkeys();};
        ApplyPreferences();
    }
    internal void ApplyPreferences()
    {
        var desiredWidth=double.IsFinite(_settings.ChecklistWidth)?Math.Clamp(_settings.ChecklistWidth,220,650):320;
        if(_fields.Count==Math.Clamp(_settings.ChecklistRows,1,ChecklistState.MaximumSlots) && Width==desiredWidth){if(_displayOnly)_input.Sync();return;}
        ChecklistState.Normalize(_settings);Width=_settings.ChecklistWidth;
        if(_fields.Count!=_settings.ChecklistRows)
        {
            Flush();_rows.Children.Clear();_fields.Clear();_countdowns.Clear();
            for(var i=0;i<_settings.ChecklistRows;i++)
            {
                if(i is 0 or 2 or 11)
                {
                    var heading=new DockPanel {Margin=new(4,i==0?3:5,4,1)};
                    if(i<11)
                    {
                        var countdown=new TextBlock {FontSize=9,Foreground=ThemeColors.Brush("#A9B8C7"),VerticalAlignment=VerticalAlignment.Center};
                        _countdowns[i==2]=countdown;DockPanel.SetDock(countdown,Dock.Right);heading.Children.Add(countdown);
                    }
                    heading.Children.Add(new TextBlock {Text=UiText.Current.Localize(i==0?"Daily":i==2?"Weekly":"Other tasks"),FontSize=9,FontWeight=FontWeights.SemiBold,Foreground=ThemeColors.Brush("#C49AF5")});
                    _rows.Children.Add(heading);
                }
                var item=_settings.ChecklistItems[i];
                var check=new CheckBox {IsChecked=item.Completed,VerticalAlignment=VerticalAlignment.Center,Margin=new(4,0,5,0),Style=(Style)FindResource("DarkCheck"),ToolTip=UiText.Current.Localize("Mark task complete / incomplete")};
                var text=new TextBox {Text=item.Text,MaxLength=240,Background=Brushes.Transparent,BorderThickness=new(0),Padding=new(1,1,3,1),Foreground=ThemeColors.Brush("#EDF2F7"),CaretBrush=ThemeColors.Brush("#C49AF5"),FontSize=11,VerticalContentAlignment=VerticalAlignment.Center,ToolTip=UiText.Current.Localize("Type a task")};
                void Decorate(){text.TextDecorations=item.Completed?TextDecorations.Strikethrough:null;text.Opacity=item.Completed?.6:1;}
                Decorate();
                check.Click+=(_,_)=>{item.Completed=check.IsChecked==true;Decorate();Flush();};
                text.TextChanged+=(_,_)=>{item.Text=text.Text;_save.Stop();_save.Start();};
                text.GotKeyboardFocus+=(_,_)=>AppHost.Current.SuspendHotkeys();
                text.LostKeyboardFocus+=(_,_)=>{Flush();AppHost.Current.ResumeHotkeys();};
                text.PreviewKeyDown+=(_,e)=>{if(e.Key is System.Windows.Input.Key.Enter or System.Windows.Input.Key.Escape){System.Windows.Input.Keyboard.ClearFocus();e.Handled=true;}};
                var line=new DockPanel {MinHeight=23};DockPanel.SetDock(check,Dock.Left);line.Children.Add(check);line.Children.Add(text);
                var layers=new Grid();layers.Children.Add(new Border {Background=ThemeColors.Brush("#DF20242A"),CornerRadius=new(5)});
                layers.Children.Add(new Border {Background=ThemeColors.Brush("#23C49AF5"),CornerRadius=new(5)});layers.Children.Add(line);
                _rows.Children.Add(new Border {Child=layers,CornerRadius=new(5),BorderBrush=ThemeColors.Brush("#60C49AF5"),BorderThickness=new(1),Margin=new(0,1,0,1)});
                _fields.Add((check,text));
            }
            SetDisplayOnly(_displayOnly);
        }
    }
    internal void RefreshResetClock()
    {
        var now=DateTimeOffset.UtcNow;
        ChecklistSchedule.Update(_settings,now);
        foreach(var (weekly,text) in _countdowns)
        {
            text.Text="↻ "+ChecklistSchedule.Countdown(_settings,now,weekly);
            text.ToolTip=ChecklistSchedule.Next(_settings,now,weekly).ToLocalTime().ToString("ddd dd.MM. HH:mm")+" · "+_settings.ChecklistResetHourUtc.ToString("00")+":00 UTC";
        }
        for(var i=0;i<_fields.Count;i++)
        {
            var completed=_settings.ChecklistItems[i].Completed;
            _fields[i].Check.IsChecked=completed;
            _fields[i].Text.TextDecorations=completed?TextDecorations.Strikethrough:null;
            _fields[i].Text.Opacity=completed?.6:1;
        }
    }
    private void SavePlacement()
    {
        _settings.ChecklistLeft=Left;_settings.ChecklistTop=Top;_settings.ChecklistPositionSaved=true;Flush();
    }
    internal void Flush()
    {
        _save.Stop();
        try{_settings.Save();}
        catch(Exception error) when(error is System.IO.IOException or UnauthorizedAccessException)
        {Log.Error("Checklist could not be saved",error);_header.ToolTip=UiText.Current.Localize("Could not save checklist.");}
    }
    public void SetDisplayOnly(bool enabled)
    {
        _displayOnly=enabled;_header.Visibility=enabled?Visibility.Collapsed:Visibility.Visible;
        foreach(var field in _fields)field.Text.IsReadOnly=enabled;
        _input.Sync();_input.Schedule();
    }
}

