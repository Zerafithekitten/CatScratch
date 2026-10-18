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
    private StackPanel? _layoutRoot;
    private FrameworkElement? _personalButton,_petButton;
    private UIElement[] _horizontalItems=[];
    private readonly Dictionary<FrameworkElement,(double Width,double Height,Thickness Margin,HorizontalAlignment Alignment)> _horizontalSizing=[];
    internal Grid? VerticalActions=>_verticalActions;
    internal bool IsVerticalRail=>_settings.ControlsLayout==ControlsLayout.Vertical;
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
        UseLayoutRounding=true;SnapsToDevicePixels=true;
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
        _verticalActions=new Grid {Margin=new(3,1,3,0),Visibility=Visibility.Collapsed};
        for(var i=0;i<4;i++){_verticalActions.ColumnDefinitions.Add(new(){Width=new(1,GridUnitType.Star)});_verticalActions.RowDefinitions.Add(new(){Height=new(18)});}
        _verticalFooter=new Grid {Margin=new(4,1,4,3),Visibility=Visibility.Collapsed};
        _verticalFooter.ColumnDefinitions.Add(new(){Width=GridLength.Auto});_verticalFooter.ColumnDefinitions.Add(new(){Width=new(1,GridUnitType.Star)});
        var root=_layoutRoot=new StackPanel();root.Children.Add(header);root.Children.Add(buttons);root.Children.Add(_verticalActions);root.Children.Add(_verticalFooter);
        var updateButton=CreateUpdateNoticeButton(this);
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

    internal static Button CreateUpdateNoticeButton(FrameworkElement resources)=>new()
    {
        Content="Update!",Width=72,Height=26,Padding=new Thickness(8,2,8,2),
        FontFamily=new("Segoe UI"),FontSize=12,FontWeight=FontWeights.SemiBold,
        Style=(Style)resources.FindResource("GhostButton"),
        Background=ThemeColors.Brush("#223A2D"),BorderBrush=ThemeColors.Brush("#4FC18A"),Foreground=ThemeColors.Brush("#A4E8BD"),
        UseLayoutRounding=true,SnapsToDevicePixels=true,ToolTip=UiText.Current.TipUpdate
    };

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
            var vertical=_layout==ControlsLayout.Grid;
            var rail=_layout==ControlsLayout.Vertical;
            Width=vertical?112:rail?52:316;
            _layoutRoot!.Orientation=rail?Orientation.Horizontal:Orientation.Vertical;
            _header.LayoutTransform=rail?new RotateTransform(90):Transform.Identity;
            _header.Width=rail?304:double.NaN;
            _header.Margin=rail?new(1,2,0,0):new(3,1,3,0);_header.Height=vertical?40:26;
            var paw=(Image)_caption!.Children[0];paw.Width=paw.Height=13;paw.Margin=new(0,0,3,0);
            paw.RenderTransformOrigin=new(.5,.5);paw.RenderTransform=Transform.Identity;
            _caption.Orientation=rail?Orientation.Vertical:Orientation.Horizontal;
            _caption.LayoutTransform=rail?new RotateTransform(-90):Transform.Identity;
            paw.Margin=rail?new(0,0,0,2):new(0,0,3,0);
            var branding=(StackPanel)_caption.Children[1];
            var title=(TextBlock)branding.Children[0];
            title.FontSize=rail?9:10.5;title.TextAlignment=rail?TextAlignment.Center:TextAlignment.Left;
            title.LineHeight=rail?8.5:double.NaN;
            title.LineStackingStrategy=rail?LineStackingStrategy.BlockLineHeight:LineStackingStrategy.MaxHeight;
            var version=(TextBlock)branding.Children[1];version.FontSize=rail?6:7;
            version.Width=rail?24:double.NaN;version.TextWrapping=rail?TextWrapping.Wrap:TextWrapping.NoWrap;
            version.TextAlignment=rail?TextAlignment.Center:TextAlignment.Left;
            var clock=_header.Children.OfType<StackPanel>().Single(p=>p.Name=="ShellClockPanel");
            clock.Visibility=rail?Visibility.Collapsed:Visibility.Visible;
            foreach(var text in clock.Children.OfType<TextBlock>()){text.FontSize=7.5;text.Margin=new(1,0,1,0);}
            _header.ColumnDefinitions.Clear();_header.RowDefinitions.Clear();
            if(vertical)
            {
                _header.RowDefinitions.Add(new(){Height=new(22)});_header.RowDefinitions.Add(new(){Height=new(18)});
                _header.ColumnDefinitions.Add(new(){Width=new(1,GridUnitType.Star)});
                for(var i=0;i<3;i++)_header.ColumnDefinitions.Add(new(){Width=new(18)});
            }
            else
            {
                for(var i=0;i<8;i++)_header.ColumnDefinitions.Add(new(){Width=i==3?new(1,GridUnitType.Star):GridLength.Auto});
            }
            foreach(FrameworkElement child in _header.Children)
            {
                Grid.SetRow(child,0);Grid.SetColumnSpan(child,1);
                switch(child.Name)
                {
                    case "ShellCaption":Grid.SetColumn(child,0);Grid.SetColumnSpan(child,vertical?3:1);break;
                    case "ShellClockPanel":Grid.SetRow(child,vertical?1:0);Grid.SetColumn(child,vertical?0:1);break;
                    case "ShellClickButton":Grid.SetRow(child,vertical?1:0);Grid.SetColumn(child,vertical?1:4);break;
                    case "ShellSettingsButton":Grid.SetRow(child,vertical?1:0);Grid.SetColumn(child,vertical?2:5);break;
                    case "ShellPin":Grid.SetRow(child,vertical?1:0);Grid.SetColumn(child,vertical?3:6);break;
                    case "ShellHideButton":Grid.SetColumn(child,vertical?3:7);break;
                }
                if(child is Button button){button.Width=button.Height=18;button.Margin=new(0);button.Padding=new(0);button.RenderTransformOrigin=new(.5,.5);button.RenderTransform=rail?new RotateTransform(-90):Transform.Identity;}
                if(child is ContentControl {Content:Button pin}){child.Width=18;pin.Width=pin.Height=18;pin.Margin=new(0);pin.Padding=new(0);pin.RenderTransformOrigin=new(.5,.5);pin.RenderTransform=rail?new RotateTransform(-90):Transform.Identity;}
            }
            ArrangeActions(vertical,rail);
        }
        if(_anchor!=_settings.ControlsAnchor){_anchor=_settings.ControlsAnchor;AlignToScreen();}
    }

    private static void Detach(FrameworkElement element)
    {
        if(element.Parent is Panel parent)parent.Children.Remove(element);
    }

    private void ArrangeActions(bool vertical,bool rail)
    {
        if(_buttons is null || _verticalActions is null || _verticalFooter is null || _caption is null || _header is null)return;
        foreach(var item in _horizontalSizing.Keys)Detach(item);
        _buttons.Children.Clear();_verticalActions.Children.Clear();_verticalFooter.Children.Clear();
        _buttons.Visibility=vertical?Visibility.Collapsed:Visibility.Visible;
        _verticalActions.Visibility=_verticalFooter.Visibility=vertical?Visibility.Visible:Visibility.Collapsed;
        _buttons.Orientation=rail?Orientation.Vertical:Orientation.Horizontal;
        _buttons.Margin=rail?new(0,2,2,2):new(2,0,2,2);_buttons.Height=rail?double.NaN:20;_buttons.Width=rail?20:double.NaN;
        FrameworkElement Item(string name)=>_horizontalItems.OfType<FrameworkElement>().Single(c=>c.Name==name);
        // Matching functional columns vertically and matching groups from left to right horizontally.
        var groups=new[]{
            new[]{Item("DeathRecapButton"),_personalButton!,_petButton!,Item("ChecklistButton")},
            new[]{Item("CompactHistory"),Item("DetailsLink"),Item("BossRecordsButton"),Item("LastPlayedButton")},
            new[]{RestoreButton,Item("BossTimersLink"),Item("SaveLastFightButton"),Item("ChatExportButton")},
            new[]{Item("RestartButton"),Item("LockButton"),Item("PatchNotesButton"),Item("CheckUpdatesButton")}
        };
        for(var column=0;column<groups.Length;column++)
        {
            foreach(var action in groups[column])
            {
                action.Width=vertical?20:18;action.Height=18;action.Margin=new(0);action.HorizontalAlignment=HorizontalAlignment.Center;
                if(action is Control control)control.Padding=new(0);
                if(vertical){Grid.SetColumn(action,column);Grid.SetRow(action,Array.IndexOf(groups[column],action));_verticalActions.Children.Add(action);}
                else _buttons.Children.Add(action);
            }
            if(column<3)
            {
                var separator=new Border {Width=1,Background=ThemeColors.Brush("#414A56"),Opacity=.8,IsHitTestVisible=false};
                if(vertical)
                {
                    separator.HorizontalAlignment=HorizontalAlignment.Right;separator.Margin=new(0,1,0,1);
                    Grid.SetColumn(separator,column);Grid.SetRowSpan(separator,4);_verticalActions.Children.Add(separator);
                }
                else
                {
                    separator.Width=rail?12:1;separator.Height=rail?1:12;separator.Margin=new(3);
                    _buttons.Children.Add(separator);
                }
            }
        }
        var mode=(Button)_horizontalItems.OfType<FrameworkElement>().Single(c=>Equals(c.Tag,"damage-mode"));
        mode.RenderTransform=Transform.Identity;mode.Width=34;mode.MinWidth=0;mode.Height=18;mode.FontSize=9;mode.Padding=new(2,0,2,0);mode.Margin=new(0);
        mode.LayoutTransform=rail?new RotateTransform(-90):Transform.Identity;
        mode.ContentTemplate=null;
        if(rail)
        {
            mode.Width=18;mode.Height=44;mode.Padding=new(0);
            var label=new FrameworkElementFactory(typeof(TextBlock));
            label.SetBinding(TextBlock.TextProperty,new System.Windows.Data.Binding("."){Converter=new StackedLetters()});
            label.SetValue(TextBlock.TextAlignmentProperty,TextAlignment.Center);
            label.SetValue(TextBlock.LineHeightProperty,9.0);
            label.SetValue(TextBlock.LineStackingStrategyProperty,LineStackingStrategy.BlockLineHeight);
            // The stacked font leaves more descent below the last glyph than above its caps.
            // Center the visible lettering, rather than that asymmetric font line box.
            label.SetValue(TextBlock.RenderTransformProperty,new TranslateTransform(0,1.5));
            mode.ContentTemplate=new DataTemplate{VisualTree=label};
        }
        var slider=_horizontalItems.OfType<Slider>().Single();slider.Width=double.NaN;slider.MinWidth=25;slider.Margin=new(4,0,3,0);slider.HorizontalAlignment=HorizontalAlignment.Stretch;
        if(vertical)
        {
            Grid.SetColumn(mode,0);Grid.SetRow(mode,0);_verticalFooter.Children.Add(mode);
            Grid.SetColumn(slider,1);Grid.SetRow(slider,0);_verticalFooter.Children.Add(slider);
        }
        else
        {
            Grid.SetRow(mode,0);Grid.SetColumn(mode,2);_header.Children.Add(mode);
            Grid.SetRow(slider,0);Grid.SetColumn(slider,3);_header.Children.Add(slider);
        }
    }
    private sealed class StackedLetters:System.Windows.Data.IValueConverter
    {
        public object Convert(object value,Type targetType,object parameter,System.Globalization.CultureInfo culture)=>string.Join("\n",(value?.ToString()=="ONLY BOSS"?"ONLY":value?.ToString()??"").ToCharArray());
        public object ConvertBack(object value,Type targetType,object parameter,System.Globalization.CultureInfo culture)=>throw new NotSupportedException();
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
