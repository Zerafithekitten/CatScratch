using System.Buffers.Binary;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Protocol;

namespace AionMeter.Tests;

public class PetPositionPacketTests
{
    private static byte[] Record(uint actor=3084,uint repeated=3084,uint first=231,uint second=200,float x=1000)
    {
        var result=new List<byte>();
        void Var(uint v){while(v>=128){result.Add((byte)((v&127)|128));v>>=7;}result.Add((byte)v);}
        void U32(uint v){var bytes=new byte[4];BinaryPrimitives.WriteUInt32LittleEndian(bytes,v);result.AddRange(bytes);}
        Var(actor);result.AddRange([1,0x11]);Var(300);U32(first);U32(second);U32(0);result.AddRange(new byte[8]);
        Var(repeated);result.AddRange([1,0]);
        U32(BitConverter.SingleToUInt32Bits(x));U32(BitConverter.SingleToUInt32Bits(-2000));U32(BitConverter.SingleToUInt32Bits(300));
        return result.ToArray();
    }

    [Theory][InlineData(10002u,5000u)][InlineData(231u,200u)][InlineData(200u,300u)]
    public void ValidatedShapesPreserveActorAndCoordinates(uint first,uint second)
    {
        var pose=PetPositionPackets.Notification(100,Record(first:first,second:second));
        Assert.NotNull(pose);Assert.Equal(3084u,pose.ActorId);Assert.Equal(1000,pose.X);Assert.Equal(-2000,pose.Y);Assert.Equal(300,pose.Z);
    }
    [Fact]public void EveryTruncationAndTrailingDataAreRejected()
    {
        var body=Record();for(var length=0;length<body.Length;length++)Assert.Null(PetPositionPackets.Notification(100,body.AsSpan(0,length)));
        Assert.Null(PetPositionPackets.Notification(100,[..body,0]));
    }
    [Fact]public void DecoderOnlyRunsWhilePetPositionCaptureIsEnabled()
    {
        var events=new List<GameEvent>();var parser=new PacketParser(GameData.Empty,events.Add){TimeMs=100};
        parser.Handle(0x2A38,Record());Assert.Empty(events);
        parser.DecodePetPositions=true;parser.Handle(0x2A38,Record());
        Assert.Equal(3084u,Assert.IsType<ActorPositionEvent>(Assert.Single(events)).ActorId);
        parser.DecodePetPositions=false;parser.Handle(0x2A38,Record());Assert.Single(events);
    }
    [Fact]public void UnknownShapesMismatchedActorAndInvalidCoordinatesAreRejected()
    {
        Assert.Null(PetPositionPackets.Notification(100,Record(repeated:22)));
        Assert.Null(PetPositionPackets.Notification(100,Record(first:232)));
        Assert.Null(PetPositionPackets.Notification(100,Record(x:float.NaN)));
        Assert.Null(PetPositionPackets.Notification(100,Record(x:float.PositiveInfinity)));
        var body=Record();body[3]=0x10;Assert.Null(PetPositionPackets.Notification(100,body));
    }
    [Theory][InlineData(1u)][InlineData(3084u)][InlineData(300000u)]
    public void ForeignActorsAreNeverPromotedToOwnPosition(uint actor)
    {
        var pose=PetPositionPackets.Notification(100,Record(actor,actor));Assert.NotNull(pose);
        var navigation=new PetNavigation();navigation.Observe(new AionMeter.Core.Events.ZoneChangedEvent(0,1110,"Altgard",false));
        navigation.Observe(new AionMeter.Core.Events.SelfIdentifiedEvent(0,99,"Test",1,GameClass.Cleric));navigation.Observe(pose);
        Assert.Null(navigation.Snapshot(100));
    }
}
