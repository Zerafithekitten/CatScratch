using System.IO;
using System.Text.Json;
using AionMeter.Core.Capture;
using AionMeter.Core.Events;
using AionMeter.Core.Game;

namespace AionMeter.App.Services;

/// <summary>Character-scoped last-known progress. Fresh server snapshots always win; saved counts remain marked stale.</summary>
internal sealed class PetProgressCache:IDisposable
{
    private readonly PetCollection _pets;
    private readonly GameData _data;
    private readonly string _path;
    private readonly string _directory;
    private readonly object _gate=new();
    private Dictionary<string,PetProgress> _records=[];
    private readonly HashSet<string> _recovering=[];
    private readonly CancellationTokenSource _stop=new();
    private readonly System.Threading.Timer _timer;
    private long _stamp;
    private bool _disposed;
    private static string Key(string name,int server)=>server+":"+name;
    public PetProgressCache(PetCollection pets,GameData data,string? directory=null)
    {
        _pets=pets;_data=data;_directory=directory??AppSettings.AppDataDir;_path=Path.Combine(_directory,"pet-progress.json");
        try
        {
            if(File.Exists(_path) && new FileInfo(_path).Length<=4000000)
                _records=JsonSerializer.Deserialize<Dictionary<string,PetProgress>>(File.ReadAllText(_path))??[];
        }
        catch(Exception e) when(e is IOException or JsonException or UnauthorizedAccessException){Log.Error("Pet progress cache could not be read",e);}
        _records=_records.Where(p=>p.Value is not null && p.Value.Pets is not null && p.Value.Pets.Length<=4096).OrderByDescending(p=>p.Value.TimeMs).Take(32).ToDictionary(p=>p.Key,p=>p.Value);
        _timer=new(_=>Save(),null,Timeout.Infinite,Timeout.Infinite);
        pets.Changed+=Changed;
    }
    private void Changed()
    {
        lock(_gate)
        {
            if(_disposed || _pets.ProgressVersion==_stamp)return;
            _stamp=_pets.ProgressVersion;
            if(_pets.SavedProgress() is not { } progress)return;
            _records[Key(progress.Name,progress.ServerId)]=progress;
            if(_records.Count>32)_records.Remove(_records.MinBy(x=>x.Value.TimeMs).Key);
            _timer.Change(1000,Timeout.Infinite);
        }
    }
    public void Identity(SelfIdentifiedEvent own)
    {
        if(string.IsNullOrWhiteSpace(own.Name) || own.ServerId<=0 || _pets.Confirmed)return;
        PetProgress? cached;
        var key=Key(own.Name,own.ServerId);
        lock(_gate)
        {
            if(_disposed)return;
            _records.TryGetValue(key,out cached);
        }
        if(cached is not null && _pets.RestoreProgress(cached))return;
        lock(_gate){if(!_recovering.Add(key))return;}
        _=Task.Run(()=>Recover(own));
    }
    private void Recover(SelfIdentifiedEvent own)
    {
        // Bootstrap upgrades from the user's own completed recordings only; never touch game files or send requests.
        try
        {
            var directory=Path.Combine(_directory,"captures");
            if(!Directory.Exists(directory))return;
            foreach(var file in new DirectoryInfo(directory).EnumerateFiles("*.pcap").Where(f=>f.Length>24 && f.Length<=32000000 && f.LastWriteTimeUtc>DateTime.UtcNow.AddDays(-1)).OrderByDescending(f=>f.LastWriteTimeUtc).Take(3))
            {
                _stop.Token.ThrowIfCancellationRequested();
                var recovered=new PetCollection();var identity=new LiveIdentity();
                using var replay=new PcapReplaySource(_data,file.FullName,false);
                replay.EventDecoded+=e=>{var self=identity.Observe(e);recovered.Observe(e is SelfIdentifiedEvent && self is not null?self:e);if(e is PlayerSeenEvent && self is not null)recovered.Observe(self);};
                replay.RunToEnd(_stop.Token);
                if(recovered.SavedProgress() is not { } progress || progress.Name!=own.Name || progress.ServerId!=own.ServerId)continue;
                if(_stop.IsCancellationRequested)return;
                _pets.RestoreProgress(progress);
                Log.Info("Recovered last-known pet progress from a completed own recording; fresh login data takes priority");
                return;
            }
        }
        catch(OperationCanceledException){ }
        catch(Exception e){if(!_stop.IsCancellationRequested)Log.Error("Pet progress recovery could not read the recording",e);}
    }
    private void Save()
    {
        lock(_gate)
        {
            if(_records.Count==0)return;
            try
            {
                Directory.CreateDirectory(_directory);
                File.WriteAllText(_path+".tmp",JsonSerializer.Serialize(_records));File.Move(_path+".tmp",_path,true);
            }
            catch(Exception e) when(e is IOException or UnauthorizedAccessException){Log.Error("Pet progress cache could not be saved",e);}
        }
    }
    internal static void CheckRecording(string recording,string output)
    {
        var data=GameData.Load(MeterService.DataDirectory,"en");var original=new PetCollection();var identity=new LiveIdentity();
        using(var replay=new PcapReplaySource(data,recording,false))
        {
            replay.EventDecoded+=e=>{var own=identity.Observe(e);original.Observe(e is SelfIdentifiedEvent && own is not null?own:e);if(e is PlayerSeenEvent && own is not null)original.Observe(own);};replay.RunToEnd();
        }
        var saved=original.SavedProgress()??throw new InvalidOperationException("No confirmed pet baseline in recording");
        var root=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!,"pet-recovery-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(Path.Combine(root,"captures"));
        File.Copy(recording,Path.Combine(root,"captures","own.pcap"));
        var recovered=new PetCollection();var owner=new SelfIdentifiedEvent(saved.TimeMs,1,saved.Name,saved.ServerId,GameClass.Unknown);recovered.Observe(owner);
        using(var cache=new PetProgressCache(recovered,data,root))
        {
            cache.Identity(owner);
            if(!SpinWait.SpinUntil(()=>recovered.Confirmed,TimeSpan.FromSeconds(5)))throw new InvalidOperationException("Recording recovery timed out");
            if(!recovered.FromCache || !saved.Pets.OrderBy(p=>p.Id).SequenceEqual(recovered.Snapshot().OrderBy(p=>p.Id)))throw new InvalidOperationException("Recovered counters differ");
        }
        var roundtrip=new PetCollection();roundtrip.Observe(owner);
        using(var cache=new PetProgressCache(roundtrip,data,root)){cache.Identity(owner);if(!roundtrip.Confirmed || !roundtrip.FromCache)throw new InvalidOperationException("Persisted recovery not restored");}
        File.WriteAllText(output,JsonSerializer.Serialize(new{Recovered=true,Count=roundtrip.Snapshot().Count,Cached=true,Persisted=true}));
    }
    public void Dispose()
    {
        lock(_gate){if(_disposed)return;_disposed=true;}
        _pets.Changed-=Changed;_stop.Cancel();_timer.Dispose();
        lock(_gate)if(_pets.SavedProgress() is { } final)_records[Key(final.Name,final.ServerId)]=final;
        Save();
    }
}
