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
            Task.Run(()=>meter.Pets.Observe(new PetSoulEvent(4,new Dictionary<int,int>{{1193,1}}))).GetAwaiter().GetResult();Flush();
            Check(Progress()=="11/25","Capture-thread soul delta updates the window immediately through its dispatcher");
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
            File.WriteAllText(output,JsonSerializer.Serialize(passed));
        }
        finally{window.Close();}
    }
}
