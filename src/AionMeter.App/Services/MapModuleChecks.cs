using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AionMeter.App.Windows;

namespace AionMeter.App.Services;

internal static class MapModuleChecks
{
    private static IEnumerable<DependencyObject> Nodes(DependencyObject root)
    {
        yield return root;
        foreach(var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            foreach(var node in Nodes(child))yield return node;
    }
    private static void Render(FrameworkElement element,string path)
    {
        element.UpdateLayout();
        var bitmap=new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth),(int)Math.Ceiling(element.ActualHeight),96,96,PixelFormats.Pbgra32);
        bitmap.Render(element);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file=File.Create(path);encoder.Save(file);
    }
    internal static void Run(string output)
    {
        UiText.Use("de");var checks=new List<string>();
        void Check(bool valid,string text){if(!valid)throw new InvalidOperationException(text);checks.Add(text);}
        var directory=Path.GetDirectoryName(Path.GetFullPath(output))!;Directory.CreateDirectory(directory);
        var settings=new AppSettings{Transient=true,DownloadIcons=false,CheckUpdates=false};
        Check(!settings.ShowMapOverlay&&settings.MapCategories.SequenceEqual(["hidden-cube"]),"Map recognition is opt-in, with only cubes selected by default");
        var properties=new[]{"ShowMapOverlay","MapCategories","MapDirectDrag"};
        using(var preview=new SettingsPreviewSession(settings))
        {
            var draft=settings.CreateDraft();draft.ShowMapOverlay=true;draft.MapCategories=["aether"];draft.MapDirectDrag=false;
            preview.Apply(draft,properties);
            Check(settings.ShowMapOverlay&&!settings.MapDirectDrag&&settings.MapCategories.SequenceEqual(["aether"]),"All map preferences apply through live preview");
        }
        Check(!settings.ShowMapOverlay&&settings.MapDirectDrag&&settings.MapCategories.SequenceEqual(["hidden-cube"]),"Discard restores map activation, filters and tracking");
        var data=MapOverlayData.Shared.Value;
        Check(data.Points.Count==20614&&data.Categories.Count==39,"Altgard contains 20,614 locations in 39 categories");
        var all=data.Categories.Select(c=>c.Id).ToHashSet();
        var grouped=MapFilterGroups.Groups.SelectMany(g=>g.Categories).ToArray();
        Check(MapFilterGroups.Groups.Length==6&&grouped.Length==39&&grouped.Distinct().Count()==39&&grouped.ToHashSet().SetEquals(all),"All map filters belong to exactly one of six ordered groups");
        Check(!data.Points.Any(p=>p.Category is "monolith" or "teleport-artifact")&&!data.Categories.Any(c=>c.Id is "monolith" or "teleport-artifact"),"Feathers and Kibelisks are absent from both map locations and filters");
        var gemstones=data.Project(new MapTransform(.001,0,700,0,.001,300),1919,1079,new HashSet<string>{"jewelry"});
        Check(gemstones.Markers.Count==3&&gemstones.Markers.Sum(m=>m.Count)==590&&gemstones.Markers.Select(m=>m.Location.Label).Distinct().Count()==3,"Overlapping Diamond, Ruby and Sapphire locations remain separate material groups");
        Check(gemstones.Markers.Select(m=>m.IconKey).Distinct().Count()==3,"Grouped gemstones retain their distinct original material icons");
        var transform=new MapTransform(.3,0,700,0,.3,300);
        var projected=data.Project(transform,1919,1079,all);
        var visible=data.Points.Where(p=>{var q=transform.Apply(p.X,p.Y);return q.X>=330&&q.X<1820&&q.Y>=110&&q.Y<1040;}).Select(p=>p.Category).ToHashSet();
        Check(projected.Markers.Count<=350&&projected.Markers.Select(m=>m.Location.Category).ToHashSet().SetEquals(visible),"The symbol cap preserves every visible selected category");
        Check(data.Project(transform,1919,1079,new HashSet<string>()).Markers.Count==0,"No selected categories produces no symbols");
        var preserved=projected.Markers.ToArray();
        data.Project(new MapTransform(.001,0,700,0,.001,300),1919,1079,new HashSet<string>{"jewelry"});
        Check(projected.Markers.SequenceEqual(preserved),"Reused projection buffers never mutate a previously displayed marker list");
        var parallel=Task.WhenAll(Enumerable.Range(0,8).Select(_=>Task.Run(()=>data.Project(transform,1919,1079,all)))).GetAwaiter().GetResult();
        Check(parallel.All(p=>p.Visible==projected.Visible&&p.Markers.SequenceEqual(projected.Markers)),"Concurrent projections retain deterministic independent outputs");
        var panelDraft=settings.CreateDraft();var panel=new MapSettingsPanel(panelDraft);
        var allButton=Nodes(panel).OfType<Button>().Single(b=>Equals(b.Content,"Alle"));allButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(panelDraft.MapCategories.Count==39&&settings.MapCategories.Count==1,"Filter buttons edit the settings draft only");
        Nodes(panel).OfType<Button>().Single(b=>Equals(b.Content,"Nur Cubes")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(panelDraft.MapCategories.SequenceEqual(["hidden-cube"]),"Only cubes restores the single cube filter");
        IReadOnlyList<string>? selectedFromMap=null;
        var filters=new MapFiltersWindow(ids=>selectedFromMap=ids);
        try
        {
            filters.Present(new Rect(-30000,-30000,1919,1079),new HashSet<string>{"hidden-cube"},"Altgard · Marker werden abgeglichen …");
            var placementCount=filters.NativePlacementCount;
            for(var i=0;i<20;i++)filters.Present(new Rect(-30000,-30000,1919,1079),new HashSet<string>{"hidden-cube"},"Altgard");
            Check(filters.NativePlacementCount==placementCount,"Stationary map refreshes do not repeatedly reposition or reorder the filter window");
            Check(Nodes(filters).OfType<TextBlock>().Any(t=>t.Text.Contains("STARK EXPERIMENTELL")),"The in-map panel displays a prominent experimental warning even while collapsed");
            Nodes(filters).OfType<Button>().Single(b=>b.Content is string text&&text.Contains("Kartenfilter")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Nodes(filters).OfType<Button>().Single(b=>Equals(b.Content,"Alle")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(selectedFromMap?.Count==39,"In-map All button changes all 39 filters without opening Settings");
            Nodes(filters).OfType<Button>().Single(b=>Equals(b.Content,"Keine")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(selectedFromMap?.Count==0,"In-map None button clears markers");
            Nodes(filters).OfType<Button>().Single(b=>Equals(b.Content,"Cubes")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(selectedFromMap?.SequenceEqual(["hidden-cube"])==true,"In-map cube preset restores the cube filter");
            var groupPanels=Nodes(filters).OfType<Expander>().Where(e=>e.Tag is string tag&&tag.StartsWith("map-group-")).ToArray();
            Check(groupPanels.Length==6&&groupPanels.All(e=>!e.IsExpanded),"In-map groups initially show a compact six-category overview");
            foreach(var group in groupPanels){group.IsExpanded=true;group.IsExpanded=false;}
            Check(selectedFromMap?.SequenceEqual(["hidden-cube"])==true&&Nodes(filters).OfType<CheckBox>().Single(c=>Equals(c.Tag,"hidden-cube")).IsChecked==true,"Expanding and collapsing groups preserves selected markers");
            filters.UpdateLayout();Render(filters,Path.Combine(directory,"map-filters.png"));
            var style=(long)NativeMethods.GetWindowLongPtr(new WindowInteropHelper(filters).Handle,NativeMethods.GWL_EXSTYLE);
            Check((style&NativeMethods.WS_EX_NOACTIVATE)!=0&&!filters.ShowInTaskbar,"Map filters are clickable without activating a separate app window");
            Check(MapNative.GetWindowDisplayAffinity(new WindowInteropHelper(filters).Handle,out var filterAffinity)&&filterAffinity==0x11,"Map filter window is excluded from desktop recording as requested");
        }
        finally{filters.Close();}
        var streamOverlay=new MapOverlayWindow();
        try
        {
            var handle=new WindowInteropHelper(streamOverlay).EnsureHandle();
            Check(MapNative.GetWindowDisplayAffinity(handle,out var affinity)&&affinity==0x11,"Marker overlay is excluded from desktop recording as requested");
        }
        finally{streamOverlay.Close();}
        var shown=new Window{Width=100,Height=80,Left=-30000,Top=-30000,ShowActivated=false,ShowInTaskbar=false};
        var hidden=new Window{Width=100,Height=80,Left=-30000,Top=-30000,ShowActivated=false,ShowInTaskbar=false};
        try
        {
            shown.Show();hidden.Show();hidden.Hide();
            using var suppression=new MapHudSuppression();suppression.Apply(true,[shown,hidden]);
            var handle=new WindowInteropHelper(shown).Handle;
            Check(MapNative.DwmGetWindowAttribute(handle,14,out var cloak,4)==0&&(cloak&1)!=0&&shown.IsVisible&&!hidden.IsVisible,"Map suppression hides native HUD presentation without changing WPF visibility");
            suppression.Apply(false,[shown,hidden]);MapNative.DwmGetWindowAttribute(handle,14,out cloak,4);
            Check(cloak==0&&shown.IsVisible&&!hidden.IsVisible,"Closing the map restores only previously visible windows");
            suppression.Apply(true,[shown,hidden]);shown.Hide();suppression.Dispose();
            Check(!shown.IsVisible&&!hidden.IsVisible,"Manual hiding during map suppression is preserved");
        }
        finally{shown.Close();hidden.Close();}
        var hud=new Window{Width=100,Height=80,Left=-30000,Top=-30000,ShowActivated=false,ShowInTaskbar=false};
        using(var service=new MapOverlayService(settings,()=>[hud],()=>false))
        {
            try
            {
                hud.Show();service.ApplyPreferences();
                var receive=typeof(MapOverlayService).GetMethod("Receive",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
                var tick=typeof(MapOverlayService).GetMethod("Tick",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
                receive.Invoke(service,["{\"kind\":\"presence\",\"open\":false,\"reason\":\"waiting-game\",\"foreground\":\"other-client.exe\"}"]);
                Check(service.Status.Contains("other-client.exe"),"Waiting-for-game status identifies the actual foreground executable for remote diagnosis");
                receive.Invoke(service,["{\"kind\":\"presence\",\"open\":false,\"reason\":\"waiting-game\",\"foreground\":null}"]);
                Check(service.Status.Contains("Prozessname nicht verfügbar"),"Unavailable foreground metadata is explicitly distinguished from a mismatched client");
                receive.Invoke(service,["{\"kind\":\"presence\",\"open\":false,\"reason\":\"waiting-map\"}"]);
                Check(!service.MapOpen&&service.Status.Contains("warte auf die Altgard-Karte")&&!service.Status.Contains("wird abgeglichen"),"An unconfirmed map never reports active localization");
                receive.Invoke(service,["{\"kind\":\"presence\",\"open\":true,\"rect\":[-30000,-30000,-28081,-28921],\"reason\":\"waiting-map\",\"score\":0.95}"]);
                tick.Invoke(service,null);
                MapNative.DwmGetWindowAttribute(new WindowInteropHelper(hud).Handle,14,out var state,4);
                Check(service.MapOpen&&(state&1)!=0,"The integrated service suppresses HUD immediately on confirmed map presence, before a marker pose exists");
                Check(Application.Current.Windows.OfType<MapFiltersWindow>().Any(w=>w.IsVisible),"In-map filters remain available before marker localization");
                var fields=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                typeof(MapOverlayService).GetField("_data",fields)!.SetValue(service,data);
                string PoseMessage()=>JsonSerializer.Serialize(new{kind="pose",rect=new[]{-30000,-30000,-28081,-28921},matrix=new[]{new[]{.3,0,700},new[]{0,.3,300}},stamp=MapNative.Now,pointer=new[]{0d,0d,0d,MapNative.Now}});
                receive.Invoke(service,[PoseMessage()]);
                var markerList=typeof(MapOverlayService).GetField("_markers",fields)!.GetValue(service);var markerStatus=service.Status;
                receive.Invoke(service,[PoseMessage()]);
                Check(ReferenceEquals(markerList,typeof(MapOverlayService).GetField("_markers",fields)!.GetValue(service)),"An unchanged accepted pose reuses the existing projected markers");
                receive.Invoke(service,["{\"kind\":\"pose\",\"matrix\":null}"]);
                receive.Invoke(service,[PoseMessage()]);
                Check(service.Status==markerStatus,"Recovering the same pose restores marker status after a temporary failed match");
                receive.Invoke(service,["{\"kind\":\"pose\",\"matrix\":null}"]);tick.Invoke(service,null);
                Check(service.MapOpen&&Application.Current.Windows.OfType<MapFiltersWindow>().Any(w=>w.IsVisible),"A failed marker match does not restore HUD or remove map filters");
                receive.Invoke(service,["{\"kind\":\"presence\",\"open\":false}"]);tick.Invoke(service,null);
                MapNative.DwmGetWindowAttribute(new WindowInteropHelper(hud).Handle,14,out state,4);
                Check(!service.MapOpen&&state==0,"The integrated service releases HUD suppression when the map closes");
            }
            finally{hud.Close();}
        }
        var drag=new MapDragAssist();var bounds=new Rect(0,0,1919,1079);
        for(var i=0;i<4;i++){var p=new MapPointer(700+20*i,500,1,1+i*.1);drag.Observe(p);drag.Confirm(new MapTransform(1,0,20*i,0,1,0),p,bounds);}
        var source=new MapPointer(760,500,1,1.3);var moved=new MapPointer(800,500,1,1.38);drag.Observe(moved);
        Check(drag.Calibrated&&Math.Abs(drag.Adjust(new(1,0,60,0,1,0),source,moved,bounds).X-100)<.001,"Confirmed cursor motion compensates image delay during dragging");
        Check(drag.Adjust(new(1,0,60,0,1,0),source,moved with {Stamp=2},bounds).X==60,"Stale frames cannot drive cursor compensation");
        using var meter=new MeterService(settings,null,historyDirectory:Path.Combine(directory,"isolated-history"));
        using var cards=new TimerCards(meter,directory:Path.Combine(directory,"isolated-events"));
        var window=new SettingsWindow(meter,cards){Left=-30000,Top=-30000,ShowActivated=false};
        try
        {
            window.Show();window.SelectTab("map");window.UpdateLayout();
            Check(Nodes(window).OfType<MapSettingsPanel>().Any(),"The map module is integrated into the real Settings window");
            Check(Nodes(window).OfType<TextBlock>().Any(t=>t.Text.Contains("STARK EXPERIMENTELL")&&t.FontSize>=17),"Settings prominently warn that map recognition and marker positions are experimental");
            Render(window,Path.Combine(directory,"map-settings.png"));
        }
        finally{window.Close();}
        var canvas=new MapCanvas{Width=1919,Height=1079};canvas.Set(transform,projected.Markers,new Size(1919,1079));
        var layers=new Grid();
        var previewPath=Path.Combine(directory,"altgard-preview.png");
        if(File.Exists(previewPath))layers.Children.Add(new Image{Source=new BitmapImage(new Uri(previewPath)),Stretch=Stretch.Fill});
        layers.Children.Add(canvas);
        var frame=new Border{Width=1919,Height=1079,Background=ThemeColors.Brush("#191D23"),Child=layers};
        frame.Measure(new Size(1919,1079));frame.Arrange(new Rect(0,0,1919,1079));Render(frame,Path.Combine(directory,"map-symbols.png"));
        var materialSamples=new[]{"Sapphire Gemstone","Diamond Gemstone","Ruby Gemstone","Orichalcum Ore"}.Select((label,i)=>new MapMarker(data.Points.First(p=>p.Label==label) with {X=500+i%2*340,Y=250+i/2*100},i==1?3:1)).ToArray();
        Check(materialSamples.Select(m=>m.IconKey).Distinct().Count()==4,"Sapphire, Diamond, Ruby and Orichalcum each use their own original game icon");
        foreach(var marker in materialSamples)
        {
            using var resource=Application.GetResourceStream(new Uri($"pack://application:,,,/Assets/Map/{marker.IconKey}.png")).Stream;
            Check(resource.Length>0,"Bundled material icon is available: "+marker.Location.Label);
        }
        canvas.Set(new(1,0,0,0,1,0),materialSamples,new Size(1919,1079));Render(frame,Path.Combine(directory,"map-materials.png"));
        Check(File.Exists(Path.Combine(AppContext.BaseDirectory,"map-engine","CatScratch.MapEngine.exe")),"The hidden image engine is bundled with CatScratch");
        Check(File.Exists(Path.Combine(AppContext.BaseDirectory,"data","map","map-close.png")),"Covered-title bootstrap cue is bundled with the map data");
        File.WriteAllText(output,JsonSerializer.Serialize(new{success=true,checks},new JsonSerializerOptions{WriteIndented=true}));
    }
}
