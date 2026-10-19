using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AionMeter.App.Windows;
using AionMeter.Core.Combat;
namespace AionMeter.App.Services;

internal static class WorldBossEventChecks
{
    private static IEnumerable<T> Find<T>(DependencyObject root) where T:DependencyObject
    {
        if(root is T match)yield return match;
        for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)foreach(var child in Find<T>(VisualTreeHelper.GetChild(root,i)))yield return child;
    }
    internal static void Run(string output)
    {
        var passed=new List<string>();void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);passed.Add(message);}
        using var meter=new MeterService(new AppSettings{Transient=true,DownloadIcons=false},null);
        using var cards=new TimerCards(meter,false);
        using(var watchMeter=new MeterService(new AppSettings{Transient=true,DownloadIcons=false},null))
        {
            const int watchCode=2400800;
            watchMeter.Timers.MarkKilled(2305,watchCode);
            Check(!watchMeter.Timers.ForServer(2305).Single().Watch,"Manual kill entry does not automatically watch a boss");
            watchMeter.Timers.SetWatch(2305,watchCode,true);
            watchMeter.Timers.MarkKilled(2305,watchCode);
            Check(watchMeter.Timers.ForServer(2305).Single().Watch,"A manually watched boss stays watched after a kill entry");
            watchMeter.Timers.SetWatch(2305,watchCode,false);
            watchMeter.Timers.OnNotice(new BossNotice(watchCode,1110,null,BossNoticeKind.Killed,DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),ServerId:2305));
            Check(!watchMeter.Timers.ForServer(2305).Single().Watch,"Detected kills preserve an explicitly disabled watch");
        }
        var now=new DateTimeOffset(2026,10,19,12,0,0,TimeSpan.Zero);
        const int code=2400800;var id=TimerCards.WorldBossKey(code);
        meter.Timers.SetCurrentServer(2305);
        Check(cards.WorldBossOptions().Any(b=>b.Code==code),"World boss options include verified field-boss data before a timer is known");
        cards.Saved.SelectedEvents=[];cards.SetFavorite(id,true);
        var unknown=cards.SelectedWorldBossCards().Single();
        Check(unknown.Next is null && !unknown.Alive,"A favorite with no timer has an honest unknown state");
        var hub=new TimerHubWindow(cards){ClockOverride=()=>now,Width=480,Left=-30000,Top=-30000};
        var started=new TimerHubWindow(cards,true){ClockOverride=()=>now,Left=-30000,Top=-30000};
        try
        {
            hub.Show();started.Show();hub.Reload();started.Reload();hub.UpdateLayout();
            Check(Find<Grid>(hub).Count(g=>g.Tag is TimerCardData b && b.Id==unknown.Id)==1,"Favorite-only unknown boss appears exactly once in Upcoming events");
            var tile=Find<Border>(hub).Single(b=>b.Child is Grid{Tag:TimerCardData});
            Check(tile.Effect is not null,"Favorited boss tiles have the established golden glow");
            meter.Timers.OnNotice(new BossNotice(code,1110,null,BossNoticeKind.Killed,now.ToUnixTimeMilliseconds(),ServerId:2305));
            Check(!meter.Timers.ForServer(2305).Single(t=>t.NpcCode==code).Watch,"Detected kills preserve timers without automatically watching a boss");
            meter.Timers.SetRespawn(2305,code,120);hub.Reload();
            Check(cards.SelectedWorldBossCards().Single().Next==now.AddHours(2) && Find<Grid>(hub).Count(g=>g.Tag is TimerCardData)==1,"Chosen world boss stays visible beyond the usual one-hour window");
            cards.Saved.ShowAllWorldBosses=true;meter.Timers.SetRespawn(2305,code,30);hub.Reload();
            Check(Find<Grid>(hub).Count(g=>g.Tag is TimerCardData b && b.Id==unknown.Id)==1 && !Find<Grid>(hub).Any(g=>g.Tag is Core.Events.EventStart e && e.Id==unknown.Id),"Bulk and individual display never duplicate a boss tile");
            cards.Saved.ShowAllWorldBosses=false;cards.Saved.SelectedWorldBosses.Add(code);cards.SetFavorite(id,false);hub.Reload();
            Check(cards.SelectedWorldBossCards().Count==1,"Removing a favorite preserves explicit individual selection");
            cards.Saved.SelectedWorldBosses.Clear();hub.Reload();Check(cards.SelectedWorldBossCards().Count==0,"Unselected and unfavorited bosses leave the fixed display");
            cards.Saved.FavoriteEvents.Add("boss:2305:"+code);Check(cards.IsFavorite(id),"Legacy server-specific favorites remain recognizable");
            cards.SetFavorite(id,false);Check(!cards.IsFavorite("boss:2305:"+code),"Removing a favorite clears equivalent legacy keys");
            cards.SetFavorite("nahma",true);
            Check(cards.NextEvents(now).Any(e=>e.Id=="nahma" && e.StartsAt>now.AddDays(1)),"Favorite scheduled bosses stay included even when their next occurrence is days away");
            cards.Saved.FavoriteEvents.Clear();cards.Saved.SelectedWorldBosses.Add(code);
            cards.Saved.Sounds[id]=new(){Enabled=true,Sound="Bell"};cards.Saved.SoundLeadMinutes=2;
            var dueAt=now.AddMinutes(30);
            Check(cards.TakeDueWorldBossSounds(dueAt.AddMinutes(-3)).Count==0,"Individual boss sounds respect the configured lead time");
            Check(cards.TakeDueWorldBossSounds(dueAt.AddMinutes(-2)).Count==1 && cards.TakeDueWorldBossSounds(dueAt.AddMinutes(-1)).Count==0,"Each individual respawn sound is issued once");
            meter.Timers.SetCurrentServer(2306);Check(cards.TakeDueWorldBossSounds(dueAt).Count==0 && cards.SelectedWorldBossCards().Single().Next is null,"Other servers cannot supply timers or sounds for the current server");
            meter.Timers.SetCurrentServer(2305);
            meter.Timers.OnNotice(new BossNotice(code,1110,null,BossNoticeKind.Killed,now.AddHours(2).ToUnixTimeMilliseconds(),ServerId:2305));
            Check(cards.TakeDueWorldBossSounds(now.AddHours(2).AddMinutes(28)).Count==1,"A later respawn produces its own sound");
            var draft=cards.CreatePreferencesDraft();draft.Saved.SelectedWorldBosses.Add(2400900);draft.SetFavorite(id,true);draft.Saved.Sounds[id].Sound="Ping";
            Check(!cards.IsFavorite(id) && cards.Saved.Sounds[id].Sound=="Bell" && !cards.Saved.SelectedWorldBosses.Contains(2400900),"Boss display, favorites and sound edits remain isolated drafts");
            cards.SetFavorite("rift",true);cards.ApplyPreferences(draft);
            Check(cards.IsFavorite(id) && cards.IsFavorite("rift") && cards.Saved.Sounds[id].Sound=="Ping" && cards.Saved.SelectedWorldBosses.Contains(2400900),"Save applies boss preferences while preserving newer untouched live favorites");
            var preferences=new WorldBossSettingsPanel(draft,()=>{},(_,_)=>{});var container=new Window{Content=preferences,Width=600,Height=450,Left=-30000,Top=-30000};
            try
            {
                container.Show();container.UpdateLayout();
                var show=Find<CheckBox>(preferences).Single(b=>Equals(b.Tag,"world-boss-show:"+code));show.IsChecked=false;show.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(!draft.Saved.SelectedWorldBosses.Contains(code) && cards.Saved.SelectedWorldBosses.Contains(code),"Per-boss display checkbox modifies only the draft");
                var audio=Find<Expander>(preferences).Single(b=>Equals(b.Tag,"world-boss-sound:"+code));audio.IsExpanded=true;container.UpdateLayout();
                Check(Find<ComboBox>(audio).Any(c=>c.Items.Count==4),"Every boss offers built-in sounds and the validated custom-audio picker");
            }
            finally{container.Close();}
            meter.Timers.OnNotice(new BossNotice(code,1110,null,BossNoticeKind.Alive,now.ToUnixTimeMilliseconds(),ServerId:2305));
            // The observation must be newer than the latest synthetic kill.
            meter.Timers.OnNotice(new BossNotice(code,1110,null,BossNoticeKind.Alive,now.AddHours(3).ToUnixTimeMilliseconds(),ServerId:2305));
            hub.Reload();started.Reload();
            Check(!Find<Grid>(hub).Any(g=>g.Tag is TimerCardData b && b.Id==unknown.Id) && Find<Grid>(started).Any(g=>g.Tag is TimerCardData b && b.Id==unknown.Id),"Confirmed alive bosses belong exclusively to Started events");
            foreach(var eventWindow in new[]{hub,started})
            {
                eventWindow.SetDisplayOnly(false);eventWindow.UpdateLayout();
                var content=Find<System.Windows.Controls.WrapPanel>(eventWindow).Single();
                var before=content.PointToScreen(new Point());var height=eventWindow.ActualHeight;
                for(var toggle=0;toggle<4;toggle++)
                {
                    eventWindow.SetDisplayOnly(true);eventWindow.UpdateLayout();
                    Check((content.PointToScreen(new Point())-before).Length<.5 && Math.Abs(eventWindow.ActualHeight-height)<.5,"Event content and window geometry stay fixed while locked");
                    eventWindow.Reload();eventWindow.UpdateLayout();
                    Check((content.PointToScreen(new Point())-before).Length<.5,"Event refresh preserves locked content position");
                    eventWindow.SetDisplayOnly(false);eventWindow.UpdateLayout();
                    Check((content.PointToScreen(new Point())-before).Length<.5,"Unlock restores controls without shifting event content");
                }
            }
            foreach(var module in new Window[]{new PetTrackerWindow(meter),new HiddenCubeWindow(meter.Settings),new Window{Title="Placement test",Width=300,Height=150,Content=new TextBlock{Text="Content anchor"}}})
            {
                module.Left=-30000;module.Top=-30000;WindowClickThrough.Attach(module,()=>false);
                try
                {
                    module.Show();module.UpdateLayout();
                    var labels=Find<TextBlock>(module).Where(t=>t.IsVisible).ToArray();
                    var positions=labels.ToDictionary(t=>t,t=>t.PointToScreen(new Point()));
                    var size=new Size(module.ActualWidth,module.ActualHeight);
                    WindowClickThrough.ApplyAll(true);module.UpdateLayout();
                    Check(labels.Where(t=>t.IsVisible).All(t=>(t.PointToScreen(new Point())-positions[t]).Length<.5) && Math.Abs(module.ActualHeight-size.Height)<.5 && Math.Abs(module.ActualWidth-size.Width)<.5,"Module content and geometry remain fixed when switching to click-through");
                    WindowClickThrough.ApplyAll(false);module.UpdateLayout();
                    Check(labels.All(t=>(t.PointToScreen(new Point())-positions[t]).Length<.5),"Module controls return without shifting content");
                }
                finally{module.Close();}
            }
            var path=output+".settings.json";
            try{JsonSettingsFile.Write(path,cards.Saved);var restored=JsonSettingsFile.Read<TimerCards.State>(path)!;Check(restored.SelectedWorldBosses.SetEquals(cards.Saved.SelectedWorldBosses) && restored.FavoriteEvents.SetEquals(cards.Saved.FavoriteEvents) && restored.Sounds[id].Sound=="Ping","Boss selections, favorites and per-boss audio survive reload");}
            finally{foreach(var file in new[]{path,path+".bak"})if(File.Exists(file))File.Delete(file);}
            File.WriteAllText(output,JsonSerializer.Serialize(passed));
        }
        finally{hub.Close();started.Close();}
    }
}
