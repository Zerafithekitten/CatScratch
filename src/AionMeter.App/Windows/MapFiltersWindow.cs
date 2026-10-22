using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using AionMeter.App.Services;

namespace AionMeter.App.Windows;

internal sealed class MapFiltersWindow:Window
{
    private readonly Dictionary<string,CheckBox> _checks=[];
    private readonly TextBlock _status=new(){TextWrapping=TextWrapping.Wrap,FontSize=11,Margin=new(0,6,0,8)};
    private readonly StackPanel _body=new(){Visibility=Visibility.Collapsed};
    private readonly Action<IReadOnlyList<string>> _changed;
    private bool _syncing;
    private Rect _placed=Rect.Empty;
    internal int NativePlacementCount {get;private set;}
    internal MapFiltersWindow(Action<IReadOnlyList<string>> changed)
    {
        _changed=changed;Title="CatScratch · Map filters";Width=264;SizeToContent=SizeToContent.Height;
        WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;Background=Brushes.Transparent;
        ShowInTaskbar=false;ShowActivated=false;Topmost=true;Focusable=false;
        Foreground=ThemeColors.Brush("#EDF2F7");FontFamily=new FontFamily("Segoe UI");
        Resources.MergedDictionaries.Add(new(){Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative)});
        var root=new StackPanel{Margin=new(10)};
        root.Children.Add(MapExperimentalNotice.Create(compact:true));
        var toggle=new Button{Content=MapWords.L("🐾  Map filters  ▾","🐾  Kartenfilter  ▾"),Style=(Style)FindResource("GhostButton"),Focusable=false};
        toggle.Click+=(_,_)=>_body.Visibility=_body.Visibility==Visibility.Visible?Visibility.Collapsed:Visibility.Visible;
        root.Children.Add(toggle);root.Children.Add(_body);_body.Children.Add(_status);
        var actions=new StackPanel{Orientation=Orientation.Horizontal,Margin=new(0,0,0,8)};_body.Children.Add(actions);
        foreach(var (label,choose) in new (string,Func<string,bool>)[]{(MapWords.L("Cubes","Cubes"),id=>id=="hidden-cube"),(MapWords.L("All","Alle"),_=>true),(MapWords.L("None","Keine"),_=>false)})
        {
            var button=new Button{Content=label,Style=(Style)FindResource("GhostButton"),Focusable=false,Margin=new(0,0,5,0)};
            button.Click+=(_,_)=>{foreach(var pair in _checks)pair.Value.IsChecked=choose(pair.Key);Notify();};actions.Children.Add(button);
        }
        var choices=MapFilterGroups.Create(category=>
        {
            var box=new CheckBox{Content=MapWords.Category(category),Style=(Style)FindResource("DarkCheck"),Focusable=false,Margin=new(0,4,0,4),Tag=category.Id};
            box.Click+=(_,_)=>Notify();_checks.Add(category.Id,box);return box;
        });
        _body.Children.Add(new ScrollViewer{Content=choices,Height=310,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Focusable=false});
        _body.Children.Add(new TextBlock{Text=MapWords.L("Possible locations · not live spawns","Mögliche Fundstellen · keine Live-Spawns"),TextWrapping=TextWrapping.Wrap,FontSize=10,Margin=new(0,8,0,0)});
        Content=new Border{Background=ThemeColors.Brush("#20242A"),BorderBrush=ThemeColors.Brush("#414A56"),BorderThickness=new(1),CornerRadius=new(9),Child=root};
        SourceInitialized+=(_,_)=>
        {
            var handle=new WindowInteropHelper(this).Handle;
            NativeMethods.SetExStyle(handle,NativeMethods.WS_EX_TOOLWINDOW|NativeMethods.WS_EX_NOACTIVATE,0x00040000);
            HwndSource.FromHwnd(handle)?.AddHook((nint h,int message,nint w,nint l,ref bool handled)=>{if(message==0x0021){handled=true;return 3;}return 0;});
        };
    }
    private void Notify(){if(!_syncing)_changed(_checks.Where(p=>p.Value.IsChecked==true).Select(p=>p.Key).ToArray());}
    internal void Present(Rect game,IReadOnlySet<string> selected,string status)
    {
        _syncing=true;
        try{foreach(var pair in _checks)pair.Value.IsChecked=selected.Contains(pair.Key);}
        finally{_syncing=false;}
        _status.Text=status;
        var handle=new WindowInteropHelper(this).EnsureHandle();var dpi=VisualTreeHelper.GetDpi(this);
        var target=new Rect(game.Right-Width*dpi.DpiScaleX-26,game.Top+110*game.Height/1079,dpi.DpiScaleX,dpi.DpiScaleY);
        if(_placed!=target||!IsVisible)
        {
            _placed=target;Left=target.Left/dpi.DpiScaleX;Top=target.Top/dpi.DpiScaleY;
            if(!IsVisible)Show();
            NativeMethods.SetWindowPos(handle,(nint)(-1),(int)target.Left,(int)target.Top,0,0,0x11);NativePlacementCount++;
        }
    }
}
