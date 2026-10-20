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
            Check(ResourcesWindow.Entries.All(e=>ResourceIcons.For(e.Key) is System.Windows.Media.Imaging.BitmapSource {PixelWidth:>0,PixelHeight:>0,IsFrozen:true}),"Every resource has a bundled frozen game icon");
            Check(Find<Image>(window).Count(i=>i.Width==18)==5,"Each selected resource row displays its matching icon");
            Check(Find<TextBlock>(window).Any(t=>t.Text.Contains("(+495)")),"Odyle keeps available and supplementary values separate");
            Check(Find<TextBlock>(window).Any(t=>t.Text.EndsWith(" *")),"Inventory balances are visibly marked as last received");
            var height=window.ActualHeight;var top=window.Top;
            WindowClickThrough.ApplyAll(true);window.UpdateLayout();Check(Math.Abs(window.ActualHeight-height)<.5 && window.Top==top,"Click-through preserves resource geometry");
            WindowClickThrough.ApplyAll(false);
            foreach(var layoutName in new[]{"List","Strip","Grid"})
            {
                meter.Settings.ResourcesLayout=layoutName;window.Refresh();window.UpdateLayout();
                Check(Find<Image>(window).Count(i=>i.Width==18)==5,"All selected resource icons survive layout switch: "+layoutName);
                var before=new Size(window.ActualWidth,window.ActualHeight);WindowClickThrough.ApplyAll(true);window.UpdateLayout();
                Check(Math.Abs(window.ActualWidth-before.Width)<.5 && Math.Abs(window.ActualHeight-before.Height)<.5,"Click-through preserves layout geometry: "+layoutName);WindowClickThrough.ApplyAll(false);
            }
            meter.Settings.VisibleResources=["odyle"];window.Refresh();window.UpdateLayout();
            Check(!Find<TextBlock>(window).Any(t=>t.Text=="Abyss Points") && Find<TextBlock>(window).Any(t=>t.Text=="Odyle Energy"),"Only selected resources appear");
            meter.Settings.ResourcesLayout="List";var draft=meter.Settings.CreateDraft();var panel=new ResourcesSettingsPanel(draft);
            var host=new Window{Content=panel,Width=500,Height=500,Left=-30000,Top=-30000};
            try{host.Show();host.UpdateLayout();var choices=Find<CheckBox>(host).ToArray();Check(choices.Length==6,"Settings offer module visibility and five independent resource choices");var layout=Find<ComboBox>(host).Single();layout.SelectedIndex=2;Check(draft.ResourcesLayout=="Grid" && meter.Settings.ResourcesLayout!="Grid","Layout changes stay in the Settings draft");choices[2].IsChecked=true;choices[2].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Check(draft.VisibleResources.Contains("abyss") && !meter.Settings.VisibleResources.Contains("abyss"),"Resource selection stays in the settings draft until Save");}
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
                meter.Settings.VisibleDungeonEntries=["nightmare"];dungeon.Refresh();dungeon.UpdateLayout();
                Check(!Find<TextBlock>(dungeon).Any(t=>t.Text=="Daily Dungeon") && Find<TextBlock>(dungeon).Any(t=>t.Text=="Nightmare"),"Dungeon selection hides unselected rows");
                var draftDungeon=meter.Settings.CreateDraft();var panelDungeon=new DungeonEntriesSettingsPanel(draftDungeon);var hostDungeon=new Window{Content=panelDungeon,Width=500,Height=400,Left=-30000,Top=-30000};
                try{hostDungeon.Show();hostDungeon.UpdateLayout();var boxes=Find<CheckBox>(hostDungeon).ToArray();Check(boxes.Length==4,"Dungeon settings contain module toggle and three choices");boxes[1].IsChecked=true;boxes[1].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Check(draftDungeon.VisibleDungeonEntries.Contains("daily") && !meter.Settings.VisibleDungeonEntries.Contains("daily"),"Dungeon settings remain in draft until Save");}finally{hostDungeon.Close();}
            }
            finally{dungeon.Close();}
            File.WriteAllText(output,JsonSerializer.Serialize(passed));
        }
        finally{window.Close();}
    }
}
