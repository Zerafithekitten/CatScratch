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
        Title="CatScratch · Event settings"; Width=470; Height=680; MinWidth=430; MinHeight=400;
        Topmost=true; WindowStyle=WindowStyle.None; ResizeMode=ResizeMode.CanResize;
        Background=Brush("#1B2027"); Foreground=Brush("#EDF2F7"); FontFamily=(FontFamily)FindResource("UiFont"); FontSize=12;
        UseLayoutRounding=true; TextOptions.SetTextFormattingMode(this,TextFormattingMode.Display);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative) });
        var root=new DockPanel { Margin=new(16) };
        var header=new DockPanel { Margin=new(0,0,0,14) }; DockPanel.SetDock(header,Dock.Top); root.Children.Add(header);
        var close=new Button { Content="\uE711", Style=(Style)FindResource("IconButton"), Width=22, Height=22 }; close.Click+=(_,_)=>Close(); DockPanel.SetDock(close,Dock.Right); header.Children.Add(close);
        var caption=new StackPanel { Orientation=Orientation.Horizontal };
        caption.Children.Add(new Image { Source=Icon,Width=20,Height=20,Margin=new(0,0,7,0) });
        caption.Children.Add(new TextBlock { Text="Event settings",Foreground=Brush("#79E3CF"),FontSize=17,FontWeight=FontWeights.SemiBold }); header.Children.Add(caption);
        var body=new StackPanel(); root.Children.Add(new ScrollViewer { Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto });
        Content=new Border { Child=root,Background=Background,BorderBrush=Brush("#607B827F"),BorderThickness=new(1),CornerRadius=new(10) };
        Controls.DragAnywhere.Attach(this);
        void Save() { cards.Save(); reload(); }
        StackPanel Section(string title)
        {
            var panel=new StackPanel(); panel.Children.Add(new TextBlock { Text=title,Foreground=Brush("#79E3CF"),FontWeight=FontWeights.SemiBold,Margin=new(0,0,0,8) });
            body.Children.Add(new Border { Child=panel,Background=Brush("#171E27"),BorderBrush=Brush("#364957"),BorderThickness=new(1),CornerRadius=new(7),Padding=new(12),Margin=new(0,0,0,10) }); return panel;
        }
        void Hint(StackPanel parent,string text) => parent.Children.Add(new TextBlock { Text=text,Foreground=Brush("#A9B8C7"),FontSize=11,TextWrapping=TextWrapping.Wrap,Margin=new(0,6,0,4) });
        var regionPanel=Section("SCHEDULE");
        var region=new ComboBox { ItemsSource=new[]{"Europe","NA East","NA West"},SelectedItem=cards.Saved.EventRegion };
        region.SelectionChanged+=(_,_)=> { if(region.SelectedItem is string selected) { cards.Saved.EventRegion=selected; Save(); } }; regionPanel.Children.Add(region);
        Hint(regionPanel,"Only the next selected start within one hour. Events stay visible for two minutes after starting, with a pulsing border.");
        var events=Section("EVENTS TO SHOW");
        foreach(var group in AutomaticEventSchedule.Catalog.GroupBy(e=>EventKinds.For(e.Id)))
        {
            events.Children.Add(new TextBlock { Text=group.Key,Foreground=EventPanelBrushes.ForKind(group.Key),FontSize=10,FontWeight=FontWeights.SemiBold,Margin=new(0,6,0,3) });
            foreach(var item in group)
            {
                var check=new CheckBox { Content=item.Name,Style=(Style)FindResource("DarkCheck"),IsChecked=cards.Saved.SelectedEvents.Contains(item.Id) };
                check.Click+=(_,_)=> { cards.Saved.SelectedEvents.Remove(item.Id); if(check.IsChecked==true) cards.Saved.SelectedEvents.Add(item.Id); Save(); }; events.Children.Add(check);
            }
        }
        var allBosses=new CheckBox { Content="Show all world bosses",Style=(Style)FindResource("DarkCheck"),IsChecked=cards.Saved.ShowAllWorldBosses,Margin=new(0,8,0,0) };
        allBosses.Click+=(_,_)=> { cards.Saved.ShowAllWorldBosses=allBosses.IsChecked==true; Save(); }; events.Children.Add(allBosses);
        Hint(events,"Includes scheduled bosses and known respawns received from the game.");
        var appearance=Section("APPEARANCE");
        void Slider(string label,double min,double max,double current,Action<double> change,string unit)
        {
            var text=new TextBlock { Text=$"{label} · {current:0}{unit}",Margin=new(0,4,0,2) }; appearance.Children.Add(text);
            var slider=new Slider { Minimum=min,Maximum=max,Value=current,Style=(Style)FindResource("GoldSlider") };
            slider.ValueChanged+=(_,e)=> { text.Text=$"{label} · {e.NewValue:0}{unit}"; change(e.NewValue); }; appearance.Children.Add(slider);
        }
        Slider("Opacity",40,100,cards.Saved.EventOpacity*100,value=> { cards.Saved.EventOpacity=value/100; setOpacity(value/100); cards.Save(); },"%");
        Slider("Window width",240,1000,width(),setWidth," px");
        var tools=Section("SOUNDS & BOSS TIMERS");
        var sounds=new Button { Content="Event sounds",Style=(Style)FindResource("GhostButton"),Margin=new(0,0,0,6) }; sounds.Click+=(_,_)=>AppHost.Current.ShowEventSounds(); tools.Children.Add(sounds);
        var bosses=new Button { Content="Open boss timers",Style=(Style)FindResource("GhostButton") }; bosses.Click+=(_,_)=>AppHost.Current.ShowBossTimers(); tools.Children.Add(bosses);
        Hint(tools,"Changes are saved automatically. Community schedule from Shugo.GG, checked Oct 12, 2026; availability is not officially confirmed.");
        var source=new Button { Content="Open schedule source",Style=(Style)FindResource("GhostButton"),Margin=new(0,6,0,0) }; source.Click+=(_,_)=>System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(AutomaticEventSchedule.Source) { UseShellExecute=true }); tools.Children.Add(source);
    }
    private static Brush Brush(string color) { var brush=new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)); brush.Freeze(); return brush; }
}
