using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AionMeter.Core.Game;
using AionMeter.Core.Events;
using AionMeter.Core.Capture;
using AionMeter.App.Windows;
namespace AionMeter.App.Services;
internal static class ResourceToolsChecks
{
    private static IEnumerable<T> Find<T>(DependencyObject root) where T:DependencyObject
    {if(root is T found)yield return found;for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)foreach(var child in Find<T>(VisualTreeHelper.GetChild(root,i)))yield return child;}
    internal static void Run(string output)
    {
        var passed=new List<string>();void Check(bool value,string text){if(!value)throw new InvalidOperationException(text);passed.Add(text);}

        var cacheDir=Path.Combine(Path.GetTempPath(),"catscratch-switch-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(cacheDir);
        var switching=new ResourceBalances();var liveIdentity=new LiveIdentity();
        using(var cache=new ResourceProgressCache(switching,cacheDir))
        {
            void Send(GameEvent e){var resolved=liveIdentity.Observe(e)??e;switching.Observe(resolved);cache.Observe(resolved);}
            Send(new SelfIdentifiedEvent(1,99,"Zerafí",2305,GameClass.Cleric));Send(new OdyleEnergyEvent(2,60,525));
            Send(new ResourceSnapshotStartedEvent(3));Send(new OdyleEnergyEvent(4,175,270));
            Send(new DungeonEntriesEvent(4,new Dictionary<uint,DungeonEntryValue>{{4,new(14,0,14)}}));
            Check(cache.Snapshot().Single().Energy!.Available==60,"Quick-switch login packets do not overwrite the previous character cache");
            Send(new ZoneChangedEvent(5,1110,"Same map",false));Send(new SelfIdentifiedEvent(6,102,"Zerafi",2305,GameClass.Elementalist));
            Check(cache.Snapshot().Count==2 && cache.Snapshot().Single(p=>p.Name=="Zerafí").Energy!.Available==60 && cache.Snapshot().Single(p=>p.Name=="Zerafi").Energy!.Available==175,"Accented and plain character names retain separate cached resource values");
            Check(cache.Snapshot().Single(p=>p.Name=="Zerafi").Entries[4].Available==14,"Queued dungeon counts belong to the newly confirmed character");
        }
        using(var restoredCache=new ResourceProgressCache(new ResourceBalances(),cacheDir))Check(restoredCache.Snapshot().Count==2,"Both quick-switched character records survive a cache reload");
        foreach(var file in Directory.GetFiles(cacheDir))File.Delete(file);Directory.Delete(cacheDir);
        var settings=new AppSettings{Transient=true,DownloadIcons=false};settings.ResourceAlerts["odyle"]=new(){Enabled=true,Threshold=30};settings.ResourceAlerts["daily"]=new(){Enabled=true,Threshold=0};
        var state=new ResourceBalances();state.Observe(new SelfIdentifiedEvent(1,1,"One",2305,GameClass.Cleric));state.Observe(new OdyleEnergyEvent(2,15,500));state.Observe(new DungeonEntriesEvent(3,new Dictionary<uint,DungeonEntryValue>{{101,new(0,18,14)}}));
        using(var monitor=new ResourceAlertMonitor())
        {
            Check(monitor.Evaluate(settings,state).SequenceEqual(new[]{"odyle"}),"Confirmed low regular Odyle triggers once; supplementary dungeon entries count toward the total");
            Check(monitor.Evaluate(settings,state).Count==0,"Unchanged low values never repeat an alert");
            state.Observe(new OdyleEnergyEvent(4,45,500));Check(monitor.Evaluate(settings,state).Count==0,"Recovery above the threshold rearms the warning");
            state.MarkCached();state.Observe(new DungeonEntriesEvent(5,new Dictionary<uint,DungeonEntryValue>{{101,new(0,0,14)}}));Check(monitor.Evaluate(settings,state).SequenceEqual(new[]{"daily"}),"Confirmed empty dungeon total triggers without warning on cached Odyle");
            state.Observe(new OdyleEnergyEvent(6,15,500));Check(monitor.Evaluate(settings,state).SequenceEqual(new[]{"odyle"}),"Fresh confirmation can trigger a rearmed warning");
            state.Observe(new SelfIdentifiedEvent(7,2,"Other",2305,GameClass.Cleric));state.Observe(new OdyleEnergyEvent(8,15,0));Check(monitor.Evaluate(settings,state).SequenceEqual(new[]{"odyle"}),"Warning latches are scoped to the active character");
        }
        Check(!ResourceAlertMonitor.IsLow(settings,"odyle",0,true),"Approximate values never become active visual warnings");
        Check(ResourceDataStatus.Time(0).Contains("unknown") || ResourceDataStatus.Time(0).Contains("unbekannt"),"Legacy cache values retain unknown timestamps");
        Check(ResourceDataStatus.Time(1_700_000_000_000,1_700_000_180_000).Contains("3 min"),"Tooltip age is calculated from the value's own confirmation");
        var area=new Rect(0,0,1920,1080);var target=new Rect(200,200,100,80);
        var snapped=ModuleSnapping.Snap(new Rect(307,202,100,80),area,new[]{target},6,10);
        Check(snapped.X==306 && snapped.Y==200,"Module snapping aligns edges and maintains the selected six-pixel gap");
        Check(ModuleSnapping.Snap(new Rect(750,750,100,80),area,new[]{target},6,10)==new Rect(750,750,100,80),"Distant modules remain freely placed");
        Check(ModuleSnapping.Snap(new Rect(-1915,4,100,80),new Rect(-1920,0,1920,1080),Array.Empty<Rect>(),6,10).Location==new Point(-1920,0),"Snapping handles negative monitor coordinates and screen edges");
        Check(CaptureIndicator.Describe(new(CaptureState.Capturing,""),true).Color=="#82D6A3" && CaptureIndicator.Describe(new(CaptureState.Capturing,""),false).Color=="#FFB454" && CaptureIndicator.Describe(new(CaptureState.Error,""),false).Color=="#FF5F63","Data indicator distinguishes active data, missing identity and capture errors");
        var draft=settings.CreateDraft();draft.ResourceAlerts["odyle"].Threshold=99;Check(settings.ResourceAlerts["odyle"].Threshold==30,"Alert edits remain isolated in Settings drafts");
        var profile=OverlayProfiles.Capture("Tools",new AppSettings{Transient=true,SnapModules=true,ModuleSpacing=8},new TimerCards.State());
        OverlayProfiles.Apply(profile,settings,new TimerCards.State());Check(settings.SnapModules && settings.ModuleSpacing==8 && settings.ResourceAlerts["odyle"].Threshold==30,"Profiles carry snapping layout but never warning preferences");
        using var meter=new MeterService(new AppSettings{Transient=true,DownloadIcons=false},null);
        var now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var records=new[]{new ResourceProgress("Private One",2305,now,new(now,160,270),new(){{930100024,1500}},new(){{1,new(16,3,21)}},new(){{"currency:930100024",now}}),new ResourceProgress("Private Two",2306,now-3600000,null,new(){{930100024,3000}},new())};
        var overview=new ResourceOverviewWindow(meter,records){Left=-30000,Top=-30000};
        var previousStream=StreamNames.Enabled;
        try
        {
            overview.Show();overview.UpdateLayout();Check(Find<TextBlock>(overview).Any(t=>t.Text.Contains("Private One")) && Find<TextBlock>(overview).Any(t=>t.Text.Contains("Private Two")),"Character overview displays separate saved characters and servers");
            StreamNames.Enabled=true;overview.Refresh();overview.UpdateLayout();Check(!Find<TextBlock>(overview).Any(t=>t.Text.Contains("Private One")||t.Text.Contains("Private Two")),"Character overview honors stream-mode aliases");
        }
        finally{StreamNames.Enabled=previousStream;overview.Close();}
        var preferences=new ResourcesSettingsPanel(settings,meter);var host=new Window{Content=preferences,Left=-30000,Top=-30000,Width=600,Height=800};
        try
        {
            host.Show();host.UpdateLayout();var section=preferences.Children.OfType<Expander>().Single();section.IsExpanded=true;host.UpdateLayout();
            Check(Find<ResourceAlertsSettingsPanel>(host).Single().Children.OfType<DockPanel>().Count()==7,"Resources expose seven independent opt-in warning thresholds");
            Check(Find<ComboBox>(section).Single().Items.Count==3,"Warning audio offers three bundled sounds");
        }
        finally{host.Close();}
        var path=output+".settings";
        try{JsonSettingsFile.Write(path,settings);var loaded=JsonSettingsFile.Read<AppSettings>(path)!;Check(loaded.SnapModules && loaded.ModuleSpacing==8 && loaded.ResourceAlerts["odyle"].Threshold==30,"Warning preferences and snap spacing survive restart serialization");}
        finally{foreach(var f in new[]{path,path+".bak"})if(File.Exists(f))File.Delete(f);}
        File.WriteAllText(output,JsonSerializer.Serialize(passed));
    }
}
