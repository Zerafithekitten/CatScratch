using AionMeter.Core.Events;
using AionMeter.Core.Protocol;
namespace AionMeter.Core.Game;

public sealed record ResourceBalanceSnapshot(long TimeMs,IReadOnlyDictionary<int,ulong> Values,IReadOnlySet<int>? Ambiguous=null):GameEvent(TimeMs);
public static class ResourceInventoryPackets
{
    public static readonly int[] Supported=[930100003,930100011,930100024,930100017,930100019];
    public static ResourceBalanceSnapshot? Update(long time,ReadOnlySpan<byte> body)
    {
        // Confirmed absolute balance-change records for all supported currencies.
        // Deletion-to-zero layouts still require separate verification.
        if(body.Length<40 || body[0]==0)return null;
        return Snapshot(time,body);
    }
    public static ResourceBalanceSnapshot? Snapshot(long time,ReadOnlySpan<byte> body)
    {
        // Only the confirmed inventory snapshot and complete currency-item record shape.
        // Ambiguous repeated templates (e.g. another storage container) remain unknown.
        if(body.Length<40 || body.Length>1_000_000)return null;
        var found=new Dictionary<int,ulong>();var ambiguous=new HashSet<int>();
        for(var at=4;at+18<=body.Length;at++)
        {
            var id=(int)Wire.U32(body,at);if(!Supported.Contains(id))continue;
            var amount=System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(body[(at+4)..]);
            if(amount>1_000_000_000_000 || body[at+12]>1 || Wire.U32(body,at+13)>5000)continue;
            if(!found.TryAdd(id,amount))ambiguous.Add(id);
        }
        foreach(var id in ambiguous)found.Remove(id);
        return found.Count>0 || ambiguous.Count>0?new(time,found,ambiguous):null;
    }
}
public sealed record ResourceProgress(string Name,int ServerId,long TimeMs,OdyleEnergyEvent? Energy,Dictionary<int,ulong> Balances,Dictionary<uint,DungeonEntryValue> Entries);
public sealed record ResourceCacheMarkers(bool Energy,IReadOnlySet<int> Currencies,IReadOnlySet<uint> Entries);
public sealed class ResourceBalances
{
    private readonly object _gate=new();
    private readonly Dictionary<int,ulong> _balances=[];
    private (string,int)? _identity;
    private OdyleEnergyEvent? _energy;
    private bool _cachedEnergy;
    private readonly HashSet<int> _cachedCurrencies=[],_ambiguous=[];
    private readonly HashSet<uint> _cachedEntries=[];
    private long _lastDataTimeMs;
    private int _mapId=-1;
    private bool _awaitingIdentity;
    private readonly List<GameEvent> _pending=[];
    public bool AwaitingIdentity {get{lock(_gate)return _awaitingIdentity;}}
    public ResourceCacheMarkers CacheMarkers(){lock(_gate)return new(_cachedEnergy,new HashSet<int>(_cachedCurrencies),new HashSet<uint>(_cachedEntries));}
    public ResourceProgress? SavedProgress()
    {
        lock(_gate)return _identity is {} own && own.Item2>0 && (_energy is not null || _balances.Count>0 || _entries.Count>0)
            ?new(own.Item1,own.Item2,_lastDataTimeMs,_energy,new(_balances),new(_entries)):null;
    }
    public void MarkCached(){lock(_gate){_cachedEnergy=_energy is not null;_cachedCurrencies.UnionWith(_balances.Keys);_cachedEntries.UnionWith(_entries.Keys);}}
    public bool RestoreProgress(ResourceProgress progress)
    {
        lock(_gate)
        {
            if(_identity is not {} own || own.Item1!=progress.Name || own.Item2<=0 || own.Item2!=progress.ServerId || progress.Balances is null || progress.Entries is null)return false;
            var restored=false;
            if(_energy is null && progress.Energy is {} energy){_energy=energy;_cachedEnergy=true;restored=true;}
            foreach(var p in progress.Balances.Where(p=>ResourceInventoryPackets.Supported.Contains(p.Key) && p.Value<=1_000_000_000_000))
                if(!_ambiguous.Contains(p.Key) && _balances.TryAdd(p.Key,p.Value)){_cachedCurrencies.Add(p.Key);restored=true;}
            foreach(var p in progress.Entries.Where(p=>p.Key is 1 or 4 or 101 && p.Value is not null))
                if(_entries.TryAdd(p.Key,p.Value)){_cachedEntries.Add(p.Key);restored=true;}
            if(restored)_lastDataTimeMs=Math.Max(_lastDataTimeMs,progress.TimeMs);
            return restored;
        }
    }
    private void ClearValues(){_balances.Clear();_energy=null;_entries.Clear();_cachedEnergy=false;_cachedCurrencies.Clear();_cachedEntries.Clear();_ambiguous.Clear();_lastDataTimeMs=0;}
    private readonly Dictionary<uint,DungeonEntryValue> _entries=[];
    public IReadOnlyDictionary<uint,DungeonEntryValue> ReadEntries(){lock(_gate)return new Dictionary<uint,DungeonEntryValue>(_entries);}
    public void Reset(){lock(_gate){ClearValues();_identity=null;_pending.Clear();_awaitingIdentity=false;_mapId=-1;}}
    public void Observe(GameEvent e)
    {
        lock(_gate)
        {
            if(e is ZoneChangedEvent zone)
            {
                if(!zone.IsTeleport && zone.MapId!=_mapId && _identity is not null)
                {MarkCached();_awaitingIdentity=true;_pending.Clear();}
                _mapId=zone.MapId;return;
            }
            if(_awaitingIdentity && e is OdyleEnergyEvent or ResourceBalanceSnapshot or DungeonEntriesEvent)
            {
                if(_pending.Count==128)_pending.RemoveAt(0);
                _pending.Add(e);return;
            }
            if(e is SelfIdentifiedEvent own && !string.IsNullOrWhiteSpace(own.Name))
            {
                var changed=_identity is {} prior && (prior.Item1!=own.Name ||
                    prior.Item2>0 && own.ServerId>0 && prior.Item2!=own.ServerId);
                if(changed)ClearValues();
                var server=own.ServerId>0?own.ServerId:_identity is {} previous && previous.Item1==own.Name?previous.Item2:0;
                _identity=(own.Name,server);
                if(_awaitingIdentity)
                {
                    _awaitingIdentity=false;var pending=_pending.ToArray();_pending.Clear();
                    foreach(var update in pending)Observe(update);
                }
            }
            if(e is OdyleEnergyEvent energy){_energy=energy;_cachedEnergy=false;_lastDataTimeMs=energy.TimeMs;}
            if(e is DungeonEntriesEvent entries)foreach(var item in entries.Values)
            {
                _entries[item.Key]=item.Value with {Capacity=item.Value.Capacity??(_entries.TryGetValue(item.Key,out var priorEntry)?priorEntry.Capacity:null)};
                _cachedEntries.Remove(item.Key);_lastDataTimeMs=entries.TimeMs;
            }
            if(e is ResourceBalanceSnapshot snapshot)
            {
                // Inventory packets can cover different containers/sections. Missing entries do not mean zero.
                if(snapshot.Ambiguous is {} ambiguous)foreach(var id in ambiguous){_balances.Remove(id);_cachedCurrencies.Remove(id);_ambiguous.Add(id);}
                foreach(var item in snapshot.Values){_balances[item.Key]=item.Value;_cachedCurrencies.Remove(item.Key);_ambiguous.Remove(item.Key);_lastDataTimeMs=snapshot.TimeMs;}
            }
        }
    }
    public (OdyleEnergyEvent? Energy,IReadOnlyDictionary<int,ulong> Balances) Read(){lock(_gate)return (_energy,new Dictionary<int,ulong>(_balances));}
}
