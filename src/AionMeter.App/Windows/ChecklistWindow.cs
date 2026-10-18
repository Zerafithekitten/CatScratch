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
    internal IReadOnlyList<(CheckBox Check,TextBox Text)> Fields=>_fields;
    internal ScrollViewer TasksScroller {get;}
    internal Button CloseButton {get;}
    internal ChecklistWindow(AppSettings settings)
    {
        _settings=settings;ChecklistState.Normalize(settings);
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
        Controls.DragAnywhere.Attach(this,dropped:SavePlacement);
        Closed+=(_,_)=>{_save.Stop();SavePlacement();AppHost.Current.ResumeHotkeys();};
        ApplyPreferences();
    }
    internal void ApplyPreferences()
    {
        var desiredWidth=double.IsFinite(_settings.ChecklistWidth)?Math.Clamp(_settings.ChecklistWidth,220,650):320;
        if(_fields.Count==Math.Clamp(_settings.ChecklistRows,1,ChecklistState.MaximumSlots) && Width==desiredWidth)return;
        ChecklistState.Normalize(_settings);Width=_settings.ChecklistWidth;
        if(_fields.Count!=_settings.ChecklistRows)
        {
            Flush();_rows.Children.Clear();_fields.Clear();
            for(var i=0;i<_settings.ChecklistRows;i++)
            {
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
    }
}

