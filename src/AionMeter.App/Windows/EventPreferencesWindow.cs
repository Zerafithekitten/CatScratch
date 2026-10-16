using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AionMeter.App.Services;
using AionMeter.Core.Events;

namespace AionMeter.App.Windows;

public sealed class EventPreferencesWindow : Window
{
    public EventPreferencesWindow(TimerCards cards, Func<double> width, Action<double> setWidth, Action<double> setOpacity, Action reload)
    {
        AppIcon.Apply(this);
        Title="CatScratch · "+UiText.Current.Localize("Event settings"); Width=470; Height=680; MinWidth=430; MinHeight=400;
        Topmost=true; WindowStyle=WindowStyle.None; ResizeMode=ResizeMode.CanResize;
        System.Windows.Shell.WindowChrome.SetWindowChrome(this, new System.Windows.Shell.WindowChrome
        { CaptionHeight=0, ResizeBorderThickness=new Thickness(6), GlassFrameThickness=new Thickness(0), CornerRadius=new CornerRadius(0) });
        Background=Brush("#20242A"); Foreground=Brush("#EDF2F7"); FontFamily=(FontFamily)FindResource("UiFont"); FontSize=12;
        UseLayoutRounding=true; TextOptions.SetTextFormattingMode(this,TextFormattingMode.Display);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative) });
        var root=new DockPanel { Margin=new(16) };
        var header=new DockPanel { Margin=new(0,0,0,14) }; DockPanel.SetDock(header,Dock.Top); root.Children.Add(header);
        var close=new Button { Content="\uE711", Style=(Style)FindResource("IconButton"), Width=22, Height=22 }; close.Click+=(_,_)=>Close(); DockPanel.SetDock(close,Dock.Right); header.Children.Add(close);
        var caption=new StackPanel { Orientation=Orientation.Horizontal };
        caption.Children.Add(new Image { Source=Icon,Width=20,Height=20,Margin=new(0,0,7,0) });
        caption.Children.Add(new TextBlock { Text=UiText.Current.Localize("Event settings"),Foreground=Brush("#C49AF5"),FontSize=17,FontWeight=FontWeights.SemiBold }); header.Children.Add(caption);
        var body=BuildBody(cards,width,setWidth,setOpacity,reload,this); root.Children.Add(new ScrollViewer { Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto });
        Content=new Border { Child=root,Background=Background,BorderBrush=Brush("#60616A76"),BorderThickness=new(1),CornerRadius=new(10) };
        Controls.DragAnywhere.Attach(this);
    }
    internal static StackPanel BuildBody(TimerCards cards, Func<double> width, Action<double> setWidth, Action<double> setOpacity, Action reload, FrameworkElement scope)
    {
        var body=new StackPanel();
        void Save() { cards.Save(); reload(); }
        StackPanel Section(string title)
        {
            var panel=new StackPanel(); panel.Children.Add(new TextBlock { Text=UiText.Current.Localize(title),Foreground=Brush("#C49AF5"),FontWeight=FontWeights.SemiBold,Margin=new(0,0,0,8) });
            body.Children.Add(new Border { Child=panel,Background=Brush("#191D23"),BorderBrush=Brush("#414A56"),BorderThickness=new(1),CornerRadius=new(7),Padding=new(12),Margin=new(0,0,0,10) }); return panel;
        }
        void Hint(StackPanel parent,string text) => parent.Children.Add(new TextBlock { Text=UiText.Current.Localize(text),Foreground=Brush("#A9B8C7"),FontSize=11,TextWrapping=TextWrapping.Wrap,Margin=new(0,6,0,4) });
        var regionPanel=Section("SCHEDULE");
        var regionIds=new[]{"Europe","NA East","NA West"};
        var region=new ComboBox { ItemsSource=regionIds.Select(UiText.Current.Localize).ToArray(),SelectedIndex=Math.Max(0,Array.IndexOf(regionIds,cards.Saved.EventRegion)) };
        region.SelectionChanged+=(_,_)=> { if(region.SelectedIndex>=0) { cards.Saved.EventRegion=regionIds[region.SelectedIndex]; Save(); } }; regionPanel.Children.Add(region);
        Hint(regionPanel,"All selected events within the next hour. Started events: 3 minutes; Spacetime Rift: 10 minutes; bosses: 2 minutes.");
        Hint(regionPanel,"The clock opens the 24-hour timeline. Enable Started events above.");
        var events=Section("EVENTS TO SHOW");
        foreach(var group in AutomaticEventSchedule.Catalog.GroupBy(e=>EventKinds.For(e.Id)))
        {
            events.Children.Add(new TextBlock { Text=UiText.Current.Localize(group.Key),Foreground=EventPanelBrushes.ForKind(group.Key),FontSize=10,FontWeight=FontWeights.SemiBold,Margin=new(0,6,0,3) });
            foreach(var item in group)
            {
                var check=new CheckBox { Content=UiText.Current.Localize(item.Name),Style=(Style)scope.FindResource("DarkCheck"),IsChecked=cards.Saved.SelectedEvents.Contains(item.Id) };
                check.Click+=(_,_)=> { cards.Saved.SelectedEvents.Remove(item.Id); if(check.IsChecked==true) cards.Saved.SelectedEvents.Add(item.Id); Save(); }; events.Children.Add(check);
            }
        }
        var allBosses=new CheckBox { Content=UiText.Current.Localize("Show all world bosses"),Style=(Style)scope.FindResource("DarkCheck"),IsChecked=cards.Saved.ShowAllWorldBosses,Margin=new(0,8,0,0) };
        allBosses.Click+=(_,_)=> { cards.Saved.ShowAllWorldBosses=allBosses.IsChecked==true; Save(); }; events.Children.Add(allBosses);
        Hint(events,"Includes scheduled bosses and known respawns received from the game.");
        var appearance=Section("APPEARANCE");
        void Slider(string label,double min,double max,double current,Action<double> change,string unit)
        {
            label=UiText.Current.Localize(label);
            var text=new TextBlock { Text=$"{label} · {current:0}{unit}",Margin=new(0,4,0,2) }; appearance.Children.Add(text);
            var slider=new Slider { Minimum=min,Maximum=max,Value=current,Style=(Style)scope.FindResource("GoldSlider") };
            slider.ValueChanged+=(_,e)=> { text.Text=$"{label} · {e.NewValue:0}{unit}"; change(e.NewValue); }; appearance.Children.Add(slider);
        }
        Slider("Opacity",40,100,cards.Saved.EventOpacity*100,value=> { cards.Saved.EventOpacity=value/100; setOpacity(value/100); cards.Save(); },"%");
        Slider("Upcoming events width",240,1000,width(),setWidth," px");
        var tools=Section("SOUNDS & BOSS TIMERS");
        var bosses=new Button { Content=UiText.Current.Localize("Open boss timers"),Style=(Style)scope.FindResource("GhostButton") }; bosses.Click+=(_,_)=>AppHost.Current.ShowBossTimers(); tools.Children.Add(bosses);
        Hint(tools,"Community schedule from Shugo.GG, checked Oct 12, 2026; availability is not officially confirmed.");
        var source=new Button { Content=UiText.Current.Localize("Open schedule source"),Style=(Style)scope.FindResource("GhostButton"),Margin=new(0,6,0,0) }; source.Click+=(_,_)=>System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(AutomaticEventSchedule.Source) { UseShellExecute=true }); tools.Children.Add(source);
        return body;
    }
    private static Brush Brush(string color) { return ThemeColors.Brush(color); }
}
