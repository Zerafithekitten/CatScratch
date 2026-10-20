using AionMeter.Core.Events;
using AionMeter.Core.Protocol;
namespace AionMeter.Core.Game;

public sealed record CubeKeyInventoryEvent(long TimeMs,bool IsSnapshot,IReadOnlyDictionary<ulong,uint> Stacks):GameEvent(TimeMs);
/// <summary>Bound Hidden Cube key records verified against login inventory and the item tooltip.</summary>
public static class HiddenCubeKeyPackets
{
    // Client item data: https://aion2data.com/items/630900003-key-hidden-cube-bound
    public const int BoundKeyId=630900003;
    private static bool Record(ReadOnlySpan<byte> body,int at,out ulong instance,out uint amount)
    {
        instance=0;amount=0;
        if(at<9||at+18>body.Length||body[at-9]!=0||Wire.U32(body,at)!=BoundKeyId)return false;
        instance=System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(body[(at-8)..]);
        var count=System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(body[(at+4)..]);
        if(instance==0||count>100||body[at+12]>1||Wire.U32(body,at+13)>=5000)return false;
        amount=(uint)count;return true;
    }
    public static CubeKeyInventoryEvent? Snapshot(long time,ReadOnlySpan<byte> body)
    {
        // The verified backpack section. Other container layouts remain unknown.
        if(body.Length<40||body.Length>1_000_000||body[0]!=0x10||body[1]!=2)return null;
        var cursor=2;if(!Wire.TryVarint(body,ref cursor,out var count)||count is 0 or >2048)return null;
        var stacks=new Dictionary<ulong,uint>();
        for(var at=cursor+9;at+18<=body.Length;at++)
        {
            if(!Record(body,at,out var instance,out var amount))continue;
            if(stacks.TryGetValue(instance,out var previous)&&previous!=amount)return null;
            stacks[instance]=amount;
        }
        // Absence is not a confirmed zero. In particular, do not mix other storage sections.
        return stacks.Count>0&&stacks.Count<=count?new(time,true,stacks):null;
    }
    public static CubeKeyInventoryEvent? Update(long time,ReadOnlySpan<byte> body)
    {
        if(body.Length!=41||body[0]!=1||body[1]>2||!Record(body,11,out var instance,out var amount))return null;
        if(body[1]==2&&amount!=0)return null;
        return new(time,false,new Dictionary<ulong,uint>{{instance,amount}});
    }
}
