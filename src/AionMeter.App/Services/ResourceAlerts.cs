using AionMeter.Core.Game;
namespace AionMeter.App.Services;
public sealed class ResourceAlertRule
{
    public bool Enabled {get;set;}
    public ulong Threshold {get;set;}
}
internal sealed class ResourceAlertMonitor:IDisposable
{
    private readonly EventSoundPlayer _sound=new();
    private readonly HashSet<string> _latched=[];
    private string? _character;
    internal static bool IsLow(AppSettings settings,string key,ulong count,bool cached)=>!cached && settings.ResourceAlerts is {} rules && rules.TryGetValue(key,out var rule) && rule.Enabled && count<=rule.Threshold;
    internal IReadOnlyList<string> Evaluate(AppSettings settings,ResourceBalances resources)
    {
        settings.ResourceAlerts??=[];
        var progress=resources.SavedProgress();
        if(progress is null || resources.AwaitingIdentity)return [];
        var own=progress.ServerId+":"+progress.Name;
        if(_character!=own){_character=own;_latched.Clear();}
        var markers=resources.CacheMarkers();var hits=new List<string>();
        var values=new List<(string Key,ulong Value,bool Cached)>();
        if(progress.Energy is {} energy)values.Add(("odyle",energy.Available,markers.Energy));
        foreach(var entry in Windows.ResourcesWindow.Entries.Where(e=>e.Item!=0))if(progress.Balances.TryGetValue(entry.Item,out var n))values.Add((entry.Key,n,markers.Currencies.Contains(entry.Item)));
        foreach(var entry in Windows.DungeonEntriesWindow.Entries)if(progress.Entries.TryGetValue(entry.Id,out var n))values.Add((entry.Key,(ulong)n.Available+n.Supplementary,markers.Entries.Contains(entry.Id)));
        foreach(var value in values)
        {
            if(!settings.ResourceAlerts.TryGetValue(value.Key,out var rule)||!rule.Enabled){_latched.Remove(value.Key);continue;}
            if(value.Cached)continue;
            if(value.Value>rule.Threshold){_latched.Remove(value.Key);continue;}
            if(_latched.Add(value.Key))hits.Add(value.Key);
        }
        return hits;
    }
    internal void Tick(MeterService meter)
    {
        if(meter.DemoRunning)return;
        if(Evaluate(meter.Settings,meter.Resources).Count>0 && meter.Settings.ResourceAlertSoundEnabled)
            _sound.Play(new(){Enabled=true,Sound=meter.Settings.ResourceAlertSound},Math.Clamp(meter.Settings.ResourceAlertVolume,0,1));
    }
    public void Dispose()=>_sound.Dispose();
}
