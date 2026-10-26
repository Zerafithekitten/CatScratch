using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AionMeter.App.Windows;
using AionMeter.Core.Events;
using AionMeter.Core.Game;

namespace AionMeter.App.Services;

internal static class PetLiveChecks
{
    internal static void Run(string output)
    {
        var passed=new List<string>();
        void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);passed.Add(message);}
        using var meter=new MeterService(new AppSettings{Transient=true,DownloadIcons=false,PetOnHitOnly=true},null);
        var window=new PetTrackerWindow(meter){Left=-30000,Top=-30000,ShowActivated=false};
        IEnumerable<DependencyObject> Nodes(DependencyObject root)
        {
            yield return root;
            foreach(var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())foreach(var node in Nodes(child))yield return node;
        }
        void Flush()=>window.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        void Attack(long at)
        {
            meter.Pets.Observe(new NpcSeenEvent(at,7,2400455));
            meter.Pets.Observe(new DamageEvent(at,1,7,17010000,100,HitFlags.None));
        }
        try
        {
            window.Show();
            var list=Nodes(window).OfType<ListBox>().Single();
            string Progress()=>list.Items.OfType<PetTrackerWindow.Row>().Single().Progress;
            meter.Pets.Observe(new SelfIdentifiedEvent(1,1,"Pet live A",2305,GameClass.Cleric));
            meter.Pets.Observe(new PetCollectionEvent(2,new Dictionary<int,PetState>{{1193,new(1193,1,10)}}));Attack(3);Flush();
            Check(Progress()=="10/25","Live baseline reaches the window without manual Refresh");
            using(var suppression=new MapHudSuppression())
            {
                suppression.Apply(true,[window]);
                Task.Run(()=>meter.Pets.Observe(new PetSoulEvent(4,new Dictionary<int,int>{{1193,1}}))).GetAwaiter().GetResult();Flush();
                Check(Progress()=="10/25"&&meter.Pets.Snapshot().Single().Souls==11,"Map suppression pauses pet drawing while capture-thread progress continues");
                suppression.Apply(false,[window]);
                Check(Progress()=="11/25","Closing the map immediately displays accumulated pet progress without waiting for the next timer");
            }
            meter.Pets.Observe(new PetSoulEvent(5,new Dictionary<int,int>{{1193,14}}));Flush();
            Check(Progress()=="0/75","Level-up replaces both soul count and next-level requirement live");
            var saved=meter.Pets.SavedProgress()!;
            meter.Pets.Observe(new SelfIdentifiedEvent(6,1,"Pet live B",2305,GameClass.Cleric));Flush();
            Check(list.Items.Count==0&&!meter.Pets.Confirmed,"Character switch immediately removes previous character progress");
            meter.Pets.Observe(new SelfIdentifiedEvent(7,1,saved.Name,saved.ServerId,GameClass.Cleric));
            meter.Pets.RestoreProgress(saved);Attack(8);Flush();
            Check(Progress()=="~0/75","Restored last-known counters remain visibly marked");
            meter.Pets.Observe(new PetSoulEvent(9,new Dictionary<int,int>{{1193,1}}));Flush();
            Check(Progress()=="~1/75","New soul deltas continue updating a cached baseline live");
            meter.Pets.Observe(new PetCollectionEvent(10,new Dictionary<int,PetState>{{1193,new(1193,2,2)}}));Flush();
            Check(Progress()=="2/75","Fresh full progress replaces cached counters and removes the estimate marker live");
            meter.Pets.Observe(new PetSoulEvent(11,new Dictionary<int,int>{{1193,73}}));Flush();
            Check(list.Items.Count==0,"Max-level pet disappears immediately after the final souls");
            var cacheDirectory=Path.Combine(Path.GetTempPath(),"CatScratch-pet-persist-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(cacheDirectory);
            try
            {
                var pets=new PetCollection();var own=new SelfIdentifiedEvent(1,1,"Cache verification",2305,GameClass.Cleric);
                using(var cache=new PetProgressCache(pets,meter.Data,cacheDirectory))
                {
                    pets.Observe(own);pets.Observe(new PetCollectionEvent(2,new Dictionary<int,PetState>{{1193,new(1193,1,10)}}));
                    pets.Observe(new PetSoulEvent(3,new Dictionary<int,int>{{1193,2}}));
                }
                var restored=new PetCollection();restored.Observe(own);
                using(var cache=new PetProgressCache(restored,meter.Data,cacheDirectory))cache.Identity(own);
                Check(restored.Confirmed&&restored.FromCache&&restored.Snapshot().Single().Souls==12,"Shutdown flush persists the latest pet snapshot and restores it for the matching character");
            }
            finally{foreach(var file in Directory.GetFiles(cacheDirectory))File.Delete(file);Directory.Delete(cacheDirectory);}
            var now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            void Feed(GameEvent value){meter.Pets.Observe(value);meter.PetNavigation.Observe(value);}
            meter.Settings.PetOnHitOnly=false;
            Feed(new SelfIdentifiedEvent(now-200,1,"Navigation sample",2305,GameClass.Cleric));
            Feed(new ZoneChangedEvent(now-190,1110,"Altgard",false));
            Feed(new PetCollectionEvent(now-180,new Dictionary<int,PetState>{{1193,new(1193,1,11)},{1176,new(1176,1,8)}}));
            Feed(new ActorPositionEvent(now-10,1,10,20,30));
            Feed(new NpcSeenEvent(now-10,7,2400455,X:1010,Y:1020,Z:30));
            Feed(new NpcSeenEvent(now-10,8,2400707,X:-1990,Y:1020,Z:30));
            Flush();window.Refresh();window.UpdateLayout();
            var rows=list.Items.OfType<PetTrackerWindow.Row>().ToArray();
            Check(rows[0].Definition.Id==1193&&rows[0].Direction.Length==0&&rows[0].Distance.StartsWith("≈ "),"A missing character heading leaves distance available without substituting a world-axis arrow");
            Check(rows[0].Distance=="≈ 14 m","The 1,414-world-unit diagonal is displayed as approximately 14 metres");
            Check(PetNavigationText.Distance(new(0,1,22559,0,0,50,null,true,true))=="~ 226 m","Historical farm distances use the same metre conversion and retain their marker");
            SavePreview(window,Path.ChangeExtension(output,"png"));
            Feed(new PetFacingEvent(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),1,0));window.Refresh();
            rows=list.Items.OfType<PetTrackerWindow.Row>().ToArray();
            Check(rows[0].Direction=="↗","A fresh character orientation enables a character-relative arrow");
            Check(rows[0].Tooltip.Contains("character facing")&&rows[0].Tooltip.Contains("not the camera")&&rows[0].Tooltip.Contains("100 world units = 1 m"),"Tooltips explain character orientation and the validated conversion to metres");
            Feed(new PetFacingEvent(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),1,90));
            var turnFrame=new DispatcherFrame();var turnWait=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(550)};
            turnWait.Tick+=(_,_)=>{turnWait.Stop();turnFrame.Continue=false;};turnWait.Start();Dispatcher.PushFrame(turnFrame);
            Check(list.Items.OfType<PetTrackerWindow.Row>().Single(r=>r.Definition.Id==1193).Direction=="↖","Turning the character without moving rotates the arrow through the live timer");
            var moveAt=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Feed(new ActorPositionEvent(moveAt,7,-990,1020,30));
            var frame=new DispatcherFrame();var wait=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(600)};
            wait.Tick+=(_,_)=>{wait.Stop();frame.Continue=false;};wait.Start();Dispatcher.PushFrame(frame);
            Check(list.Items.OfType<PetTrackerWindow.Row>().Single(r=>r.Definition.Id==1193).Direction=="↗","NPC movement updates its arrow through the live window timer without manual Refresh");
            Thread.Sleep(1600);window.Refresh();
            Check(list.Items.OfType<PetTrackerWindow.Row>().All(r=>r.Direction.Length==0&&(r.Distance.Length==0||r.Distance.StartsWith("~ ")&&r.Tooltip.Contains("not the current position"))),"Stale coordinates remove live guidance; historical farm distances are explicitly labeled and never gain arrows");
            Check(list.Items.Count>0,"Loss of navigation does not remove the normal region recommendations");
            Check(Nodes(window).OfType<TextBlock>().Any(t=>t.Text.Contains("last position")||t.Text.Contains("letzte Position")),"Historical farm distances show the age of their confirmed origin");
            window.UpdateLayout();SavePreview(window,Path.ChangeExtension(output,"last-known.png"));
            Feed(new ZoneChangedEvent(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),1110,"Altgard",false,true));window.Refresh();
            Check(Nodes(window).OfType<TextBlock>().Any(t=>t.Text.Contains("Distance: own position unavailable")||t.Text.Contains("Entfernung: eigene Position fehlt")),"Missing own coordinates are explained instead of silently omitting distances");
            Check(PetNavigationText.Arrow(0)=="→"&&PetNavigationText.Arrow(90)=="↑"&&PetNavigationText.Arrow(-90)=="↓"&&PetNavigationText.Arrow(double.NaN)=="","Arrow sectors handle signed angles and reject invalid bearings");
            var fresh=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();Feed(new ActorPositionEvent(fresh,1,10,20,30));
            window.Refresh();
            Check(list.Items.OfType<PetTrackerWindow.Row>().Any(r=>r.Name.StartsWith("◇ ")&&r.Distance.StartsWith("≈ ")&&r.Tooltip.Contains("not a confirmed current spawn")),"Fresh own position shows clearly identified farm-location distance even without a received source NPC");
            window.UpdateLayout();SavePreview(window,Path.ChangeExtension(output,"farm.png"));
            meter.SetPetNavigationActive(true);meter.SetPetNavigationActive(false);
            Check(meter.PetNavigation.Snapshot(fresh) is not null,"Closing an additional preview keeps the remaining tracker navigation active");
            window.Close();
            Check(meter.PetNavigation.Snapshot(fresh) is null,"Closing the final tracker clears its position guidance");
            File.WriteAllText(output,JsonSerializer.Serialize(passed));
        }
        finally{window.Close();}
    }
    private static void SavePreview(Window window,string path)
    {
        var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth),(int)Math.Ceiling(window.ActualHeight),96,96,System.Windows.Media.PixelFormats.Pbgra32);
        bitmap.Render(window);var png=new System.Windows.Media.Imaging.PngBitmapEncoder();png.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
        using var file=File.Create(path);png.Save(file);
    }
}
