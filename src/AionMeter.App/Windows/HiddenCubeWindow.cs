using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using AionMeter.App.Services;

namespace AionMeter.App.Windows;
internal sealed class HiddenCubeWindow : Window,IDisplayAppearance
{
    private readonly DockPanel _header;
    private readonly Border _glow;
    private readonly TextBlock _keys;
    private readonly StackPanel _keyRow;
    internal bool KeyStockVisible=>_keyRow.Visibility==Visibility.Visible;
    private readonly TextBlock _message;
    private readonly Image _mascot;
    private readonly BitmapSource _detectedMascot;
    private BitmapSource? _goneMascot;
    private bool _gone;
    internal string MessageText=>_message.Text;
    private readonly Func<(ulong? Count,bool Cached,long ConfirmedAt)> _keyStock;
    internal bool IsPreview {get;}
    internal bool AlertAnimationRunning=>_glow.HasAnimatedProperties;
    internal string KeyText=>_keys.Text;
    private static string L(string en,string de)=>ControlPanelSettingsPanel.L(en,de);
    internal HiddenCubeWindow(AppSettings settings,bool preview=false,Func<(ulong? Count,bool Cached,long ConfirmedAt)>? keyStock=null)
    {
        IsPreview=preview;_keyStock=keyStock??(()=>(null,false,0));
        Title="CatScratch · "+UiText.Current.Localize("Hidden cubes");
        Width=384;SizeToContent=SizeToContent.Height;MinWidth=320;MaxWidth=460;
        WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;
        ShowInTaskbar=false;ShowActivated=false;Topmost=true;Background=Brushes.Transparent;
        Resources.MergedDictionaries.Add(new(){Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative)});
        Foreground=ThemeColors.Brush("#EDF2F7");FontFamily=(FontFamily)FindResource("UiFont");AppIcon.Apply(this);
        var body=new StackPanel{Margin=new(5)};
        _header=new DockPanel{Height=22,Margin=new(4,0,4,3),Background=Brushes.Transparent};
        var close=new Button{Content="\uE711",Style=(Style)FindResource("IconButton"),Width=22,Height=22};
        close.Click+=(_,_)=>{if(!preview){settings.ShowHiddenCubeModule=false;settings.Save();}Close();};
        DockPanel.SetDock(close,Dock.Right);_header.Children.Add(close);
        var pin=WindowPinning.CreateButton(this,settings,"hidden-cubes");DockPanel.SetDock(pin,Dock.Right);_header.Children.Add(pin);
        _header.Children.Add(new Image{Source=AppIcon.Image,Width=13,Height=13,Margin=new(0,0,5,0)});
        _header.Children.Add(new TextBlock{Text=UiText.Current.Localize("Hidden cubes")+(preview?" · "+UiText.Current.Localize("Test alert"):""),FontSize=11,VerticalAlignment=VerticalAlignment.Center,Foreground=ThemeColors.Brush("#A9B8C7")});body.Children.Add(_header);
        var layers=new Grid();
        var content=new Grid{Margin=new(10,9,12,9)};
        content.ColumnDefinitions.Add(new(){Width=new GridLength(78)});content.ColumnDefinitions.Add(new());
        var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.UriSource=new Uri("pack://application:,,,/CatScratch;component/Assets/HiddenCube/cube-alert-mascot.png");bitmap.DecodePixelWidth=192;bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.EndInit();bitmap.Freeze();
        _detectedMascot=bitmap;
        _mascot=new Image{Source=bitmap,Width=72,Height=72,Margin=new(0,0,6,0),Stretch=Stretch.Uniform};content.Children.Add(_mascot);
        var copy=new StackPanel{VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(copy,1);content.Children.Add(copy);
        copy.Children.Add(new TextBlock{Text=L("HIDDEN CUBE","VERSTECKTER WÜRFEL"),FontSize=11,FontWeight=FontWeights.Bold,Foreground=ThemeColors.Brush("#C49AF5"),Margin=new(0,0,0,3)});
        _message=new TextBlock{Text=L("Cube detected!","Würfel entdeckt!"),FontSize=18,FontWeight=FontWeights.SemiBold,Foreground=ThemeColors.Brush("#EDF2F7"),Margin=new(0,0,0,7)};copy.Children.Add(_message);
        var keyRow=_keyRow=new StackPanel{Orientation=Orientation.Horizontal};
        keyRow.Children.Add(new System.Windows.Shapes.Path{Data=Geometry.Parse("M 7,1 A 4,4 0 1 0 7,9 A 4,4 0 1 0 7,1 M 4,9 L 0,13 L 2,15 L 4,13 L 6,15 L 8,13 L 6,11"),Width=14,Height=14,Stretch=Stretch.Uniform,Stroke=ThemeColors.Brush("#E4C17C"),StrokeThickness=1.6,Margin=new(0,0,6,0)});
        _keys=new TextBlock{FontSize=11,VerticalAlignment=VerticalAlignment.Center};keyRow.Children.Add(_keys);copy.Children.Add(keyRow);
        var backdrop=new Grid();
        var signature=new Controls.CatSignature{Width=98,Height=90,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Center};
        backdrop.Children.Add(signature);backdrop.Children.Add(content);
        layers.Children.Add(new Border{Background=ThemeColors.Brush("#F5191D23"),BorderBrush=ThemeColors.Brush("#414A56"),BorderThickness=new(1),CornerRadius=new(12),Child=backdrop});
        _glow=new Border{BorderBrush=ThemeColors.Brush("#C49AF5"),BorderThickness=new(1.3),CornerRadius=new(12),Opacity=.6,IsHitTestVisible=false,Effect=new DropShadowEffect{Color=Color.FromRgb(161,113,232),BlurRadius=10,ShadowDepth=0,Opacity=.4}};layers.Children.Add(_glow);body.Children.Add(layers);Content=body;
        IsVisibleChanged+=(_,_)=>Animate(IsVisible&&!_gone);Closed+=(_,_)=>Animate(false);RefreshKeyStock();
        if(settings.HiddenCubePositionSaved&&double.IsFinite(settings.HiddenCubeLeft)&&double.IsFinite(settings.HiddenCubeTop)){Left=settings.HiddenCubeLeft;Top=settings.HiddenCubeTop;}
        else{Left=SystemParameters.WorkArea.Left+Math.Max(0,(SystemParameters.WorkArea.Width-Width)/2);Top=SystemParameters.WorkArea.Top+SystemParameters.WorkArea.Height*.2;}
        Controls.DragAnywhere.Attach(this);
        Closed+=(_,_)=>{settings.HiddenCubeLeft=Left;settings.HiddenCubeTop=Top;settings.HiddenCubePositionSaved=true;settings.Save();};
    }
    private void Animate(bool active)
    {
        var pulse=new DoubleAnimation(.4,.85,TimeSpan.FromSeconds(1.2)){AutoReverse=true,RepeatBehavior=RepeatBehavior.Forever};Timeline.SetDesiredFrameRate(pulse,15);
        _glow.BeginAnimation(OpacityProperty,active?pulse:null);
    }
    internal void SetGone(bool gone)
    {
        _keyRow.Visibility=gone?Visibility.Collapsed:Visibility.Visible;
        _message.Text=gone?"Cube disappeared":L("Cube detected!","Würfel entdeckt!");
        _message.FontSize=18;
        if(gone && _goneMascot is null)
        {
            var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.UriSource=new Uri("pack://application:,,,/CatScratch;component/Assets/HiddenCube/cube-gone-mascot.png");bitmap.DecodePixelWidth=192;bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.EndInit();bitmap.Freeze();_goneMascot=bitmap;
        }
        _mascot.Source=gone?_goneMascot:_detectedMascot;
        if(_gone!=gone){_gone=gone;Animate(IsVisible&&!gone);}
    }
    internal void RefreshKeyStock()
    {
        var stock=_keyStock();
        var text=stock.Count is {} count?L("Keys (bound): ","Schlüssel (geb.): ")+(stock.Cached?"≈ ":"")+count.ToString("N0",UiText.Current.Culture):L("Key stock unknown","Schlüsselbestand unbekannt");
        if(_keys.Text==text)return;_keys.Text=text;
        _keys.Foreground=ThemeColors.Brush(stock.Count is null?"#A9B8C7":stock.Count==0?"#FFB454":"#E4C17C");
        _keys.ToolTip="Key: Hidden Cube (Bound)\n"+(stock.Cached?L("Last recorded stock; awaiting confirmation.","Zuletzt gespeicherter Bestand; Bestätigung steht aus."):stock.Count is null?L("Log in while capture is running to read the inventory.","Mit laufender Erfassung einloggen, um das Inventar zu lesen."):L("Server-confirmed inventory stock.","Vom Server bestätigter Inventarbestand."));
    }
    public void SetDisplayOnly(bool enabled)=>_header.Visibility=enabled?Visibility.Hidden:Visibility.Visible;
}
