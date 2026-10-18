using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Protocol;

namespace AionMeter.Tests;

public class HiddenCubeTests
{
    private static byte[] ObjectBody(int template=HiddenCubePackets.ConfirmedTemplate)
    {
        // Synthetic object with no character, location or connection information from a capture.
        var b=new byte[53];b[0]=7;b[1]=4;
        BitConverter.GetBytes(template).CopyTo(b,3);
        BitConverter.GetBytes(1f).CopyTo(b,7);BitConverter.GetBytes(2f).CopyTo(b,11);BitConverter.GetBytes(3f).CopyTo(b,15);
        return b;
    }
    [Fact] public void ConfirmedObjectAndOpenedStateDecodeThroughParser()
    {
        var events=new List<GameEvent>();var parser=new PacketParser(GameData.Empty,events.Add){TimeMs=1000};
        parser.Handle(0x3436,ObjectBody());parser.Handle(0x3536,new byte[]{7,4,3,0});
        Assert.Equal(new HiddenCubeSeenEvent(1000,7,HiddenCubePackets.ConfirmedTemplate),events[0]);
        Assert.Equal(new HiddenCubeGoneEvent(1000,7),events[1]);
    }
    [Theory][InlineData(1300865)][InlineData(1300891)][InlineData(2400751)]
    public void OtherWorldObjectsAreNotCubeAlerts(int template)=>Assert.Null(HiddenCubePackets.Seen(1,ObjectBody(template)));
    [Fact] public void TruncatedObjectsAndInvalidPositionsAreRejected()
    {
        var body=ObjectBody();for(var n=0;n<body.Length;n++)Assert.Null(HiddenCubePackets.Seen(1,body.AsSpan(0,n)));
        BitConverter.GetBytes(float.NaN).CopyTo(body,7);Assert.Null(HiddenCubePackets.Seen(1,body));
        Assert.Null(HiddenCubePackets.State(1,new byte[]{7,1,1,0}));
    }
    [Fact] public void RepeatedAnnouncementsDoNotDuplicateAndOpeningSuppressesOldObject()
    {
        var cubes=new HiddenCubes();cubes.Observe(new HiddenCubeSeenEvent(1,7,HiddenCubePackets.ConfirmedTemplate));
        cubes.Observe(new HiddenCubeSeenEvent(2,7,HiddenCubePackets.ConfirmedTemplate));Assert.Equal(1,cubes.Count(3));
        cubes.Observe(new HiddenCubeGoneEvent(4,99));Assert.Equal(1,cubes.Count(4));
        cubes.Observe(new HiddenCubeGoneEvent(5,7));Assert.Equal(0,cubes.Count(5));
        cubes.Observe(new HiddenCubeSeenEvent(6,7,HiddenCubePackets.ConfirmedTemplate));Assert.Equal(0,cubes.Count(6));
    }
    [Fact] public void LocationCharacterAndCaptureChangesClearAlerts()
    {
        var cubes=new HiddenCubes();cubes.Observe(new SelfIdentifiedEvent(1,1,"One",1,GameClass.Cleric));
        void Seen()=>cubes.Observe(new HiddenCubeSeenEvent(2,7,HiddenCubePackets.ConfirmedTemplate));
        Seen();cubes.Observe(new ZoneChangedEvent(3,1110,"Map",false));Assert.Equal(0,cubes.Count(3));
        Seen();cubes.Observe(new SelfIdentifiedEvent(4,1,"Two",1,GameClass.Cleric));Assert.Equal(0,cubes.Count(4));
        Seen();cubes.Reset();Assert.Equal(0,cubes.Count(5));
        Seen();Assert.Equal(0,cubes.Count(2+HiddenCubes.LifetimeMs));
    }
}
