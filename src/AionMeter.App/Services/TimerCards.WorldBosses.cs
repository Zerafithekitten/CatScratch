using AionMeter.Core.Events;
namespace AionMeter.App.Services;

public sealed partial class TimerCards
{
    internal event Action BossDataChanged { add=>_meter.Timers.Changed+=value; remove=>_meter.Timers.Changed-=value; }
    internal static string WorldBossKey(int code)=>"world-boss:"+code;
    private static bool TryBossCode(string id,out int code)
    {
        code=0;var parts=id.Split(':');
        return ((parts.Length==3 && parts[0]=="boss") || (parts.Length==2 && parts[0]=="world-boss")) && int.TryParse(parts[^1],out code) && code>0;
    }
    private static string FavoriteKey(string id)=>TryBossCode(id,out var code)?WorldBossKey(code):id.StartsWith("auto:")?id[5..]:id;
    internal IReadOnlyList<(string Id,string Name,int Code)> WorldBossOptions()=>_meter.Timers.KnownWorldBossCodes()
        .Concat(Saved.SelectedWorldBosses).Concat(Saved.FavoriteEvents.Select(id=>TryBossCode(id,out var code)?code:0).Where(c=>c>0))
        .Distinct().Select(code=>(Id:WorldBossKey(code),Name:_meter.Data.NpcName(code),Code:code)).OrderBy(b=>b.Name,StringComparer.CurrentCultureIgnoreCase).ToArray();
    private IEnumerable<int> SelectedBossCodes()=>Saved.SelectedWorldBosses.Concat(Saved.FavoriteEvents.Select(id=>TryBossCode(id,out var code)?code:0).Where(c=>c>0)).Distinct().Order();
    private bool IncludeWorldBoss(int code)=>Saved.ShowAllWorldBosses || SelectedBossCodes().Contains(code);
    internal IEnumerable<string> EffectiveScheduledIds()=>Saved.SelectedEvents
        .Concat(Saved.ShowAllWorldBosses?AutomaticEventSchedule.Catalog.Where(e=>EventKinds.For(e.Id)=="BOSS").Select(e=>e.Id):[])
        .Concat(AutomaticEventSchedule.Catalog.Where(e=>EventKinds.For(e.Id)=="BOSS" && IsFavorite(e.Id)).Select(e=>e.Id)).Distinct().Order();
    private EventStart? NextScheduledOccurrence(string id,DateTimeOffset now)
    {
        if(!AutomaticEventSchedule.Catalog.Any(e=>e.Id==id))return null;
        for(var day=0;day<=7;day++)
        {
            var next=AutomaticEventSchedule.Next(now.AddDays(day),Saved.EventRegion,[id],EventActivity.Duration(id),TimeSpan.FromHours(24)).FirstOrDefault();
            if(next is not null)return next;
        }
        return null;
    }
    private IEnumerable<EventStart> PersistentScheduledBosses(DateTimeOffset now)
    {
        foreach(var item in AutomaticEventSchedule.Catalog.Where(e=>EventKinds.For(e.Id)=="BOSS" && (Saved.SelectedEvents.Contains(e.Id)||IsFavorite(e.Id))))
        {
            var next=NextScheduledOccurrence(item.Id,now);
            if(next is not null)yield return next;
        }
    }
    internal IReadOnlyList<TimerCardData> SelectedWorldBossCards()=>SelectedBossCodes().Select(code=>BossCard(_meter.Timers.CurrentServer,code)).ToArray();
    private readonly Dictionary<string,DateTimeOffset> _bossSoundAlerts=[];
    internal IReadOnlyList<(EventSoundPreference Sound,TimerCardData Boss)> TakeDueWorldBossSounds(DateTimeOffset now)
    {
        var due=new List<(EventSoundPreference,TimerCardData)>();
        foreach(var boss in _meter.Timers.ForServer(_meter.Timers.CurrentServer).Where(b=>b.NpcCode>0 && !b.AliveNow))
        {
            var id=WorldBossKey(boss.NpcCode);
            if(!Saved.Sounds.TryGetValue(id,out var sound) || !sound.Enabled || boss.NextSpawn is not {} at || at<now.AddSeconds(-2)
                || at>now.AddMinutes(Math.Clamp(Saved.SoundLeadMinutes,0,10)) || _bossSoundAlerts.GetValueOrDefault($"{boss.ServerId}:{id}")==at)continue;
            if(_bossSoundAlerts.Count>512)_bossSoundAlerts.Clear();
            _bossSoundAlerts[$"{boss.ServerId}:{id}"]=at;due.Add((sound,BossCard(boss.ServerId,boss.NpcCode)));
        }
        return due;
    }
    internal bool HasIndividualBossSound(int code)=>Saved.Sounds.TryGetValue(WorldBossKey(code),out var sound) && sound.Enabled;
}
