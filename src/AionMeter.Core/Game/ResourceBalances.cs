using AionMeter.Core.Events;
using AionMeter.Core.Protocol;
namespace AionMeter.Core.Game;

public sealed record ResourceBalanceSnapshot(long TimeMs,IReadOnlyDictionary<int,ulong> Values):GameEvent(TimeMs);
public static class ResourceInventoryPackets
{
    public static readonly int[] Supported=[930100003,930100011,930100024,930100017];
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
        return new(time,found);
    }
}
public sealed class ResourceBalances
{
    private readonly object _gate=new();
    private readonly Dictionary<int,ulong> _balances=[];
    private (string,int)? _identity;
    private OdyleEnergyEvent? _energy;
    public void Reset(){lock(_gate){_balances.Clear();_energy=null;_identity=null;}}
    public void Observe(GameEvent e)
    {
        lock(_gate)
        {
            if(e is SelfIdentifiedEvent own)
            {
                var id=(own.Name,own.ServerId);
                if(_identity is not null && _identity!=id){_balances.Clear();_energy=null;}
                _identity=id;
            }
            if(e is OdyleEnergyEvent energy)_energy=energy;
            if(e is ResourceBalanceSnapshot snapshot){_balances.Clear();foreach(var item in snapshot.Values)_balances[item.Key]=item.Value;}
        }
    }
    public (OdyleEnergyEvent? Energy,IReadOnlyDictionary<int,ulong> Balances) Read(){lock(_gate)return (_energy,new Dictionary<int,ulong>(_balances));}
}
