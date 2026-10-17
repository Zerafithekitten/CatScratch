using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using AionMeter.App.Controls;
using AionMeter.App.Services;

namespace AionMeter.App.Windows;

/// <summary>Clickable settings access while the DPS display is disabled; capture remains independent.</summary>
internal sealed class MeterControlsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly TextBlock _status=new();
    private Border _backdrop=null!;
    private Grid? _header;
    private WrapPanel? _buttons;
    private Grid? _verticalActions;
    private Grid? _verticalFooter;
    private StackPanel? _caption;
    private FrameworkElement? _personalButton,_petButton;
    private UIElement[] _horizontalItems=[];
    private readonly Dictionary<FrameworkElement,(double Width,double Height,Thickness Margin,HorizontalAlignment Alignment)> _horizontalSizing=[];
    internal Grid? VerticalActions=>_verticalActions;
    private ControlsLayout? _layout;
    private ControlsAnchor? _anchor;
    private bool _aligning;
    private readonly Popup _updateNotice=new(){AllowsTransparency=true,StaysOpen=true,Placement=PlacementMode.Top};
    private bool _updateAvailable;
    internal Popup UpdateNotice=>_updateNotice;
    internal double BackgroundOpacity=>_backdrop.Opacity;
    internal Button SettingsButton { get; private set; }
    internal Button RestoreButton { get; }

    internal MeterControlsWindow(AppSettings settings,Action openSettings,Action restore,Action<double,double>? moved=null)
    {
        _settings=settings;
        Title="CatScratch";Width=280;Height=48;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;
        AllowsTransparency=true;Background=Brushes.Transparent;Topmost=true;ShowInTaskbar=false;ShowActivated=false;
        FontFamily=new("Segoe UI");Foreground=ThemeColors.Brush("#EDF2F7");
        Resources.MergedDictionaries.Add(new ResourceDictionary {Source=new Uri("/CatScratch;component/Theme/MeterTools.xaml",UriKind.Relative)});
        AppIcon.Apply(this);WindowPresentation.Attach(this);
        var header=new DockPanel {Margin=new(10,4,8,4)};
        SettingsButton=new Button {Content="\uE713",ToolTip=UiText.Current.TraySettings,Style=(Style)FindResource("IconButton"),Width=24,Height=24};
        SettingsButton.Click+=(_,_)=>openSettings();DockPanel.SetDock(SettingsButton,Dock.Right);header.Children.Add(SettingsButton);
        var pin=WindowPinning.CreateButton(this,settings,"controls");DockPanel.SetDock(pin,Dock.Right);header.Children.Add(pin);
        RestoreButton=new Button {Content="\uE7B3",ToolTip=UiText.Current.Localize("Show DPS meter")+" · "+settings.HotkeyToggleOverlay,Style=(Style)FindResource("IconButton"),Width=24,Height=24};
        RestoreButton.Click+=(_,_)=>restore();DockPanel.SetDock(RestoreButton,Dock.Right);header.Children.Add(RestoreButton);
        var paw=new Image {Source=AppIcon.Image,Width=22,Height=22,Margin=new(0,0,8,0),VerticalAlignment=VerticalAlignment.Center};
        DockPanel.SetDock(paw,Dock.Left);header.Children.Add(paw);
        var label=new StackPanel {VerticalAlignment=VerticalAlignment.Center};
        label.Children.Add(new TextBlock {Text="CatScratch",Foreground=ThemeColors.Brush("#C49AF5"),FontSize=13,FontWeight=FontWeights.SemiBold});
        _status.Foreground=ThemeColors.Brush("#A9B8C7");_status.FontSize=10;label.Children.Add(_status);
        header.Children.Add(label);
        SetFrame(header);
        // This is the settings access point, so it deliberately stays outside global click-through registration.
        SourceInitialized+=(_,_)=>NativeMethods.SetExStyle(new WindowInteropHelper(this).Handle,
            ShowInTaskbar?0x00040000:NativeMethods.WS_EX_TOOLWINDOW,ShowInTaskbar?NativeMethods.WS_EX_TOOLWINDOW:0x00040000);
        DragAnywhere.Attach(this,dropped:()=>
        {
            settings.ControlsAnchor=ControlsAnchor.Free;
            settings.ControlsLeft=Left;settings.ControlsTop=Top;settings.Save();moved?.Invoke(Left,Top);
        });
        Loaded+=(_,_)=>AlignToScreen();SizeChanged+=(_,_)=>AlignToScreen();
        LocationChanged+=(_,_)=>SyncUpdateNotice();SizeChanged+=(_,_)=>SyncUpdateNotice();
        IsVisibleChanged+=(_,_)=>SyncUpdateNotice();
        Closed+=(_,_)=>{_updateAvailable=false;_updateNotice.IsOpen=false;_updateNotice.Child=null;};
        RefreshText();RestorePosition();
    }

    internal void AttachControls(OverlayWindow overlay,FrameworkElement header,Panel buttons,FrameworkElement update,Button settingsButton)
    {
        Resources.MergedDictionaries.Add(overlay.Resources);
        DataContext=overlay.DataContext;
        ((Panel)RestoreButton.Parent).Children.Remove(RestoreButton);
        buttons.Children.Add(RestoreButton);
        SettingsButton=settingsButton;
        _verticalActions=new Grid {Margin=new(5,1,5,0),Visibility=Visibility.Collapsed};
        for(var i=0;i<4;i++){_verticalActions.ColumnDefinitions.Add(new(){Width=new(1,GridUnitType.Star)});_verticalActions.RowDefinitions.Add(new(){Height=new(22)});}
        _verticalFooter=new Grid {Margin=new(5,2,5,4),Visibility=Visibility.Collapsed};
        _verticalFooter.ColumnDefinitions.Add(new(){Width=GridLength.Auto});_verticalFooter.ColumnDefinitions.Add(new(){Width=new(1,GridUnitType.Star)});
        var root=new StackPanel();root.Children.Add(header);root.Children.Add(buttons);root.Children.Add(_verticalActions);root.Children.Add(_verticalFooter);
        var updateButton=new Button {Content="Update!",Width=72,Height=26,FontFamily=new("Segoe UI"),FontSize=12,
            Style=(Style)FindResource("GhostButton"),Foreground=ThemeColors.Brush("#C49AF5"),ToolTip=UiText.Current.TipUpdate};
        updateButton.Click+=(_,_)=>overlay.OpenReadyUpdate();
        _updateNotice.Child=updateButton;_updateNotice.PlacementTarget=this;
        SetUpdateAvailable(update.Visibility==Visibility.Visible);
        SetFrame(root);
        _header=(Grid)header;_buttons=(WrapPanel)buttons;
        foreach(var button in _buttons.Children.OfType<System.Windows.Controls.Primitives.ButtonBase>())
        {
            if(Equals(button.Tag,"damage-mode"))continue;
            button.Width=20;button.Height=20;button.Margin=new(0);
        }
        foreach(var slider in _buttons.Children.OfType<Slider>())slider.Width=40;
        foreach(var button in _header.Children.OfType<Button>()){button.Width=20;button.Height=20;}
        _caption=_header.Children.OfType<StackPanel>().Single(p=>p.Name=="ShellCaption");
        _personalButton=_caption.Children.OfType<FrameworkElement>().Single(p=>p.Name=="PersonalCombatButton");
        _petButton=_caption.Children.OfType<FrameworkElement>().Single(p=>p.Name=="PetTrackerButton");
        _horizontalItems=_buttons.Children.Cast<UIElement>().ToArray();
        foreach(var item in _horizontalItems.OfType<FrameworkElement>().Concat([_personalButton,_petButton]))
            _horizontalSizing[item]=(item.Width,item.Height,item.Margin,item.HorizontalAlignment);
        SizeToContent=SizeToContent.Height;Height=double.NaN;ApplyLayout();
    }

    internal void SetUpdateAvailable(bool available){_updateAvailable=available;SyncUpdateNotice();}
    private void SyncUpdateNotice()
    {
        _updateNotice.HorizontalOffset=(ActualWidth-72)/2;
        _updateNotice.VerticalOffset=-4;
        _updateNotice.IsOpen=_updateAvailable && IsVisible && WindowState!=WindowState.Minimized;
        // Recalculate screen placement after a parent move without touching the panel's bounds.
        if(_updateNotice.IsOpen){var offset=_updateNotice.HorizontalOffset;_updateNotice.HorizontalOffset=offset+.01;_updateNotice.HorizontalOffset=offset;}
    }

    internal void RefreshText()
    {
        ApplyAppearance();
        SettingsButton.ToolTip=UiText.Current.TraySettings;
        RestoreButton.ToolTip=(_settings.OverlayVisible?UiText.Current.TrayHide:UiText.Current.Localize("Show DPS meter"))+" · "+_settings.HotkeyToggleOverlay;
        _status.Text=UiText.Current.Localize("DPS meter hidden");
    }

    private void SetFrame(UIElement content)
    {
        var root=new Grid();
        _backdrop=new Border {Background=ThemeColors.Brush("#20242A"),CornerRadius=new(8)};
        root.Children.Add(_backdrop);
        root.Children.Add(new Border {Child=content,BorderBrush=ThemeColors.Brush("#414A56"),BorderThickness=new(1),CornerRadius=new(8),Background=Brushes.Transparent});
        Content=root;ApplyAppearance();
    }

    internal void ApplyAppearance()
    {
        Opacity=Math.Clamp(_settings.WindowOpacity,.4,1);
        _backdrop.Opacity=Math.Clamp(_settings.BackgroundOpacity,0,1);
        ApplyLayout();
    }

    private void ApplyLayout()
    {
        if(_header is null || _buttons is null)return;
        if(_layout!=_settings.ControlsLayout)
        {
            _layout=_settings.ControlsLayout;
            var vertical=_layout==ControlsLayout.Vertical;
            Width=vertical?132:400;
            _header.Margin=vertical?new Thickness(4,1,4,0):new Thickness(8,2,6,0);
            var paw=(Image)_caption!.Children[0];
            paw.Width=paw.Height=vertical?16:22;paw.Margin=vertical?new Thickness(0,0,4,0):new Thickness(0,0,7,0);
            var branding=(StackPanel)_caption.Children[1];
            ((TextBlock)branding.Children[0]).FontSize=vertical?12:16;
            ((TextBlock)branding.Children[1]).FontSize=vertical?8:9;
            var clock=_header.Children.OfType<StackPanel>().Single(p=>p.Name=="ShellClockPanel");
            foreach(var text in clock.Children.OfType<TextBlock>()){text.FontSize=vertical?8:text==clock.Children[0]?9:10;text.Margin=vertical?new Thickness(2,0,2,0):new Thickness(4,0,4,0);}
            _header.Height=vertical?double.NaN:34;
            _header.RowDefinitions.Clear();
            if(vertical){_header.RowDefinitions.Add(new(){Height=GridLength.Auto});_header.RowDefinitions.Add(new(){Height=GridLength.Auto});}
            foreach(FrameworkElement child in _header.Children)
            {
                var caption=child.Name=="ShellCaption";
                Grid.SetRow(child,vertical&&!caption&&child.Name!="ShellHideButton"?1:0);
                if(caption){Grid.SetColumnSpan(child,vertical?5:1);child.LayoutTransform=Transform.Identity;}
                if(child.Name=="ShellClockPanel"){Grid.SetColumn(child,vertical?0:1);Grid.SetColumnSpan(child,vertical?3:1);}
                if(child.Name=="ShellClickButton")Grid.SetColumn(child,vertical?3:2);
                if(child.Name=="ShellSettingsButton")Grid.SetColumn(child,vertical?4:3);
                if(child.Name=="ShellPin")Grid.SetColumn(child,vertical?5:4);
            }
            ArrangeActions(vertical);
        }
        if(_anchor!=_settings.ControlsAnchor){_anchor=_settings.ControlsAnchor;AlignToScreen();}
    }

    private static void Detach(FrameworkElement element)
    {
        if(element.Parent is Panel parent)parent.Children.Remove(element);
    }

    private void ArrangeActions(bool vertical)
    {
        if(_buttons is null || _verticalActions is null || _verticalFooter is null || _caption is null)return;
        foreach(var item in _horizontalSizing.Keys)Detach(item);
        _verticalActions.Children.Clear();_verticalFooter.Children.Clear();
        _buttons.Visibility=vertical?Visibility.Collapsed:Visibility.Visible;
        _verticalActions.Visibility=_verticalFooter.Visibility=vertical?Visibility.Visible:Visibility.Collapsed;
        if(!vertical)
        {
            foreach(var (item,size) in _horizontalSizing){item.Width=size.Width;item.Height=size.Height;item.Margin=size.Margin;item.HorizontalAlignment=size.Alignment;}
            foreach(var item in _horizontalItems)_buttons.Children.Add(item);
            _caption.Children.Add(_personalButton!);_caption.Children.Add(_petButton!);
            return;
        }
        FrameworkElement Item(string name)=>_horizontalItems.OfType<FrameworkElement>().Single(c=>c.Name==name);
        var actions=new[]{Item("DeathRecapButton"),_personalButton!,_petButton!,RestoreButton,
            Item("BossRecordsButton"),Item("LastPlayedButton"),Item("CompactHistory"),Item("DetailsLink"),
            Item("ChatExportButton"),Item("PatchNotesButton"),Item("SaveLastFightButton"),Item("CheckUpdatesButton"),
            Item("BossTimersLink"),Item("RestartButton"),Item("LockButton")};
        for(var column=0;column<3;column++)
        {
            var separator=new Border {Width=1,Background=ThemeColors.Brush("#414A56"),Opacity=.65,
                HorizontalAlignment=HorizontalAlignment.Right,Margin=new(0,2,0,2),IsHitTestVisible=false};
            Grid.SetColumn(separator,column);Grid.SetRowSpan(separator,4);_verticalActions.Children.Add(separator);
        }
        for(var i=0;i<actions.Length;i++)
        {
            var action=actions[i];action.Width=22;action.Height=20;action.Margin=new(0);action.HorizontalAlignment=HorizontalAlignment.Center;
            Grid.SetColumn(action,i%4);Grid.SetRow(action,i/4);_verticalActions.Children.Add(action);
        }
        var mode=_horizontalItems.OfType<FrameworkElement>().Single(c=>Equals(c.Tag,"damage-mode"));
        mode.Height=22;mode.Margin=new(0);Grid.SetColumn(mode,0);Grid.SetRow(mode,0);_verticalFooter.Children.Add(mode);
        var slider=_horizontalItems.OfType<Slider>().Single();
        slider.Width=double.NaN;slider.Margin=new(8,0,0,0);slider.HorizontalAlignment=HorizontalAlignment.Stretch;
        Grid.SetColumn(slider,1);Grid.SetRow(slider,0);_verticalFooter.Children.Add(slider);
    }

    internal static Rect AlignedBounds(Rect bounds,Rect area,ControlsAnchor anchor)
    {
        if(anchor==ControlsAnchor.Free)return WindowPresentation.Clamp(bounds,area);
        var x=anchor is ControlsAnchor.TopLeft or ControlsAnchor.CenterLeft or ControlsAnchor.BottomLeft?area.Left:
            anchor is ControlsAnchor.TopRight or ControlsAnchor.CenterRight or ControlsAnchor.BottomRight?area.Right-bounds.Width:area.Left+(area.Width-bounds.Width)/2;
        var y=anchor is ControlsAnchor.TopLeft or ControlsAnchor.TopCenter or ControlsAnchor.TopRight?area.Top:
            anchor is ControlsAnchor.BottomLeft or ControlsAnchor.BottomCenter or ControlsAnchor.BottomRight?area.Bottom-bounds.Height:area.Top+(area.Height-bounds.Height)/2;
        return WindowPresentation.Clamp(new Rect(x,y,bounds.Width,bounds.Height),area);
    }

    private void AlignToScreen()
    {
        if(!IsLoaded || _aligning || _settings.ControlsAnchor==ControlsAnchor.Free)return;
        _aligning=true;
        try
        {
            var bounds=WindowPresentation.PhysicalBounds(this);if(bounds.IsEmpty)return;
            var area=System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle).Bounds;
            var aligned=AlignedBounds(bounds,new Rect(area.X,area.Y,area.Width,area.Height),_settings.ControlsAnchor);
            var from=PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice??Matrix.Identity;
            var delta=from.Transform(new Vector(aligned.X-bounds.X,aligned.Y-bounds.Y));
            if(Math.Abs(delta.X)>.1)Left+=delta.X;if(Math.Abs(delta.Y)>.1)Top+=delta.Y;
            _settings.ControlsLeft=Left;_settings.ControlsTop=Top;
        }
        finally{_aligning=false;}
    }

    internal void RestorePosition()
    {
        Left=_settings.ControlsLeft??_settings.OverlayLeft;Top=_settings.ControlsTop??Math.Max(0,_settings.OverlayTop-100);WindowPresentation.Constrain(this);
        AlignToScreen();
    }
}
