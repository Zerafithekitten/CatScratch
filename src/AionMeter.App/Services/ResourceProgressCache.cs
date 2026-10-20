using System.IO;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
namespace AionMeter.App.Services;

/// <summary>Per-character last-known resources. Only fresh own identity authorizes a disk restore.</summary>
internal sealed class ResourceProgressCache:IDisposable
{
    private readonly ResourceBalances _resources;
    private readonly string _path;
    private readonly object _gate=new();
    private Dictionary<string,ResourceProgress> _records=[];
    private readonly System.Threading.Timer _timer;
    private bool _disposed,_awaitingIdentity=true;
    private static string Key(string name,int server)=>server+":"+name;
    internal ResourceProgressCache(ResourceBalances resources,string? directory=null)
    {
        _resources=resources;_path=Path.Combine(directory??AppSettings.AppDataDir,"resource-progress.json");
        try
        {
            if(!File.Exists(_path) || new FileInfo(_path).Length<=1_000_000)
                _records=JsonSettingsFile.Read<Dictionary<string,ResourceProgress>>(_path)??[];
        }
        catch(Exception e) when(e is IOException or UnauthorizedAccessException){Log.Error("Resource cache could not be read",e);}
        _records=_records.Where(p=>p.Value is {ServerId:>0,Balances:not null,Entries:not null} v && !string.IsNullOrWhiteSpace(v.Name) && v.Balances.Count<=5 && v.Entries.Count<=3 && p.Key==Key(v.Name,v.ServerId)).OrderByDescending(p=>p.Value.TimeMs).Take(32).ToDictionary(p=>p.Key,p=>p.Value);
        _timer=new(_=>Save(),null,Timeout.Infinite,Timeout.Infinite);
    }
    internal void Suspend(){lock(_gate)_awaitingIdentity=true;}
    internal void Observe(GameEvent value)
    {
        lock(_gate)
        {
            if(_disposed)return;
            if(value is ResourceSnapshotStartedEvent){_awaitingIdentity=true;return;}
            if(value is ZoneChangedEvent){if(_resources.AwaitingIdentity)_awaitingIdentity=true;return;}
            if(value is SelfIdentifiedEvent own)
            {
                if(string.IsNullOrWhiteSpace(own.Name)||own.ServerId<=0)return;
                _awaitingIdentity=false;
                if(_records.TryGetValue(Key(own.Name,own.ServerId),out var cached))_resources.RestoreProgress(cached);
            }
            else if(_awaitingIdentity || value is not (OdyleEnergyEvent or ResourceBalanceSnapshot or DungeonEntriesEvent))return;
            Remember();
        }
    }
    internal IReadOnlyList<ResourceProgress> Snapshot(){lock(_gate)return _records.Values.OrderByDescending(p=>p.TimeMs).ToArray();}
    private void Remember()
    {
        if(_resources.SavedProgress() is not {} progress)return;
        _records[Key(progress.Name,progress.ServerId)]=progress;
        while(_records.Count>32)_records.Remove(_records.MinBy(p=>p.Value.TimeMs).Key);
        _timer.Change(1000,Timeout.Infinite);
    }
    private void Save()
    {
        lock(_gate)
        {
            if(_records.Count==0)return;
            try{JsonSettingsFile.Write(_path,_records);}
            catch(Exception e) when(e is IOException or UnauthorizedAccessException){Log.Error("Resource cache could not be saved",e);}
        }
    }
    public void Dispose()
    {
        lock(_gate){if(_disposed)return;_disposed=true;_timer.Dispose();}
        Save();
    }
}
