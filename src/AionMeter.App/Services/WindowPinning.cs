using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AionMeter.App.Services;

/// <summary>Position lock independent of click-through, capture and programmatic layout changes.</summary>
internal static class WindowPinning
{
    private sealed record State(AppSettings Settings,string Key,Button Button);
    private static readonly Dictionary<Window,State> Windows=[];
    internal static bool IsPinned(Window window)=>Windows.TryGetValue(window,out var state) && state.Settings.ModulePins.GetValueOrDefault(state.Key);
    internal static Button? ButtonFor(Window window)=>Windows.TryGetValue(window,out var state)?state.Button:null;
    internal static bool BlocksMove(Window window,int message,nint parameter)=>IsPinned(window) &&
        (message==0x0112 && ((long)parameter & 0xFFF0)==0xF010 || message==0x00A1 && parameter==2);
    internal static Button CreateButton(Window window,AppSettings settings,string key)
    {
        settings.ModulePins??=[];
        if(Windows.TryGetValue(window,out var existing))return existing.Button;
        var button=new Button {Style=(Style)window.FindResource("IconButton"),Width=22,Height=22,Padding=new(0)};
        var icon=new System.Windows.Shapes.Path {Data=Geometry.Parse("M 4,1 H 10 L 9,5 L 12,8 H 8 V 11 L 7,14 L 6,11 V 8 H 2 L 5,5 Z"),Width=12,Height=12,Stretch=Stretch.Uniform,StrokeThickness=1,StrokeLineJoin=PenLineJoin.Round};
        icon.SetBinding(Shape.StrokeProperty,new Binding(nameof(Control.Foreground)){Source=button});button.Content=icon;
        System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(button,true);
        Windows[window]=new(settings,key,button);
        button.Click+=(_,e)=>{settings.ModulePins[key]=!IsPinned(window);settings.Save();Refresh(window);e.Handled=true;};
        window.SourceInitialized+=(_,_)=>HwndSource.FromHwnd(new WindowInteropHelper(window).Handle)?.AddHook((nint h,int m,nint w,nint l,ref bool handled)=>{if(BlocksMove(window,m,w))handled=true;return 0;});
        window.Closed+=(_,_)=>Windows.Remove(window);Refresh(window);return button;
    }
    internal static void Refresh(Window window)
    {
        if(!Windows.TryGetValue(window,out var state))return;
        state.Button.ToolTip=UiText.Current.Localize(IsPinned(window)?"Unpin position":"Pin position");
        state.Button.Foreground=ThemeColors.Brush(IsPinned(window)?"#C49AF5":"#A9B8C7");
        state.Button.Background=IsPinned(window)?ThemeColors.Brush("#23C49AF5"):Brushes.Transparent;
    }
    internal static void RefreshAll(){foreach(var window in Windows.Keys.ToArray())Refresh(window);}
    internal static void SetDisplayOnly(Window window,bool enabled){if(Windows.TryGetValue(window,out var state))state.Button.Visibility=enabled?Visibility.Hidden:Visibility.Visible;}
}
