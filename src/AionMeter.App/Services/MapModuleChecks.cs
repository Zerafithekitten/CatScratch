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
        Check(data.Points.Count==21234&&data.Categories.Count==41,"Altgard contains 21,234 locations in 41 categories");
        var all=data.Categories.Select(c=>c.Id).ToHashSet();
        var transform=new MapTransform(.3,0,700,0,.3,300);
        var projected=data.Project(transform,1919,1079,all);
        var visible=data.Points.Where(p=>{var q=transform.Apply(p.X,p.Y);return q.X>=330&&q.X<1820&&q.Y>=110&&q.Y<1040;}).Select(p=>p.Category).ToHashSet();
        Check(projected.Markers.Count<=350&&projected.Markers.Select(m=>m.Location.Category).ToHashSet().SetEquals(visible),"The symbol cap preserves every visible selected category");
        Check(data.Project(transform,1919,1079,new HashSet<string>()).Markers.Count==0,"No selected categories produces no symbols");
        var panelDraft=settings.CreateDraft();var panel=new MapSettingsPanel(panelDraft);
        var allButton=Nodes(panel).OfType<Button>().Single(b=>Equals(b.Content,"Alle"));allButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(panelDraft.MapCategories.Count==41&&settings.MapCategories.Count==1,"Filter buttons edit the settings draft only");
        Nodes(panel).OfType<Button>().Single(b=>Equals(b.Content,"Nur Cubes")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(panelDraft.MapCategories.SequenceEqual(["hidden-cube"]),"Only cubes restores the single cube filter");
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
        Check(File.Exists(Path.Combine(AppContext.BaseDirectory,"map-engine","CatScratch.MapEngine.exe")),"The hidden image engine is bundled with CatScratch");
        File.WriteAllText(output,JsonSerializer.Serialize(new{success=true,checks},new JsonSerializerOptions{WriteIndented=true}));
    }
}
