using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Protocol;

namespace AionMeter.Tests;

public class PetNavigationTests
{
    private static PetNavigation Create()
    {
        var navigation=new PetNavigation();navigation.Observe(new ZoneChangedEvent(0,1110,"Altgard",false));
        navigation.Observe(new SelfIdentifiedEvent(0,1,"Test",1,GameClass.Cleric));return navigation;
    }
    [Fact]public void ForeignOrInvalidCoordinatesNeverBecomeOwnPosition()
    {
        var navigation=Create();navigation.Observe(new ActorPositionEvent(100,2,1,2,3));navigation.Observe(new ActorPositionEvent(100,1,float.NaN,2,3));
        Assert.Null(navigation.Snapshot(100));
    }
    [Fact]public void ProjectionStopsAndStalePositionIsUnavailable()
    {
        var navigation=Create();navigation.Observe(new ActorPositionEvent(100,1,0,0,0));navigation.Observe(new ActorPositionEvent(1100,1,100,0,0));
        Assert.Equal(150,navigation.Snapshot(1600)!.X);Assert.Equal(175,navigation.Snapshot(2100)!.X);
        Assert.True(navigation.Snapshot(2100)!.UncertaintyUnits>navigation.Snapshot(1600)!.UncertaintyUnits);
        Assert.Null(navigation.Snapshot(2601));Assert.Null(navigation.Snapshot(1099));
        navigation.Observe(new ActorPositionEvent(1000,1,500,0,0));Assert.Equal(100,navigation.Snapshot(1100)!.X);
    }
    [Fact]public void ContextChangesAndCaptureResetRemoveGuidance()
    {
        foreach(var change in new GameEvent[]{new ZoneChangedEvent(200,1110,"Altgard",false,true),new SelfIdentifiedEvent(200,1,"Alt",1,GameClass.Cleric),new ResourceSnapshotStartedEvent(200)})
        {var navigation=Create();navigation.Observe(new ActorPositionEvent(100,1,1,2,3));navigation.Observe(change);Assert.Null(navigation.Snapshot(200));}
        var state=Create();state.Observe(new ActorPositionEvent(100,1,1,2,3));state.Reset();Assert.Null(state.Snapshot(100));
    }
    [Fact]public void OnlyFreshMatchingSourcesCanBeRankedAndNoMapPixelsAreUsed()
    {
        var navigation=Create();navigation.Observe(new ActorPositionEvent(100,1,10,20,30));var own=navigation.Snapshot(100);
        NpcSeenEvent[] npcs=[new(100,2,7,X:110,Y:20,Z:30),new(100,3,7,X:10,Y:70,Z:30),new(100,4,8,X:11,Y:20,Z:30)];
        var target=PetNavigation.Nearest(own,1110,npcs,new HashSet<int>{7},100)!;
        Assert.Equal((uint)3,target.ActorId);Assert.Equal(50,target.DistanceUnits);Assert.Equal(90,target.BearingDegrees);
        Assert.Null(PetNavigation.Nearest(own,1010,npcs,new HashSet<int>{7},100));
        Assert.Null(PetNavigation.Nearest(own,1110,npcs,new HashSet<int>{7},2000));
        Assert.Null(PetNavigation.Nearest(own,1110,[new(100,2,7)],new HashSet<int>{7},100));
    }
    [Fact]public void AvailableDistancesOrderReceivedIncompletePetsOnly()
    {
        PetMapLocation[] locations=[new(1,1110,"Altgard","A",[]),new(2,1110,"Altgard","B",[]),new(3,1110,"Altgard","C",[])];
        var states=new Dictionary<int,PetState>{{3,new(3,3,0)}};
        var guidance=new Dictionary<int,PetTargetGuidance>{{1,new(1,1,1000,0,100,50)},{2,new(2,2,200,90,100,50)}};
        var result=PetRecommendations.Select(locations,1110,states,true,new HashSet<int>{1,2,3},guidance:guidance);
        Assert.Equal(new[]{2,1},result.Select(r=>r.PetId));
    }
    [Fact]public void NarrowLoginAnchorWaitsForIdentityAndClearsOnTeleport()
    {
        var navigation=new PetNavigation();navigation.Observe(new ZoneChangedEvent(1,1110,"Altgard",false));
        navigation.Observe(new PetPositionAnchorEvent(100,10,20,30));Assert.Null(navigation.Snapshot(100));
        navigation.Observe(new SelfIdentifiedEvent(148,1,"Test",1,GameClass.Cleric));Assert.Equal(10,navigation.Snapshot(148)!.X);
        navigation.Observe(new ZoneChangedEvent(200,1110,"Altgard",false,true));Assert.Null(navigation.Snapshot(200));
    }
    [Fact]public void TeleportDecoderIsOptInAndRejectsTruncatedOrDifferentLayouts()
    {
        var events=new List<GameEvent>();var parser=new PacketParser(GameData.Empty,events.Add){TimeMs=100};
        var body=Convert.FromHexString("000000000000AF9EADC7BFB0E7C700D7004703000000DA6A");
        parser.Handle(Opcodes.Teleport,body);Assert.Empty(events);parser.DecodePetPositions=true;parser.Handle(Opcodes.Teleport,body);
        Assert.InRange(Assert.IsType<PetPositionAnchorEvent>(Assert.Single(events)).X,-88894,-88892);
        events.Clear();parser.Handle(Opcodes.Teleport,body[..23]);body[0]=1;parser.Handle(Opcodes.Teleport,body);Assert.Empty(events);
    }
    [Theory]
    [InlineData(0,0,90)][InlineData(90,0,0)][InlineData(180,0,270)][InlineData(-90,0,180)]
    [InlineData(90,90,90)][InlineData(0,90,180)][InlineData(180,90,0)][InlineData(270,270,90)]
    public void CharacterRelativeBearingUsesFacingNotWorldNorth(double target,double facing,double screen)
        =>Assert.Equal(screen,PetNavigation.RelativeBearing(target,facing),6);
    [Fact]public void HeadingMustBelongToOwnActorBeFreshAndMustNotComeFromMovement()
    {
        var navigation=Create();navigation.Observe(new ActorPositionEvent(100,1,100,100,30));
        navigation.Observe(new PetFacingEvent(100,2,90));Assert.Null(navigation.Snapshot(100)!.FacingDegrees);
        navigation.Observe(new PetFacingEvent(100,1,0));navigation.Observe(new ActorPositionEvent(200,1,90,100,30));
        Assert.Equal(0,navigation.Snapshot(200)!.FacingDegrees); // walking backward does not flip the figure
        navigation.Observe(new PetFacingEvent(201,1,double.NaN));navigation.Observe(new PetFacingEvent(99,1,90));
        Assert.Equal(0,navigation.Snapshot(201)!.FacingDegrees);
        navigation.Observe(new ActorPositionEvent(1700,1,90,100,30));Assert.Null(navigation.Snapshot(1700)!.FacingDegrees);
        navigation.Observe(new PetFacingEvent(1700,1,90));Assert.Equal(90,navigation.Snapshot(1700)!.FacingDegrees);
        navigation.Observe(new ZoneChangedEvent(1701,1110,"Altgard",false,true));navigation.Observe(new ActorPositionEvent(1702,1,90,100,30));
        Assert.Null(navigation.Snapshot(1702)!.FacingDegrees);
    }
    [Fact]public void MissingHeadingDoesNotRemoveDistanceButCannotProduceAnArrow()
    {
        var navigation=Create();navigation.Observe(new ActorPositionEvent(100,1,10,20,30));
        NpcSeenEvent[] npcs=[new(100,2,7,X:110,Y:20,Z:30)];
        Assert.Null(PetNavigation.Nearest(navigation.Snapshot(100),1110,npcs,new HashSet<int>{7},100)!.RelativeBearingDegrees);
        navigation.Observe(new PetFacingEvent(100,1,0));
        Assert.Equal(90,PetNavigation.Nearest(navigation.Snapshot(100),1110,npcs,new HashSet<int>{7},100)!.RelativeBearingDegrees);
    }
}
