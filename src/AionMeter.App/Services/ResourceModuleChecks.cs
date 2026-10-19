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
        meter.Resources.Observe(new Core.Game.OdyleEnergyEvent(1,15,495));meter.Resources.Observe(new Core.Game.ResourceBalanceSnapshot(1,new Dictionary<int,ulong>{{930100003,3188}}));
        var window=new ResourcesWindow(meter){Left=-30000,Top=-30000};WindowClickThrough.Attach(window,()=>false);
        try
        {
            window.Show();window.UpdateLayout();
            Check(Find<TextBlock>(window).Any(t=>t.Text.Contains("(+495)")),"Odyle keeps available and supplementary values separate");
            Check(Find<TextBlock>(window).Any(t=>t.Text.EndsWith(" *")),"Inventory balances are visibly marked as last received");
            var height=window.ActualHeight;var top=window.Top;
            WindowClickThrough.ApplyAll(true);window.UpdateLayout();Check(Math.Abs(window.ActualHeight-height)<.5 && window.Top==top,"Click-through preserves resource geometry");
            WindowClickThrough.ApplyAll(false);
            meter.Settings.VisibleResources=["odyle"];window.Refresh();window.UpdateLayout();
            Check(!Find<TextBlock>(window).Any(t=>t.Text=="Abyss Points") && Find<TextBlock>(window).Any(t=>t.Text=="Odyle Energy"),"Only selected resources appear");
            var draft=meter.Settings.CreateDraft();var panel=new ResourcesSettingsPanel(draft);
            var host=new Window{Content=panel,Width=500,Height=500,Left=-30000,Top=-30000};
            try{host.Show();host.UpdateLayout();var choices=Find<CheckBox>(host).ToArray();Check(choices.Length==6,"Settings offer module visibility and five independent resource choices");choices[2].IsChecked=true;choices[2].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Check(draft.VisibleResources.Contains("abyss") && !meter.Settings.VisibleResources.Contains("abyss"),"Resource selection stays in the settings draft until Save");}
            finally{host.Close();}
            meter.Resources.Reset();window.Refresh();Check(Find<TextBlock>(window).Any(t=>t.Text=="–"),"Unknown and reset values are never displayed as zero");
            var path=output+".settings";try{JsonSettingsFile.Write(path,meter.Settings);var restored=JsonSettingsFile.Read<AppSettings>(path)!;Check(restored.VisibleResources.SequenceEqual(meter.Settings.VisibleResources),"Resource selections survive serialization");}finally{foreach(var f in new[]{path,path+".bak"})if(File.Exists(f))File.Delete(f);}
            File.WriteAllText(output,JsonSerializer.Serialize(passed));
        }
        finally{window.Close();}
    }
}
