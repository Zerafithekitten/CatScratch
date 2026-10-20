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
public sealed class ResourceBalances
{
    private readonly object _gate=new();
    private readonly Dictionary<int,ulong> _balances=[];
    private (string,int)? _identity;
    private OdyleEnergyEvent? _energy;
    private readonly Dictionary<uint,DungeonEntryValue> _entries=[];
    public IReadOnlyDictionary<uint,DungeonEntryValue> ReadEntries(){lock(_gate)return new Dictionary<uint,DungeonEntryValue>(_entries);}
    public void Reset(){lock(_gate){_balances.Clear();_energy=null;_entries.Clear();_identity=null;}}
    public void Observe(GameEvent e)
    {
        lock(_gate)
        {
            if(e is SelfIdentifiedEvent own && !string.IsNullOrWhiteSpace(own.Name))
            {
                var changed=_identity is {} prior && (prior.Item1!=own.Name ||
                    prior.Item2>0 && own.ServerId>0 && prior.Item2!=own.ServerId);
                if(changed){_balances.Clear();_energy=null;_entries.Clear();}
                var server=own.ServerId>0?own.ServerId:_identity is {} previous && previous.Item1==own.Name?previous.Item2:0;
                _identity=(own.Name,server);
            }
            if(e is OdyleEnergyEvent energy)_energy=energy;
            if(e is DungeonEntriesEvent entries)foreach(var item in entries.Values)
                _entries[item.Key]=item.Value with {Capacity=item.Value.Capacity??(_entries.TryGetValue(item.Key,out var priorEntry)?priorEntry.Capacity:null)};
            if(e is ResourceBalanceSnapshot snapshot)
            {
                // Inventory packets can cover different containers/sections. Missing entries do not mean zero.
                if(snapshot.Ambiguous is {} ambiguous)foreach(var id in ambiguous)_balances.Remove(id);
                foreach(var item in snapshot.Values)_balances[item.Key]=item.Value;
            }
        }
    }
    public (OdyleEnergyEvent? Energy,IReadOnlyDictionary<int,ulong> Balances) Read(){lock(_gate)return (_energy,new Dictionary<int,ulong>(_balances));}
}
