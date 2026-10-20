using AionMeter.Core.Events;
using AionMeter.Core.Protocol;
namespace AionMeter.Core.Game;

public sealed record DungeonEntryValue(uint Available,uint Supplementary,uint? Capacity);
public sealed record DungeonEntriesEvent(long TimeMs,IReadOnlyDictionary<uint,DungeonEntryValue> Values):GameEvent(TimeMs);
public static class DungeonEntryPackets
{
    private static readonly uint[] Supported=[1,4,101];
    private static bool Entry(ReadOnlySpan<byte> body,ref int at,out uint id,out DungeonEntryValue value)
    {
        id=0;value=new(0,0,null);
        if(at+5>body.Length)return false;
        var flags=body[at++];id=Wire.U32(body,at);at+=4;
        if((flags&~15)!=0)return false;
        var wide=((flags&1)!=0?8:0)+((flags&2)!=0?8:0);
        if(at+wide>body.Length)return false;at+=wide;
        uint regular=0,extra=0;
        if((flags&4)!=0 && !Wire.TryVarint(body,ref at,out regular))return false;
        if((flags&8)!=0 && !Wire.TryVarint(body,ref at,out extra))return false;
        if((Supported.Contains(id)||Supported.Contains(id>=60000000?id-60000000:0)) && (flags&3)!=0)return false;
        value=new(regular,extra,null);return true;
    }
    public static DungeonEntriesEvent? Snapshot(long time,ReadOnlySpan<byte> body)
    {
        var at=0;if(!Wire.TryVarint(body,ref at,out var count)||count>1024)return null;
        var properties=new Dictionary<uint,DungeonEntryValue>();
        for(var n=0;n<count;n++)
        {
            if(!Entry(body,ref at,out var id,out var value)||!properties.TryAdd(id,value))return null;
        }
        if(at!=body.Length)return null;
        var values=new Dictionary<uint,DungeonEntryValue>();
        foreach(var id in Supported)if(properties.TryGetValue(id,out var v))
            values[id]=v with {Capacity=properties.TryGetValue(id==1?60000006:60000000+id,out var cap)?cap.Available:null};
        return values.Count>0?new(time,values):null;
    }
    public static DungeonEntriesEvent? Update(long time,ReadOnlySpan<byte> body)
    {
        if(body.Length<8||body[0]!=0)return null;
        var at=1;
        if(!Entry(body,ref at,out var id,out var value)||!Supported.Contains(id)||at!=body.Length-1||body[at]!=3)return null;
        return new(time,new Dictionary<uint,DungeonEntryValue>{{id,value}});
    }
}
