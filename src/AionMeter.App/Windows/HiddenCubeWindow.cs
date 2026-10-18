using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AionMeter.App.Services;

namespace AionMeter.App.Windows;

internal sealed class HiddenCubeWindow : Window,IDisplayAppearance
{
    private readonly Border _shell;
    private readonly DockPanel _header;
    internal HiddenCubeWindow(AppSettings settings)
    {
        Title="CatScratch · "+UiText.Current.Localize("Hidden cubes");
        Width=300;SizeToContent=SizeToContent.Height;MinWidth=260;MaxWidth=420;
        WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;
        ShowInTaskbar=false;ShowActivated=false;Topmost=true;Background=Brushes.Transparent;
        Resources.MergedDictionaries.Add(new(){Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative)});
        Foreground=ThemeColors.Brush("#EDF2F7");FontFamily=(FontFamily)FindResource("UiFont");
        AppIcon.Apply(this);
        var body=new StackPanel();
        _header=new DockPanel {Margin=new(0,0,0,5)};
        var close=new Button {Content="\uE711",Style=(Style)FindResource("IconButton"),Width=22,Height=22};
        close.Click+=(_,_)=>{settings.ShowHiddenCubeModule=false;settings.Save();Close();};
        DockPanel.SetDock(close,Dock.Right);_header.Children.Add(close);
        var pin=WindowPinning.CreateButton(this,settings,"hidden-cubes");DockPanel.SetDock(pin,Dock.Right);_header.Children.Add(pin);
        _header.Children.Add(new Image {Source=AppIcon.Image,Width=14,Height=14,Margin=new(0,0,6,0)});
        _header.Children.Add(new TextBlock {Text=UiText.Current.Localize("Hidden cubes"),FontSize=12,VerticalAlignment=VerticalAlignment.Center,Foreground=ThemeColors.Brush("#C49AF5")});
        body.Children.Add(_header);
        body.Children.Add(new TextBlock {Text="HIDDEN CUBE DETECTED",FontSize=15,FontWeight=FontWeights.SemiBold,Foreground=ThemeColors.Brush("#EAC96F"),TextAlignment=TextAlignment.Center,TextWrapping=TextWrapping.Wrap,Margin=new(0,4,0,4)});
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
