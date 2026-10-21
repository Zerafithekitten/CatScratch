using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AionMeter.App.Windows;
namespace AionMeter.App.Services;
internal static class ResourceModuleChecks
{
    private static IEnumerable<T> Find<T>(DependencyObject root) where T:DependencyObject
    {if(root is T found)yield return found;for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)foreach(var child in Find<T>(VisualTreeHelper.GetChild(root,i)))yield return child;}
    internal static void Run(string output)
    {
        var passed=new List<string>();void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);passed.Add(message);}
        using var meter=new MeterService(new AppSettings{Transient=true,DownloadIcons=false},null);
        var feed=typeof(MeterService).GetMethod("ProcessLiveEvent",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.CreateDelegate<Action<Core.Events.GameEvent>>(meter);
        feed(new Core.Events.SelfIdentifiedEvent(1,1,"Resource test",0,Core.Events.GameClass.Cleric));
        feed(new Core.Game.OdyleEnergyEvent(2,15,495));feed(new Core.Game.ResourceBalanceSnapshot(2,new Dictionary<int,ulong>{{930100003,3188}}));
        feed(new Core.Events.SelfIdentifiedEvent(3,2,"",0,Core.Events.GameClass.Unknown));
        feed(new Core.Game.ResourceBalanceSnapshot(4,new Dictionary<int,ulong>()));
        feed(new Core.Events.PlayerSeenEvent(5,2,"Resource test",2305,Core.Events.GameClass.Cleric));
        Check(meter.Resources.Read().Energy?.Supplementary==495 && meter.Resources.Read().Balances.Count==1,"Live event routing preserves balances across placeholder identity, partial inventory and resolved player identity");
        var window=new ResourcesWindow(meter){Left=-30000,Top=-30000};WindowClickThrough.Attach(window,()=>false);
        try
        {
            window.Show();window.UpdateLayout();
            var originalSelection=meter.Settings.VisibleResources.ToList();
            meter.Settings.VisibleResources=["cube-keys"];window.Refresh();window.UpdateLayout();
            Check(Find<TextBlock>(window).Any(t=>t.Text=="⚠ Relog!"),"Unknown cube key inventory is not invented as zero");
            feed(new Core.Game.CubeKeyInventoryEvent(6,true,new Dictionary<ulong,uint>{{10,20}}));window.Refresh();window.UpdateLayout();
            Check(Find<TextBlock>(window).Any(t=>t.Text=="20"),"Confirmed cube key baseline appears in Resources");
            feed(new Core.Game.CubeKeyInventoryEvent(7,false,new Dictionary<ulong,uint>{{10,19}}));window.Refresh();window.UpdateLayout();
            Check(Find<TextBlock>(window).Any(t=>t.Text=="19"),"Cube key spending updates Resources live");
            meter.Resources.MarkCached();window.Refresh();window.UpdateLayout();
            Check(Find<TextBlock>(window).Any(t=>t.Text=="≈ 19"),"Cached cube keys retain an approximate marker");
            feed(new Core.Game.CubeKeyInventoryEvent(8,true,new Dictionary<ulong,uint>()));window.Refresh();window.UpdateLayout();
            Check(Find<TextBlock>(window).Any(t=>t.Text=="0"),"A confirmed empty cube key inventory is displayed as zero");
            feed(new Core.Game.CubeKeyInventoryEvent(9,true,new Dictionary<ulong,uint>{{10,20}}));
            meter.Settings.VisibleResources=["odyle","abyss","cube-keys"];
            foreach(var example in new[]{"List","Strip","Grid"})
            {
                meter.Settings.ResourcesLayout=example;window.Refresh();window.UpdateLayout();
                Check(Find<TextBlock>(window).Any(t=>t.Text=="20"),"Cube key count survives layout: "+example);
                var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth),(int)Math.Ceiling(window.ActualHeight),96,96,PixelFormats.Pbgra32);bitmap.Render(window);
                var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                using var image=File.Create(Path.Combine(Path.GetDirectoryName(output)!,"cube-key-resources-"+example+".png"));encoder.Save(image);
            }
            feed(new Core.Game.OdyleEnergyEvent(10,15,495));feed(new Core.Game.ResourceBalanceSnapshot(10,new Dictionary<int,ulong>{{930100003,3188}}));
            meter.Settings.VisibleResources=originalSelection;meter.Settings.ResourcesLayout="List";window.Refresh();window.UpdateLayout();
            Check(ResourcesWindow.Entries.All(e=>ResourceIcons.For(e.Key) is {IsFrozen:true}),"Every resource has a bundled frozen game icon");
            Check(Find<Image>(window).Count(i=>i.Width==18)==7,"Each selected resource row displays its matching icon");
            Check(Find<TextBlock>(window).Any(t=>t.Text.Contains("(+495)")),"Odyle keeps available and supplementary values separate");
            Check(Find<TextBlock>(window).Any(t=>t.Text.Contains("3") && t.Text.Contains("188") && !t.Text.EndsWith(" *")),"Live Abyss balance has no snapshot marker");
            feed(new Core.Game.ResourceBalanceSnapshot(3,new Dictionary<int,ulong>{{930100024,1000},{930100011,1670},{930100017,160}}));window.Refresh();window.UpdateLayout();
            Check(Find<TextBlock>(window).Count(t=>t.Text.EndsWith(" *"))==0,"All confirmed live currency balances have no snapshot marker");
            var height=window.ActualHeight;var top=window.Top;
            WindowClickThrough.ApplyAll(true);window.UpdateLayout();Check(Math.Abs(window.ActualHeight-height)<.5 && window.Top==top,"Click-through preserves resource geometry");
            WindowClickThrough.ApplyAll(false);
            foreach(var layoutName in new[]{"List","Strip","Grid"})
            {
                meter.Settings.ResourcesLayout=layoutName;window.Refresh();window.UpdateLayout();
                Check(Find<Image>(window).Count(i=>i.Width==18)==7,"All selected resource icons survive layout switch: "+layoutName);
                var before=new Size(window.ActualWidth,window.ActualHeight);WindowClickThrough.ApplyAll(true);window.UpdateLayout();
                Check(Math.Abs(window.ActualWidth-before.Width)<.5 && Math.Abs(window.ActualHeight-before.Height)<.5,"Click-through preserves layout geometry: "+layoutName);WindowClickThrough.ApplyAll(false);
            }
            foreach(var missingStrip in new[]{false,true})
            {
                meter.Settings.ResourcesLayout="Strip";
                if(missingStrip)meter.Resources.Reset();
                window.Refresh();window.UpdateLayout();
                foreach(var tile in Find<Border>(window).Where(b=>b.CornerRadius.TopLeft==4))
                {
                    var group=Find<StackPanel>(tile).Single(p=>p.Orientation==Orientation.Horizontal);
                    var box=group.TransformToAncestor(tile).TransformBounds(new Rect(group.RenderSize));
                    Check(Math.Abs(box.Left+box.Width/2-tile.ActualWidth/2)<=.6 && Math.Abs(box.Top+box.Height/2-tile.ActualHeight/2)<=.6,"Horizontal resource group is centered with "+(missingStrip?"Relog warning":"balance"));
                }
            }
            var retained=Find<Image>(window).First(i=>i.Width==18);window.Refresh();
            Check(ReferenceEquals(retained,Find<Image>(window).First(i=>i.Width==18)),"Unchanged resource refresh retains existing controls");
            var previousWidth=meter.Settings.ResourcesWidth;
            meter.Resources.Observe(new Core.Game.ResourceBalanceSnapshot(6,new Dictionary<int,ulong>{{930100003,999999999999},{930100011,999999999999},{930100024,999999999999},{930100017,999999999999},{930100019,999999999999}}));
            foreach(var width in new[]{220d,300d,600d})
            {
                meter.Settings.ResourcesLayout="Grid";meter.Settings.ResourcesWidth=width;window.Refresh();window.UpdateLayout();
                foreach(var value in Find<TextBlock>(window).Where(t=>t.Text.Length>10 && char.IsDigit(t.Text[0]) && t.Text.Contains("999")))
                {
                    var tile=Find<Border>(window).First(b=>b.CornerRadius.TopLeft==4 && b.IsAncestorOf(value));
                    var bounds=value.TransformToAncestor(tile).TransformBounds(new Rect(value.RenderSize));
                    Check(bounds.Left>=0 && bounds.Right<=tile.ActualWidth+.6,"Large resource number remains inside its tile at width "+width);
                }
                var rows=Find<Border>(window).Where(b=>b.CornerRadius.TopLeft==4).ToArray();
                Check(rows.Max(b=>b.ActualHeight)-rows.Min(b=>b.ActualHeight)<.6,"Resource grid tiles have matching heights at width "+width);
            }
            meter.Settings.ResourcesWidth=previousWidth;
            meter.Settings.VisibleResources=["odyle"];window.Refresh();window.UpdateLayout();
            Check(!Find<TextBlock>(window).Any(t=>t.Text=="Abyss Points") && Find<TextBlock>(window).Any(t=>t.Text=="Odyle Energy"),"Only selected resources appear");
            meter.Settings.ResourcesLayout="List";var draft=meter.Settings.CreateDraft();var panel=new ResourcesSettingsPanel(draft);
            var host=new Window{Content=panel,Width=500,Height=500,Left=-30000,Top=-30000};
            try{host.Show();host.UpdateLayout();var choices=panel.Children.OfType<CheckBox>().ToArray();Check(choices.Length==8,"Settings offer module visibility and seven independent resource choices");var layout=panel.Children.OfType<ComboBox>().Single();layout.SelectedIndex=2;Check(draft.ResourcesLayout=="Grid" && meter.Settings.ResourcesLayout!="Grid","Layout changes stay in the Settings draft");choices[2].IsChecked=true;choices[2].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Check(draft.VisibleResources.Contains("abyss") && !meter.Settings.VisibleResources.Contains("abyss"),"Resource selection stays in the settings draft until Save");}
            finally{host.Close();}
            meter.Resources.Reset();
            foreach(var layoutName in new[]{"List","Strip","Grid"})
            {
                meter.Settings.ResourcesLayout=layoutName;window.Refresh();window.UpdateLayout();
                Check(Find<TextBlock>(window).Any(t=>t.Text=="⚠ Relog!") && !Find<TextBlock>(window).Any(t=>t.Text=="–"),"Missing resource data displays a relog warning: "+layoutName);
            }
            meter.Resources.Observe(new Core.Game.OdyleEnergyEvent(1,0,0));window.Refresh();
            Check(Find<TextBlock>(window).Any(t=>t.Text.Contains("(+0)")) && !Find<TextBlock>(window).Any(t=>t.Text=="⚠ Relog!"),"Confirmed zero values replace the warning and remain visible");
            var path=output+".settings";try{JsonSettingsFile.Write(path,meter.Settings);var restored=JsonSettingsFile.Read<AppSettings>(path)!;Check(restored.VisibleResources.SequenceEqual(meter.Settings.VisibleResources),"Resource selections survive serialization");}finally{foreach(var f in new[]{path,path+".bak"})if(File.Exists(f))File.Delete(f);}
            var dungeon=new DungeonEntriesWindow(meter){Left=-30000,Top=-30000};WindowClickThrough.Attach(dungeon,()=>false);
            try
            {
                dungeon.Show();dungeon.UpdateLayout();
                Check(Find<TextBlock>(dungeon).Count(t=>t.Text=="⚠ Relog!")==3,"Dungeon missing counts warn instead of guessing");
                feed(new Core.Game.DungeonEntriesEvent(1,new Dictionary<uint,Core.Game.DungeonEntryValue>{{101,new(0,18,14)},{4,new(0,3,14)},{1,new(17,3,21)}}));dungeon.Refresh();dungeon.UpdateLayout();
                Check(Find<TextBlock>(dungeon).Any(t=>t.Text=="0 (+18)/14") && Find<TextBlock>(dungeon).Any(t=>t.Text=="17 (+3)/21"),"Dungeon live events reach the separate display");
                var before=new Size(dungeon.ActualWidth,dungeon.ActualHeight);var topBefore=dungeon.Top;
                WindowClickThrough.ApplyAll(true);dungeon.UpdateLayout();
                Check(before==new Size(dungeon.ActualWidth,dungeon.ActualHeight) && topBefore==dungeon.Top,"Dungeon click-through preserves position and dimensions");WindowClickThrough.ApplyAll(false);
                foreach(var dungeonLayout in new[]{"List","Strip","Grid"})
                {
                    meter.Settings.DungeonEntriesLayout=dungeonLayout;dungeon.Refresh();dungeon.UpdateLayout();
                    Check(Find<TextBlock>(dungeon).Count(t=>t.Text.Contains("(+"))==3,"All selected dungeon counts appear in "+dungeonLayout);
                    var size=new Size(dungeon.ActualWidth,dungeon.ActualHeight);var y=dungeon.Top;
                    WindowClickThrough.ApplyAll(true);dungeon.UpdateLayout();
                    Check(size==new Size(dungeon.ActualWidth,dungeon.ActualHeight) && y==dungeon.Top,"Dungeon click-through preserves geometry in "+dungeonLayout);WindowClickThrough.ApplyAll(false);
                    if(dungeonLayout=="Strip")foreach(var tile in Find<Border>(dungeon).Where(b=>b.CornerRadius.TopLeft==4))
                    {
                        var group=Find<StackPanel>(tile).Single(g=>g.Orientation==Orientation.Horizontal);
                        var box=group.TransformToAncestor(tile).TransformBounds(new Rect(group.RenderSize));
                        Check(Math.Abs(box.Left+box.Width/2-tile.ActualWidth/2)<.6,"Horizontal dungeon groups are centered");
                    }
                }
                meter.Settings.DungeonEntriesLayout="List";dungeon.Refresh();dungeon.UpdateLayout();
                var retainedDungeon=Find<TextBlock>(dungeon).First(t=>t.Text=="Daily Dungeon");dungeon.Refresh();
                Check(ReferenceEquals(retainedDungeon,Find<TextBlock>(dungeon).First(t=>t.Text=="Daily Dungeon")),"Unchanged dungeon refresh retains existing controls");
                meter.Settings.VisibleDungeonEntries=["nightmare"];dungeon.Refresh();dungeon.UpdateLayout();
                Check(!Find<TextBlock>(dungeon).Any(t=>t.Text=="Daily Dungeon") && Find<TextBlock>(dungeon).Any(t=>t.Text=="Nightmare"),"Dungeon selection hides unselected rows");
                var draftDungeon=meter.Settings.CreateDraft();var panelDungeon=new DungeonEntriesSettingsPanel(draftDungeon);var hostDungeon=new Window{Content=panelDungeon,Width=500,Height=400,Left=-30000,Top=-30000};
                try{hostDungeon.Show();hostDungeon.UpdateLayout();var boxes=panelDungeon.Children.OfType<CheckBox>().ToArray();Check(boxes.Length==4,"Dungeon settings contain module toggle and three choices");var dungeonLayoutBox=panelDungeon.Children.OfType<ComboBox>().Single();dungeonLayoutBox.SelectedIndex=2;Check(draftDungeon.DungeonEntriesLayout=="Grid" && meter.Settings.DungeonEntriesLayout=="List","Dungeon layout remains in Settings draft until Save");boxes[1].IsChecked=true;boxes[1].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Check(draftDungeon.VisibleDungeonEntries.Contains("daily") && !meter.Settings.VisibleDungeonEntries.Contains("daily"),"Dungeon settings remain in draft until Save");}finally{hostDungeon.Close();}
            }
            finally{dungeon.Close();}
            meter.Settings.DungeonEntriesLayout="Grid";meter.Settings.DungeonEntriesWidth=360;
            var layoutProfile=OverlayProfiles.Capture("Dungeon layout",meter.Settings,new TimerCards.State());
            var restoredLayout=new AppSettings{Transient=true,VisibleDungeonEntries=["shugo"]};
            OverlayProfiles.Apply(layoutProfile,restoredLayout,new TimerCards.State());
            Check(restoredLayout.DungeonEntriesLayout=="Grid" && restoredLayout.DungeonEntriesWidth==360 && restoredLayout.VisibleDungeonEntries.SequenceEqual(new[]{"shugo"}),"Dungeon profiles restore layout and width without replacing the user's selection");
            var dungeonSettingsPath=output+".dungeons.settings";
            try{JsonSettingsFile.Write(dungeonSettingsPath,meter.Settings);var restored=JsonSettingsFile.Read<AppSettings>(dungeonSettingsPath)!;Check(restored.DungeonEntriesLayout=="Grid" && restored.DungeonEntriesWidth==360,"Dungeon layout and width survive serialization");}
            finally{foreach(var dungeonFile in new[]{dungeonSettingsPath,dungeonSettingsPath+".bak"})if(File.Exists(dungeonFile))File.Delete(dungeonFile);}

            var cacheDirectory=output+".cache";
            var cacheStore=new Core.Game.ResourceBalances();
            var cacheOwn=new Core.Events.SelfIdentifiedEvent(1,1,"Cache test",2305,Core.Events.GameClass.Cleric);
            using(var cache=new ResourceProgressCache(cacheStore,cacheDirectory))
            {
                cacheStore.Observe(cacheOwn);cache.Observe(cacheOwn);
                var balance=new Core.Game.ResourceBalanceSnapshot(2,new Dictionary<int,ulong>{{930100024,1500}});cacheStore.Observe(balance);cache.Observe(balance);
                var entries=new Core.Game.DungeonEntriesEvent(3,new Dictionary<uint,Core.Game.DungeonEntryValue>{{1,new(16,3,21)}});cacheStore.Observe(entries);cache.Observe(entries);
            }
            var restoredCache=new Core.Game.ResourceBalances();
            using(var cache=new ResourceProgressCache(restoredCache,cacheDirectory))
            {
                Check(restoredCache.Read().Balances.Count==0,"Disk cache never restores before fresh own identity");
                restoredCache.Observe(cacheOwn);cache.Observe(cacheOwn);
                Check(restoredCache.Read().Balances[930100024]==1500 && restoredCache.ReadEntries()[1].Available==16 && restoredCache.CacheMarkers().Currencies.Contains(930100024),"Disk cache restores matching character resources and dungeon counts as approximate");
                var spend=new Core.Game.DungeonEntriesEvent(4,new Dictionary<uint,Core.Game.DungeonEntryValue>{{1,new(15,3,null)}});restoredCache.Observe(spend);cache.Observe(spend);
                Check(restoredCache.ReadEntries()[1].Available==15 && !restoredCache.CacheMarkers().Entries.Contains(1),"Confirmed consumption replaces restored count without duplicate subtraction");
                var other=cacheOwn with {Name="Other character"};restoredCache.Observe(other);cache.Observe(other);
                Check(restoredCache.Read().Balances.Count==0 && restoredCache.ReadEntries().Count==0,"Character switch cannot display another character's cached data");
            }
            meter.Settings.VisibleResources=["passcoin"];meter.Resources.Observe(new Core.Game.ResourceBalanceSnapshot(1,new Dictionary<int,ulong>{{930100024,1500}}));meter.Resources.MarkCached();window.Refresh();window.UpdateLayout();
            Check(Find<TextBlock>(window).Any(t=>t.Text.StartsWith("≈ ")),"Approximate resource values have an explicit visible marker");
            meter.Resources.Observe(new Core.Game.ResourceBalanceSnapshot(2,new Dictionary<int,ulong>{{930100024,1500}}));window.Refresh();window.UpdateLayout();
            Check(!Find<TextBlock>(window).Any(t=>t.Text.StartsWith("≈ ")),"Equal live confirmation removes the approximate marker and redraws the resource row");
            File.WriteAllText(output,JsonSerializer.Serialize(passed));
        }
        finally{window.Close();}
    }
}
