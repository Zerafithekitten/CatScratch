using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Protocol;

namespace AionMeter.Tests;

public class HiddenCubeTests
{
    [Fact] public void ConfirmedRemovalShowsAFiveSecondNoticeWithoutRepeatingOnDuplicatePackets()
    {
        var cubes=new HiddenCubes();cubes.Observe(new HiddenCubeSeenEvent(100,7,HiddenCubePackets.ConfirmedTemplate));
        cubes.Observe(new HiddenCubeGoneEvent(200,7));
        Assert.Equal(new HiddenCubeStatus(0,true),cubes.Status(200));
        cubes.Observe(new HiddenCubeGoneEvent(300,7));
        Assert.True(cubes.Status(200+HiddenCubes.GoneNoticeMs-1).RecentlyGone);
        Assert.False(cubes.Status(200+HiddenCubes.GoneNoticeMs).RecentlyGone);
    }
    [Fact] public void UnknownExpiredAndOutOfOrderRemovalsNeverProduceACompletionNotice()
    {
        var cubes=new HiddenCubes();cubes.Observe(new HiddenCubeGoneEvent(50,7));Assert.False(cubes.Status(50).RecentlyGone);
        cubes.Observe(new HiddenCubeSeenEvent(100,7,HiddenCubePackets.ConfirmedTemplate));
        cubes.Observe(new HiddenCubeGoneEvent(99,7));Assert.Equal(new HiddenCubeStatus(1,false),cubes.Status(100));
        cubes.Observe(new HiddenCubeGoneEvent(100+HiddenCubes.LifetimeMs,7));
        Assert.Equal(new HiddenCubeStatus(0,false),cubes.Status(100+HiddenCubes.LifetimeMs));
        cubes.Observe(new HiddenCubeSeenEvent(70_000,8,HiddenCubePackets.VerteronTemplate));
        Assert.Equal(new HiddenCubeStatus(0,false),cubes.Status(70_000+HiddenCubes.LifetimeMs));
    }
    [Fact] public void OtherNearbyCubesSurviveTheNoticeAndNewFindsTakePriority()
    {
        var cubes=new HiddenCubes();cubes.Observe(new HiddenCubeSeenEvent(100,7,HiddenCubePackets.ConfirmedTemplate));
        cubes.Observe(new HiddenCubeSeenEvent(110,8,HiddenCubePackets.VerteronTemplate));
        cubes.Observe(new HiddenCubeGoneEvent(200,7));Assert.Equal(new HiddenCubeStatus(1,true),cubes.Status(200));
        Assert.Equal(new HiddenCubeStatus(1,false),cubes.Status(200+HiddenCubes.GoneNoticeMs));
        cubes.Observe(new HiddenCubeSeenEvent(6000,9,HiddenCubePackets.ConfirmedTemplate));Assert.Equal(new HiddenCubeStatus(2,false),cubes.Status(6000));
    }
    [Theory][InlineData("zone")][InlineData("character")][InlineData("capture")]
    public void ContextChangesDiscardCompletionNotices(string change)
    {
        var cubes=new HiddenCubes();cubes.Observe(new SelfIdentifiedEvent(1,1,"One",1,GameClass.Cleric));
        cubes.Observe(new HiddenCubeSeenEvent(100,7,HiddenCubePackets.ConfirmedTemplate));cubes.Observe(new HiddenCubeGoneEvent(200,7));
        if(change=="zone")cubes.Observe(new ZoneChangedEvent(201,1110,"Map",false));
        else if(change=="character")cubes.Observe(new SelfIdentifiedEvent(201,2,"Two",1,GameClass.Cleric));
        else cubes.Reset();
        Assert.Equal(new HiddenCubeStatus(0,false),cubes.Status(201));
    }
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
    [Fact] public void VerteronObjectUsesTheSameAlertAndOpenLifecycle()
    {
        var cubes=new HiddenCubes();var events=new List<GameEvent>();
        var parser=new PacketParser(GameData.Empty,e=>{events.Add(e);cubes.Observe(e);}){TimeMs=1000};
        parser.Handle(0x3436,ObjectBody(HiddenCubePackets.VerteronTemplate));
        Assert.Equal(new HiddenCubeSeenEvent(1000,7,1200039),Assert.Single(events));
        Assert.Equal(1,cubes.Count(1000));Assert.Equal(1,cubes.DetectionVersion);
        parser.Handle(0x3436,ObjectBody(HiddenCubePackets.VerteronTemplate));
        Assert.Equal(1,cubes.Count(1000));Assert.Equal(1,cubes.DetectionVersion);
        parser.Handle(0x3536,new byte[]{7,4,3,0});Assert.Equal(0,cubes.Count(1000));
        parser.Handle(0x3436,ObjectBody(HiddenCubePackets.VerteronTemplate));Assert.Equal(0,cubes.Count(1000));
    }
    [Theory][InlineData(1200038)][InlineData(1200040)][InlineData(1500000)]
    public void UnconfirmedNeighboringTemplatesAreNotCubeAlerts(int template)=>Assert.Null(HiddenCubePackets.Seen(1,ObjectBody(template)));
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
    [Fact] public void NotificationVersionChangesOnlyForNewDetectionsAndNeverRewindsOnReset()
    {
        var cubes=new HiddenCubes();Assert.Equal(0,cubes.DetectionVersion);
        cubes.Observe(new HiddenCubeSeenEvent(1,7,HiddenCubePackets.ConfirmedTemplate));Assert.Equal(1,cubes.DetectionVersion);
        cubes.Observe(new HiddenCubeSeenEvent(2,7,HiddenCubePackets.ConfirmedTemplate));Assert.Equal(1,cubes.DetectionVersion);
        cubes.Count(3);cubes.Count(4);Assert.Equal(1,cubes.DetectionVersion);
        cubes.Observe(new HiddenCubeSeenEvent(5,8,HiddenCubePackets.ConfirmedTemplate));Assert.Equal(2,cubes.DetectionVersion);
        cubes.Reset();Assert.Equal(2,cubes.DetectionVersion);
        cubes.Observe(new HiddenCubeSeenEvent(6,7,HiddenCubePackets.ConfirmedTemplate));Assert.Equal(3,cubes.DetectionVersion);
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
    [Fact] public void LaterCubeFindsAcrossLocationsAdvanceSoundNotificationVersion()
    {
        var cubes=new HiddenCubes();
        cubes.Observe(new HiddenCubeSeenEvent(1,7,HiddenCubePackets.ConfirmedTemplate));
        cubes.Observe(new HiddenCubeSeenEvent(2,8,HiddenCubePackets.ConfirmedTemplate));
        Assert.Equal(2,cubes.DetectionVersion);
        cubes.Observe(new HiddenCubeGoneEvent(3,7));cubes.Observe(new HiddenCubeGoneEvent(4,8));
        Assert.Equal(0,cubes.Count(5));
        cubes.Observe(new HiddenCubeSeenEvent(120000,9,HiddenCubePackets.ConfirmedTemplate));
        Assert.Equal(3,cubes.DetectionVersion);Assert.Equal(1,cubes.Count(120000));
        cubes.Observe(new ZoneChangedEvent(120001,1110,"Map",false));
        cubes.Observe(new HiddenCubeSeenEvent(120002,9,HiddenCubePackets.ConfirmedTemplate));
        Assert.Equal(4,cubes.DetectionVersion);Assert.Equal(1,cubes.Count(120002));
    }
}
