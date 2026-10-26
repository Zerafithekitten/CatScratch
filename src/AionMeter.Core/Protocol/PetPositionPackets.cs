using System.Buffers.Binary;
using AionMeter.Core.Game;

namespace AionMeter.Core.Protocol;

/// <summary>Narrow coordinate-notification layout checked against teleport anchors and paired video.
/// Actor IDs are repeated in the record; never infer the owner from proximity or movement.</summary>
public static class PetPositionPackets
{
    public static ActorPositionEvent? Notification(long timeMs,ReadOnlySpan<byte> body)
    {
        var at=0;
        if(!Wire.TryVarint(body,ref at,out var actor)||actor==0||at+2>body.Length||body[at++]!=1||body[at++]!=0x11||
           !Wire.TryVarint(body,ref at,out _))return null;
        if(body.Length-at<20)return null;
        var first=BinaryPrimitives.ReadUInt32LittleEndian(body[at..]);
        var second=BinaryPrimitives.ReadUInt32LittleEndian(body[(at+4)..]);
        if((first,second) is not ((10002,5000) or (231,200) or (200,300))||
           BinaryPrimitives.ReadUInt32LittleEndian(body[(at+8)..])!=0)return null;
        at+=20;
        if(!Wire.TryVarint(body,ref at,out var repeated)||repeated!=actor||body.Length-at!=14||body[at++]!=1||body[at++]!=0)return null;
        var x=BinaryPrimitives.ReadSingleLittleEndian(body[at..]);
        var y=BinaryPrimitives.ReadSingleLittleEndian(body[(at+4)..]);
        var z=BinaryPrimitives.ReadSingleLittleEndian(body[(at+8)..]);
        if(!float.IsFinite(x)||!float.IsFinite(y)||!float.IsFinite(z)||Math.Abs(x)>=10000000||Math.Abs(y)>=10000000||Math.Abs(z)>=10000000)return null;
        return new(timeMs,actor,x,y,z);
    }
}
