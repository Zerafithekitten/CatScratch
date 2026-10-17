using System.Windows;
using System.Windows.Controls;
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
    internal Button SettingsButton { get; }
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
        var pin=WindowPinning.CreateButton(this,settings,"meter");DockPanel.SetDock(pin,Dock.Right);header.Children.Add(pin);
        RestoreButton=new Button {Content="\uE9D9",ToolTip=UiText.Current.Localize("Show DPS meter")+" · "+settings.HotkeyToggleOverlay,Style=(Style)FindResource("IconButton"),Width=24,Height=24};
        RestoreButton.Click+=(_,_)=>restore();DockPanel.SetDock(RestoreButton,Dock.Right);header.Children.Add(RestoreButton);
        var paw=new Image {Source=AppIcon.Image,Width=22,Height=22,Margin=new(0,0,8,0),VerticalAlignment=VerticalAlignment.Center};
        DockPanel.SetDock(paw,Dock.Left);header.Children.Add(paw);
        var label=new StackPanel {VerticalAlignment=VerticalAlignment.Center};
        label.Children.Add(new TextBlock {Text="CatScratch",Foreground=ThemeColors.Brush("#C49AF5"),FontSize=13,FontWeight=FontWeights.SemiBold});
        _status.Foreground=ThemeColors.Brush("#A9B8C7");_status.FontSize=10;label.Children.Add(_status);
        header.Children.Add(label);
        Content=new Border {Child=header,Background=ThemeColors.Brush("#20242A"),BorderBrush=ThemeColors.Brush("#414A56"),BorderThickness=new(1),CornerRadius=new(8)};
        // This is the settings access point, so it deliberately stays outside global click-through registration.
        SourceInitialized+=(_,_)=>NativeMethods.SetExStyle(new WindowInteropHelper(this).Handle,NativeMethods.WS_EX_TOOLWINDOW,0x00040000);
        DragAnywhere.Attach(this,canDrag:()=>!settings.Locked,dropped:()=>
        {
            settings.OverlayLeft=Left;settings.OverlayTop=Top;settings.Save();moved?.Invoke(Left,Top);
        });
        RefreshText();RestorePosition();
    }

    internal void RefreshText()
    {
        SettingsButton.ToolTip=UiText.Current.TraySettings;
        RestoreButton.ToolTip=UiText.Current.Localize("Show DPS meter")+" · "+_settings.HotkeyToggleOverlay;
        _status.Text=UiText.Current.Localize("DPS meter hidden");
    }

    internal void RestorePosition()
    {
        Left=_settings.OverlayLeft;Top=_settings.OverlayTop;WindowPresentation.Constrain(this);
    }
}
