using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using AionMeter.App.Services;

namespace AionMeter.App.Windows;

internal sealed class HiddenCubeWindow : Window,IDisplayAppearance
{
    private readonly Border _shell;
    private readonly DockPanel _header;
    internal bool IsPreview {get;}
    internal HiddenCubeWindow(AppSettings settings,bool preview=false)
    {
        IsPreview=preview;
        Title="CatScratch · "+UiText.Current.Localize("Hidden cubes");
        Width=340;SizeToContent=SizeToContent.Height;MinWidth=300;MaxWidth=460;
        WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;
        ShowInTaskbar=false;ShowActivated=false;Topmost=true;Background=Brushes.Transparent;
        Resources.MergedDictionaries.Add(new(){Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative)});
        Foreground=ThemeColors.Brush("#EDF2F7");FontFamily=(FontFamily)FindResource("UiFont");
        AppIcon.Apply(this);
        var body=new StackPanel();
        _header=new DockPanel {Margin=new(0,0,0,5)};
        var close=new Button {Content="\uE711",Style=(Style)FindResource("IconButton"),Width=22,Height=22};
        close.Click+=(_,_)=>{if(!preview){settings.ShowHiddenCubeModule=false;settings.Save();}Close();};
        DockPanel.SetDock(close,Dock.Right);_header.Children.Add(close);
        var pin=WindowPinning.CreateButton(this,settings,"hidden-cubes");DockPanel.SetDock(pin,Dock.Right);_header.Children.Add(pin);
        _header.Children.Add(new Image {Source=AppIcon.Image,Width=14,Height=14,Margin=new(0,0,6,0)});
        _header.Children.Add(new TextBlock {Text=UiText.Current.Localize("Hidden cubes")+(preview?" · "+UiText.Current.Localize("Test alert"):""),FontSize=12,VerticalAlignment=VerticalAlignment.Center,Foreground=ThemeColors.Brush("#C49AF5")});
        body.Children.Add(_header);
        var warning=new Grid {Margin=new(0,3,0,3)};
        warning.ColumnDefinitions.Add(new ColumnDefinition {Width=GridLength.Auto});warning.ColumnDefinitions.Add(new ColumnDefinition());warning.ColumnDefinitions.Add(new ColumnDefinition {Width=GridLength.Auto});
        var red=ThemeColors.Brush("#FF5F63");
        var message=new TextBlock {Text="HIDDEN CUBE DETECTED",FontSize=16,FontWeight=FontWeights.Bold,Foreground=red,TextAlignment=TextAlignment.Center,TextWrapping=TextWrapping.Wrap,VerticalAlignment=VerticalAlignment.Center};
        Grid.SetColumn(message,1);warning.Children.Add(message);
        var markers=new[]{new TextBlock(),new TextBlock()};
        for(var n=0;n<markers.Length;n++){var marker=markers[n];marker.Text="!";marker.FontSize=27;marker.FontWeight=FontWeights.Bold;marker.Foreground=red;marker.Margin=new(5,0,5,0);marker.VerticalAlignment=VerticalAlignment.Center;Grid.SetColumn(marker,n*2);warning.Children.Add(marker);}
        void Animate(bool active)
        {
            foreach(var marker in markers)
            {
                var pulse=new DoubleAnimation(.45,1,TimeSpan.FromSeconds(.65)){AutoReverse=true,RepeatBehavior=RepeatBehavior.Forever};
                Timeline.SetDesiredFrameRate(pulse,20);
                marker.BeginAnimation(OpacityProperty,active?pulse:null);
            }
        }
        IsVisibleChanged+=(_,_)=>Animate(IsVisible);Closed+=(_,_)=>Animate(false);
        body.Children.Add(warning);
        _shell=new Border {Child=body,Background=ThemeColors.Brush("#F0191D23"),BorderBrush=ThemeColors.Brush("#414A56"),BorderThickness=new(1),CornerRadius=new(7),Padding=new(10,6,8,7)};
        Content=_shell;
        if(settings.HiddenCubePositionSaved && double.IsFinite(settings.HiddenCubeLeft) && double.IsFinite(settings.HiddenCubeTop)){Left=settings.HiddenCubeLeft;Top=settings.HiddenCubeTop;}
        else {Left=SystemParameters.WorkArea.Left+Math.Max(0,(SystemParameters.WorkArea.Width-Width)/2);Top=SystemParameters.WorkArea.Top+SystemParameters.WorkArea.Height*.2;}
        Controls.DragAnywhere.Attach(this);
        Closed+=(_,_)=>{settings.HiddenCubeLeft=Left;settings.HiddenCubeTop=Top;settings.HiddenCubePositionSaved=true;settings.Save();};
    }
    public void SetDisplayOnly(bool enabled)
    {
        _header.Visibility=enabled?Visibility.Collapsed:Visibility.Visible;
        _shell.BorderThickness=new(enabled?0:1);
    }
}
