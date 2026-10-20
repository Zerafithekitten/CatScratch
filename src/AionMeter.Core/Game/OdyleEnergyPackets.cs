using AionMeter.Core.Events;
using AionMeter.Core.Protocol;
namespace AionMeter.Core.Game;

/// <summary>Server-confirmed available and supplementary Odyle. Capacity and recharge deadlines remain unknown.</summary>
public sealed record OdyleEnergyEvent(long TimeMs,uint Available,uint Supplementary):GameEvent(TimeMs);

public static class OdyleEnergyPackets
{
    public const uint PropertyId=60000001;
    public static OdyleEnergyEvent? Snapshot(long time,ReadOnlySpan<byte> body)
    {
        var at=0;
        if(!Wire.TryVarint(body,ref at,out var count) || count>1024)return null;
        OdyleEnergyEvent? result=null;
        for(var n=0;n<count;n++)
        {
            if(!Entry(time,body,ref at,out var id,out var entry))return null;
            if(id!=PropertyId)continue;
            if(result is not null || entry is null)return null;
            result=entry;
        }
        return at==body.Length?result:null;
    }
    public static OdyleEnergyEvent? Update(long time,ReadOnlySpan<byte> body)
    {
        if(body.Length<7 || body[0]>1)return null;
        var at=1;
        if(!Entry(time,body,ref at,out var id,out var value) || id!=PropertyId)return null;
        // Confirmed regeneration and item-use envelopes, respectively. Never infer the balance from the delta.
        var valid=body[0]==0?at==body.Length-1 && body[at]==2:
            at==body.Length-5 && body[at]==1 && Wire.U32(body,at+1)>0;
        return valid?value:null;
    }
    private static bool Entry(long time,ReadOnlySpan<byte> body,ref int at,out uint id,out OdyleEnergyEvent? value)
    {
        id=0;value=null;
        if(at+5>body.Length)return false;
        var flags=body[at++];id=Wire.U32(body,at);at+=4;
        if((flags&~15)!=0)return false;
        // Property flags describe optional numeric components; validate the entire table before emitting.
        var wide=((flags&1)!=0?8:0)+((flags&2)!=0?8:0);
        if(at+wide>body.Length)return false;
        at+=wide;
        uint available=0,supplementary=0;
        if((flags&4)!=0 && !Wire.TryVarint(body,ref at,out available))return false;
        if((flags&8)!=0 && !Wire.TryVarint(body,ref at,out supplementary))return false;
        if(id==PropertyId && (flags&3)==0)value=new(time,available,supplementary);
        return true;
    }
}
