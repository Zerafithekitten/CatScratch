using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AionMeter.App.Controls;
using AionMeter.App.Windows;
namespace AionMeter.App.Services;
internal static class BrandingChecks
{
    internal static void Run(string output)
    {
        var results=new List<string>();void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);results.Add(message);}
        using var meter=new MeterService(new AppSettings{Transient=true,DownloadIcons=false},null);
        foreach(var window in new Window[]{new SettingsWindow(meter),new UpdateWindow(meter.Updates,new Core.Updates.ReleaseInfo(new Version(4,0,8),"v4.0.8","https://example.invalid","Preview notes",null,null,null),false),new LastPlayedWithWindow(meter),new ResourceOverviewWindow(meter),new ResourcesWindow(meter),new DungeonEntriesWindow(meter),new OverlayWindow(meter)})
        {
            try
            {
                window.Show();window.UpdateLayout();var root=CatBranding.Target(window)!;var size=root.RenderSize;var position=new Point(window.Left,window.Top);
                Check(CatBranding.IsBranded(window),window.GetType().Name+" has its shared signature");
                if(window is LastPlayedWithWindow recent)Check(ReferenceEquals(((Image)recent.FindName("PawIcon")).Source,CatBranding.Mascot),"Large-window heading uses the shared mascot");
                if(window is UpdateWindow update)
                {
                    var button=(Button)update.FindName("UpdateButton");var windowRoot=(FrameworkElement)window.Content;
                    var art=root.TransformToAncestor(windowRoot).TransformBounds(new Rect(root.RenderSize));
                    var action=button.TransformToAncestor(windowRoot).TransformBounds(new Rect(button.RenderSize));
                    Check(!art.IntersectsWith(action),"Update artwork stays entirely inside notes and outside the action buttons");
                }
                var layer=AdornerLayer.GetAdornerLayer(root)!;var decorations=layer.GetAdorners(root)!;
                Check(decorations.All(a=>!a.IsHitTestVisible),"Decoration passes clicks through in "+window.GetType().Name);
                CatBranding.Apply(window);window.UpdateLayout();
                Check(root.RenderSize==size&&new Point(window.Left,window.Top)==position&&layer.GetAdorners(root)!.Length==decorations.Length,"Repeated branding preserves geometry and adds no duplicate in "+window.GetType().Name);
            }
            finally{window.Close();}
        }
        var title=new CatWordmark{Text="CatScratch",FontSize=16};title.RefreshArtwork();title.Measure(new Size(200,40));
        var plain=new TextBlock{Text="CatScratch",FontSize=16};plain.Measure(new Size(200,40));
        Check(title.DesiredSize==plain.DesiredSize,"Wordmark preserves the original title slot");
        Check(title.Background is ImageBrush&&title.Text=="CatScratch","Wordmark keeps its accessible text");
        title.Text="C\na\nt\nS\nc\nr\na\nt\nc\nh";
        Check(title.Background is null&&title.Foreground!=Brushes.Transparent,"Vertical rail keeps its readable letter stack without stale logo artwork");
        title.Text="CatScratch";Check(title.Background is ImageBrush,"Returning to horizontal restores the wordmark");
        File.WriteAllText(output,JsonSerializer.Serialize(results));
    }
}
