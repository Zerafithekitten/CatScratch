using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AionMeter.App.Windows;
using AionMeter.Core.Events;
namespace AionMeter.App.Services;
internal static class PvpChecks
{
    internal static void Run(string output)
    {
        var passed=new List<string>();void Check(bool ok,string label){if(!ok)throw new InvalidOperationException(label);passed.Add(label);}
        using var meter=new MeterService(new AppSettings{Transient=true,DownloadIcons=false,Language="de"},null);
        UiText.Use("de");
        var window=new PvpWindow(meter){Left=-30000,Top=-30000};
        WindowClickThrough.Attach(window,()=>false);
        IEnumerable<DependencyObject> Nodes(DependencyObject root){yield return root;foreach(var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())foreach(var n in Nodes(child))yield return n;}
        string Text()=>string.Join("\n",Nodes(window).OfType<TextBlock>().Select(t=>t.Text));
        void Flush()=>window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        try
        {
            window.Show();Flush();var now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            meter.Pvp.Observe(new SelfIdentifiedEvent(now,1,"Own",1,GameClass.Ranger));
            meter.Pvp.Observe(new PlayerSeenEvent(now,2,"Sample opponent",1,GameClass.Gladiator));
            meter.Pvp.Observe(new DamageEvent(now,1,2,1,12500,0));meter.Pvp.Observe(new DamageEvent(now,2,1,1,4200,0));
            meter.Pvp.Observe(new NpcHpEvent(now,2,37500,50000,false));window.Refresh();Flush();
            Check(Text().Contains("Sample opponent")&&Text().Contains("37.500"),"Confirmed opponent and HP render");
            var bitmap=new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth),(int)Math.Ceiling(window.ActualHeight),96,96,PixelFormats.Pbgra32);bitmap.Render(window);
            var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using(var file=File.Create(Path.ChangeExtension(output,"png")))png.Save(file);
            meter.Settings.StreamMode=true;window.Refresh();Check(!Text().Contains("Sample opponent"),"Stream mode masks opponent name");
            window.SetDisplayOnly(true);Check(Nodes(window).OfType<Button>().All(b=>b.Visibility!=Visibility.Visible),"Click-through hides module controls without moving content");window.SetDisplayOnly(false);
            using(var suppression=new MapHudSuppression()){suppression.Apply(true,[window]);Check(MapHudSuppression.IsSuppressed(window),"Map temporarily hides PvP");suppression.Apply(false,[window]);Check(!MapHudSuppression.IsSuppressed(window),"Map restores PvP");}
            Nodes(window).OfType<Button>().Single(b=>Equals(b.Content,"\uE72C")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Check(meter.Pvp.Snapshot(now).Outgoing==0,"PvP reset clears its session");
            var settings=new AppSettings{Transient=true,ShowPvpModule=true,PvpLeft=321,PvpTop=432};var profile=OverlayProfiles.Capture("pvp",settings,new TimerCards.State());
            var restored=new AppSettings{Transient=true};OverlayProfiles.Apply(profile,restored,new TimerCards.State());Check(restored.ShowPvpModule&&restored.PvpLeft==321&&restored.PvpTop==432,"Profiles preserve PvP visibility and position");
            var overlay=new OverlayWindow(meter,false){Left=-30000,Top=-30000,ShowActivated=false};
            var panel=new MeterControlsWindow(meter.Settings,()=>{},()=>{}){Left=-30000,Top=-30000,ShowActivated=false};
            try
            {
                overlay.MoveControlsTo(panel);overlay.Show();panel.Show();
                foreach(var layout in new[]{ControlsLayout.Horizontal,ControlsLayout.Vertical,ControlsLayout.Grid})
                {
                    meter.Settings.ControlsLayout=layout;panel.ApplyAppearance();panel.UpdateLayout();Flush();
                    var button=Nodes(panel).OfType<Button>().Single(b=>b.Name=="PvpButton");
                    var bounds=button.TransformToAncestor(panel).TransformBounds(new Rect(button.RenderSize));
                    Check(button.IsVisible&&bounds.Left>=0&&bounds.Top>=0&&bounds.Right<=panel.ActualWidth+.5&&bounds.Bottom<=panel.ActualHeight+.5,"PvP button fits control panel: "+layout);
                }
            }
            finally{panel.Close();overlay.Close();}
            File.WriteAllText(output,JsonSerializer.Serialize(passed));
        }
        finally{window.Close();}
    }
}
