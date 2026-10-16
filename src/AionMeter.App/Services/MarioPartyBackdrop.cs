using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AionMeter.App.Services;

/// <summary>One continuous wallpaper per window, without changing semantic brushes or saved colors.</summary>
internal static class MarioPartyBackdrop
{
    internal const double Period=600,Seconds=12;
    private sealed record Entry(DependencyObject Target,DependencyProperty Property,Brush Normal,LinearGradientBrush Party);
    private sealed class State
    {
        internal bool Busy;
        internal readonly Dictionary<(DependencyObject,DependencyProperty),Entry> Entries=[];
    }
    private static readonly Dictionary<Window,State> Windows=[];
    internal static TranslateTransform Phase {get;}=new();
    internal static AnimationClock? Clock {get;private set;}

    internal static void Attach(Window window)
    {
        if(Windows.ContainsKey(window))return;
        Windows[window]=new();
        window.Loaded+=(_,_)=>Refresh(window);
        window.LayoutUpdated+=(_,_)=>{if(ThemeColors.Current.MarioParty)Refresh(window);};
        window.Closed+=(_,_)=>Windows.Remove(window);
    }

    private static AnimationClock Start(TranslateTransform transform)
    {
        var animation=new DoubleAnimation(0,Period,TimeSpan.FromSeconds(Seconds)){RepeatBehavior=RepeatBehavior.Forever};
        animation.Freeze();var clock=(AnimationClock)animation.CreateClock(true);
        transform.ApplyAnimationClock(TranslateTransform.XProperty,clock);return clock;
    }

    internal static LinearGradientBrush CreateBrush(byte alpha=255,double opacity=1,bool preview=false)
    {
        var transform=preview?new TranslateTransform():Phase;
        if(preview)Start(transform);
        var brush=new LinearGradientBrush{MappingMode=BrushMappingMode.Absolute,StartPoint=new(0,0),EndPoint=new(Period,0),SpreadMethod=GradientSpreadMethod.Repeat,Transform=transform,Opacity=opacity};
        for(var i=0;i<=6;i++)
        {
            var color=Controls.HsvColorPicker.FromHsv(i*60,.65,.22);color.A=alpha;
            brush.GradientStops.Add(new(color,i/6d));
        }
        return brush;
    }

    internal static void RefreshAll()
    {
        if(ThemeColors.Current.MarioParty)Clock??=Start(Phase);
        else{Phase.BeginAnimation(TranslateTransform.XProperty,null);Phase.X=0;Clock=null;}
        foreach(var window in Windows.Keys.ToArray())Refresh(window);
    }

    internal static void Refresh(Window window)
    {
        if(!Windows.TryGetValue(window,out var state)||state.Busy)return;
        state.Busy=true;
        try
        {
            if(!ThemeColors.Current.MarioParty||window is AionMeter.App.Windows.OverlayWindow {DisplayOnly:true})
            {
                foreach(var entry in state.Entries.Values)
                    if(ReferenceEquals(entry.Target.GetValue(entry.Property),entry.Party))entry.Target.SetCurrentValue(entry.Property,entry.Normal);
                return;
            }
            Clock??=Start(Phase);
            var alive=new HashSet<DependencyObject>();
            void Visit(DependencyObject node,bool interactive=false)
            {
                alive.Add(node);
                interactive|=node is ButtonBase or TextBoxBase or ComboBox or ScrollBar or Slider;
                var property=node switch{Window=>Control.BackgroundProperty,Border=>Border.BackgroundProperty,Panel=>Panel.BackgroundProperty,_=>null};
                if(!interactive&&property is not null)
                {
                    var key=(node,property);var current=node.GetValue(property) as Brush;
                    if(current is SolidColorBrush solid&&ThemeColors.IsBackgroundBrush(solid))
                    {
                        if(!state.Entries.TryGetValue(key,out var entry))
                            state.Entries[key]=entry=new(node,property,solid,CreateBrush(solid.Color.A,solid.Opacity));
                        node.SetCurrentValue(property,entry.Party);
                    }
                    if(state.Entries.TryGetValue(key,out var existing)&&ReferenceEquals(node.GetValue(property),existing.Party)&&node is FrameworkElement element)
                    {
                        var root=window.Content as UIElement??window;
                        var origin=element==window?0:element.TranslatePoint(new(0,0),root).X;
                        var start=new Point(-origin,0);var end=new Point(Period-origin,0);
                        if(existing.Party.StartPoint!=start)existing.Party.StartPoint=start;
                        if(existing.Party.EndPoint!=end)existing.Party.EndPoint=end;
                    }
                }
                for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)Visit(VisualTreeHelper.GetChild(node,i),interactive);
            }
            Visit(window);
            if(window.Content is DependencyObject content&&!alive.Contains(content))Visit(content);
            foreach(var key in state.Entries.Keys.Where(k=>!alive.Contains(k.Item1)).ToArray())state.Entries.Remove(key);
        }
        finally{state.Busy=false;}
    }
}
