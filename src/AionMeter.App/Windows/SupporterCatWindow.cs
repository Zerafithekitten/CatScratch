using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using AionMeter.App.Services;

namespace AionMeter.App.Windows;

internal sealed class SupporterCatWindow : Window,IDisplayAppearance,IMapPresentation
{
    private readonly AppSettings _settings;
    private readonly StackPanel _root;
    internal string PackFingerprint {get;}
    private readonly DockPanel _header;
    private readonly Image _image;
    private readonly BitmapSource[] _frames;
    private readonly DispatcherTimer _timer;
    private int _frame;
    private CompanionUpdateBubble? _updateBubble;
    private string? _updateTag,_announcedTag;
    private Action? _openUpdate;
    private readonly TranslateTransform _attention=new();
    internal CompanionUpdateBubble? UpdateBubble=>_updateBubble;
    internal bool UpdateNoticeVisible=>IsVisible && _updateBubble?.IsVisible==true;
    internal bool AttentionRunning=>_attention.HasAnimatedProperties;
    internal event Action? UpdateNoticeChanged;
    internal static double NormalizeScale(double value)=>double.IsFinite(value)?Math.Clamp(value,.5,2):1;
    internal double CatSize=>_image.Width;
    internal bool AnimationRunning=>_timer.IsEnabled;
    internal int FrameIndex=>_frame;
    internal int FrameCount=>_frames.Length;
    internal SupporterCatWindow(AppSettings settings,InstalledSupporterPack pack)
    {
        _settings=settings;PackFingerprint=pack.Pack.Fingerprint;
        var mascot=pack.Pack.Manifest.Mascot??throw new InvalidDataException("This package contains no cat.");
        using(var stream=new MemoryStream(pack.Pack.MascotImage!))
        {
            var decoder=BitmapDecoder.Create(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);
            var sheet=decoder.Frames[0];sheet.Freeze();
            _frames=Enumerable.Range(0,mascot.Frames).Select(i=>
            {
                var frame=new CroppedBitmap(sheet,new Int32Rect(i%mascot.Columns*mascot.FrameWidth,i/mascot.Columns*mascot.FrameHeight,mascot.FrameWidth,mascot.FrameHeight));
                frame.Freeze();return (BitmapSource)frame;
            }).ToArray();
        }
        Title="CatScratch · "+ControlPanelSettingsPanel.L("Supporter cat","Supporter-Katze");
        WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;
        ShowInTaskbar=false;ShowActivated=false;Topmost=true;Background=Brushes.Transparent;
        SizeToContent=SizeToContent.WidthAndHeight;
        Resources.MergedDictionaries.Add(new(){Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative)});
        var size=160*NormalizeScale(settings.SupporterCatScale);
        var root=_root=new StackPanel{Width=size,Background=Brushes.Transparent};
        _header=new DockPanel{Height=24,Background=ThemeColors.Brush("#DF20242A")};
        var close=new Button{Content="\uE711",Style=(Style)FindResource("IconButton"),Width=22,Height=22,ToolTip=ControlPanelSettingsPanel.L("Hide cat","Katze ausblenden")};
        close.Click+=(_,_)=>{settings.ShowSupporterCat=false;settings.Save();Close();};
        DockPanel.SetDock(close,Dock.Right);_header.Children.Add(close);
        var pin=WindowPinning.CreateButton(this,settings,"supporter-cat");DockPanel.SetDock(pin,Dock.Right);_header.Children.Add(pin);
        _header.Children.Add(new Image{Source=AppIcon.Image,Width=14,Height=14,Margin=new(5,0,5,0)});
        _header.Children.Add(new TextBlock{Text=ControlPanelSettingsPanel.L("Cat companion","Katzenbegleiter"),FontSize=10,TextTrimming=TextTrimming.CharacterEllipsis,Foreground=ThemeColors.Brush("#C49AF5"),VerticalAlignment=VerticalAlignment.Center});
        root.Children.Add(_header);
        _image=new Image{Width=size,Height=size,Source=_frames[0],Stretch=Stretch.Uniform,RenderTransform=_attention};root.Children.Add(_image);Content=root;
        _timer=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(mascot.FrameMilliseconds)};
        _timer.Tick+=(_,_)=>SetFrame((_frame+1)%_frames.Length);
        IsVisibleChanged+=(_,_)=>{SetMapPresentationSuspended(MapHudSuppression.IsSuppressed(this));RefreshUpdateNotice();};
        LocationChanged+=(_,_)=>_updateBubble?.Follow(this);
        SizeChanged+=(_,_)=>_updateBubble?.Follow(this);
        if(settings.SupporterCatPositionSaved&&double.IsFinite(settings.SupporterCatLeft)&&double.IsFinite(settings.SupporterCatTop))
        {Left=settings.SupporterCatLeft;Top=settings.SupporterCatTop;}
        else{Left=SystemParameters.WorkArea.Right-size-20;Top=SystemParameters.WorkArea.Bottom-size-50;}
        Controls.DragAnywhere.Attach(this);
        Closed+=(_,_)=>{_timer.Stop();_attention.BeginAnimation(TranslateTransform.YProperty,null);_updateBubble?.Close();_updateBubble=null;UpdateNoticeChanged?.Invoke();settings.SupporterCatLeft=Left;settings.SupporterCatTop=Top;settings.SupporterCatPositionSaved=true;settings.Save();};
    }
    internal void ApplyPreferences()
    {
        var size=160*NormalizeScale(_settings.SupporterCatScale);
        if(_image.Width==size)return;
        _root.Width=size;_image.Width=size;_image.Height=size;
        SizeToContent=SizeToContent.WidthAndHeight;
        WindowPresentation.Constrain(this);
    }
    public void SetMapPresentationSuspended(bool suspended)
    {
        if(IsVisible&&!suspended)_timer.Start();
        else{_timer.Stop();_attention.BeginAnimation(TranslateTransform.YProperty,null);}
    }
    internal void RecoverPosition(Window owner,Action? completed=null)
    {
        WindowPresentation.RecoverTo(this,owner);
        Dispatcher.BeginInvoke(()=>
        {
            if(!IsLoaded)return;
            WindowPresentation.Constrain(this);
            _settings.SupporterCatLeft=Left;_settings.SupporterCatTop=Top;_settings.SupporterCatPositionSaved=true;
            _updateBubble?.Follow(this);completed?.Invoke();_settings.Save();
        });
    }
    internal void SetUpdateNotice(string? tag,Action openUpdate)
    {
        _updateTag=tag;_openUpdate=openUpdate;RefreshUpdateNotice();
    }
    private void RefreshUpdateNotice()
    {
        if(!IsVisible || _updateTag is null)
        {
            _updateBubble?.Hide();_attention.BeginAnimation(TranslateTransform.YProperty,null);
            UpdateNoticeChanged?.Invoke();return;
        }
        _updateBubble??=new CompanionUpdateBubble(this,()=>_openUpdate?.Invoke());
        _updateBubble.Follow(this);
        if(!_updateBubble.IsVisible)_updateBubble.Show();
        _updateBubble.Follow(this);
        if(!MapHudSuppression.IsSuppressed(this)&&_announcedTag!=_updateTag)
        {
            _announcedTag=_updateTag;
            var bounce=new DoubleAnimation(0,-4,TimeSpan.FromMilliseconds(350)){AutoReverse=true,RepeatBehavior=new RepeatBehavior(2),FillBehavior=FillBehavior.Stop,EasingFunction=new SineEase {EasingMode=EasingMode.EaseInOut}};
            bounce.Completed+=(_,_)=>_attention.BeginAnimation(TranslateTransform.YProperty,null);
            _attention.BeginAnimation(TranslateTransform.YProperty,bounce);
        }
        UpdateNoticeChanged?.Invoke();
    }
    internal void SetFrame(int frame){_frame=Math.Clamp(frame,0,_frames.Length-1);_image.Source=_frames[_frame];}
    public void SetDisplayOnly(bool enabled)=>_header.Visibility=enabled?Visibility.Hidden:Visibility.Visible;
}
