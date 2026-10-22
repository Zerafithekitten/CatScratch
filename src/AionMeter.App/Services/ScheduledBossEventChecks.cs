using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AionMeter.App.Windows;
using AionMeter.Core.Events;
using AionMeter.Core.Combat;

namespace AionMeter.App.Services;

internal static class ScheduledBossEventChecks
{
    internal static void Run(Action<bool,string> check,string output)
    {
        using var meter=new MeterService(new AppSettings{Transient=true,DownloadIcons=false},null);
        using var cards=new TimerCards(meter,false);
        var anchor=new DateTimeOffset(2026,10,19,0,0,0,TimeSpan.Zero);
        foreach(var boss in AutomaticEventSchedule.Catalog.Where(e=>EventKinds.For(e.Id)=="BOSS"))
        foreach(var region in new[]{"Europe","NA East","NA West"})
        {
            cards.Saved.SelectedEvents=[boss.Id];cards.Saved.EventRegion=region;
            var valid=true;
            for(var day=0;day<7;day++)foreach(var hour in new[]{0,8,15,23})
            {
                var now=anchor.AddDays(day).AddHours(hour);
                var detached=cards.GetAt("auto:"+boss.Id,now);
                var events=cards.NextEvents(now).Where(e=>e.Id==boss.Id).ToArray();
                valid&=detached?.Next is {} at&&at>=now-TimeSpan.FromMinutes(2)&&at<=now.AddDays(7)&&events.Any(e=>e.StartsAt==at);
            }
            check(valid,$"{boss.Name}: selected and detached times remain available throughout the week in {region}");
        }
        cards.Saved.EventRegion="Europe";cards.Saved.SelectedEvents=["kaira","nahma","siege-bosses"];
        var sample=new DateTimeOffset(2026,10,22,10,5,0,TimeSpan.FromHours(2));
        var watcher=cards.GetAt("boss:2305:2600089",sample)!;
        check(watcher.Next==cards.GetAt("auto:kaira",sample)!.Next&&watcher.Source=="Community schedule · Shugo.GG","Exact Watcher Kaira NPC uses the explicitly labelled existing community schedule when live data is absent");
        check(cards.GetAt("boss:2305:2600098",sample)!.Next is null,"Executor Kaira never inherits Watcher Kaira's schedule");
        check(cards.NextEvents(sample).Count(e=>e.Id=="kaira")==1&&cards.NextEvents(sample).Single(e=>e.Id=="kaira").StartsAt>sample.AddHours(1),"Watcher Kaira stays visible with a concrete time beyond the next hour");
        foreach(var boss in AutomaticEventSchedule.Catalog.Where(e=>EventKinds.For(e.Id)=="BOSS"))
        {
            var at=cards.GetAt("auto:"+boss.Id,sample)!.Next!.Value;
            check(cards.GetAt("auto:"+boss.Id,at.AddSeconds(119))!.Next==at&&cards.GetAt("auto:"+boss.Id,at.AddMinutes(2))!.Next>at,$"{boss.Name}: Started duration expires exactly before the next occurrence");
        }
        var hub=new TimerHubWindow(cards){ClockOverride=()=>sample,Width=480,Left=-30000,Top=-30000,ShowActivated=false};
        try
        {
            hub.Show();hub.Reload();hub.UpdateLayout();
            IEnumerable<DependencyObject> Nodes(DependencyObject root)
            {
                yield return root;
                foreach(var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())foreach(var node in Nodes(child))yield return node;
            }
            var rows=Nodes(hub).OfType<Grid>().Where(g=>g.Tag is EventStart).ToArray();
            check(rows.Length==3&&rows.All(g=>g.Children.OfType<TextBlock>().Last().Text.Length>0),"All three selected schedule bosses render a non-empty countdown in the actual event module");
            var bitmap=new RenderTargetBitmap((int)Math.Ceiling(hub.ActualWidth),(int)Math.Ceiling(hub.ActualHeight),96,96,PixelFormats.Pbgra32);
            bitmap.Render(hub);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var image=File.Create(output+".scheduled.png");encoder.Save(image);
        }
        finally{hub.Close();}
        cards.Saved.SelectedEvents=[];cards.Saved.FavoriteEvents.Clear();
        var codes=cards.WorldBossOptions().Select(b=>b.Code).ToArray();
        cards.Saved.SelectedWorldBosses=codes.ToHashSet();meter.Timers.SetCurrentServer(2305);
        check(cards.SelectedWorldBossCards().All(b=>b.Next is null&&!string.IsNullOrWhiteSpace(TimerCards.Countdown(b,sample))),$"All {codes.Length} field-boss options preserve an explicit unknown state without source data");
        foreach(var code in codes)
        {
            meter.Timers.OnNotice(new BossNotice(code,1110,null,BossNoticeKind.Killed,sample.ToUnixTimeMilliseconds(),ServerId:2305));
            meter.Timers.SetRespawn(2305,code,60);
        }
        check(cards.SelectedWorldBossCards().Count==codes.Length&&cards.SelectedWorldBossCards().All(b=>b.Next==sample.AddHours(1)),$"All {codes.Length} field-boss options expose their known respawn time");
        var live=new TimerHubWindow(cards){ClockOverride=()=>sample,Left=-30000,Top=-30000};
        var running=new TimerHubWindow(cards,true){ClockOverride=()=>sample,Left=-30000,Top=-30000};
        IEnumerable<Grid> BossRows(DependencyObject root)
        {
            if(root is Grid{Tag:TimerCardData} row)yield return row;
            foreach(var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())foreach(var rowChild in BossRows(child))yield return rowChild;
        }
        void Flush()=>live.Dispatcher.Invoke(()=>{},System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        try
        {
            live.Show();running.Show();
            Task.Run(()=>{foreach(var code in codes)meter.Timers.SetRespawn(2305,code,90);}).GetAwaiter().GetResult();Flush();
            check(BossRows(live).Count()==codes.Length&&BossRows(live).All(r=>((TimerCardData)r.Tag).Next==sample.AddMinutes(90)&&!string.IsNullOrWhiteSpace(((TextBlock)r.Children[2]).Text)),"Every field boss updates its displayed time from capture-thread changes without manual Reload");
            foreach(var code in codes)meter.Timers.OnNotice(new BossNotice(code,1110,null,BossNoticeKind.Alive,sample.AddSeconds(1).ToUnixTimeMilliseconds(),ServerId:2305));Flush();
            check(!BossRows(live).Any()&&BossRows(running).Count()==codes.Length&&BossRows(running).All(r=>((TimerCardData)r.Tag).Alive),"Every confirmed alive field boss moves immediately from Upcoming to Started without manual Reload");
            foreach(var code in codes)meter.Timers.OnNotice(new BossNotice(code,1110,null,BossNoticeKind.Killed,sample.AddMinutes(3).ToUnixTimeMilliseconds(),ServerId:2305));Flush();
            check(BossRows(live).Count()==codes.Length&&!BossRows(running).Any(),"Every new boss kill immediately restores its respawn countdown and clears Started");
        }
        finally{live.Close();running.Close();}
        meter.Timers.SetCurrentServer(2306);
        check(cards.SelectedWorldBossCards().All(b=>b.Next is null),"The full boss catalog never borrows respawn times from another server");
    }
}
