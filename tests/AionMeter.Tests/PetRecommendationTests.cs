using AionMeter.Core.Events;
using AionMeter.Core.Game;

namespace AionMeter.Tests;

public class PetRecommendationTests
{
    private static readonly PetMapLocation[] Locations=[new(1001,1010,"Verteron","A",[]),new(1001,1010,"Verteron","B",[]),new(1002,1110,"Altgard","C",[]),new(1003,1010,"Verteron","D",[])];
    [Fact] public void RegionNeedsNoAttackAndDeduplicatesPets()
    {
        var result=PetRecommendations.Select(Locations,1010,new Dictionary<int,PetState>(),true,new HashSet<int>());
        Assert.Equal(new[]{1001,1003},result.Select(r=>r.PetId));Assert.All(result,r=>Assert.False(r.Nearby));
    }
    [Fact] public void UnknownCollectionIsNeverAssumedMissing()=>Assert.Empty(PetRecommendations.Select(Locations,1010,new Dictionary<int,PetState>(),false,new HashSet<int>{1001}));
    [Fact] public void SeenMonstersTakePriorityAndCompletedPetsAreExcluded()
    {
        var states=new Dictionary<int,PetState>{{1001,new(1001,3,0)},{1002,new(1002,1,10)}};
        var result=PetRecommendations.Select(Locations,1010,states,true,new HashSet<int>{1002},guidance:new Dictionary<int,PetTargetGuidance>{{1002,new(1,1,1000,0,100,50)}});
        Assert.Equal(new[]{1002,1003},result.Select(r=>r.PetId));Assert.Equal(15,result[0].Needed);Assert.True(result[0].Nearby);
    }
    [Fact] public void UnknownMapDoesNotRecommendOtherRegions()=>Assert.Empty(PetRecommendations.Select(Locations,0,new Dictionary<int,PetState>(),true,new HashSet<int>()));
    [Fact]public void ReceivedFarMonstersCannotDisplaceCloserFarmLocationsOrClaimNearby()
    {
        var guidance=new Dictionary<int,PetTargetGuidance>{{1001,new(7,7,100000,0,100,50)},{1003,new(0,8,1000,0,100,50,null,true)}};
        var rows=PetRecommendations.Select(Locations,1010,new Dictionary<int,PetState>(),true,new HashSet<int>{1001},guidance:guidance);
        Assert.Equal(1003,rows[0].PetId);Assert.All(rows,r=>Assert.False(r.Nearby));
    }
    [Theory][InlineData(9900,50,true)][InlineData(9990,50,false)][InlineData(50000,50,false)]
    public void NearbyRequiresMeasuredDistanceIncludingUncertainty(double distance,double uncertainty,bool expected)
        =>Assert.Equal(expected,PetNavigation.IsNearby(new(1,7,distance,0,100,uncertainty)));
    [Fact]public void FarmAndHistoricalCoordinatesNeverEstablishNearby()
    {
        Assert.False(PetNavigation.IsNearby(new(0,7,100,0,100,50,null,true)));
        Assert.False(PetNavigation.IsNearby(new(1,7,100,0,100,50,null,false,true)));
        var rows=PetRecommendations.Select(Locations,1010,new Dictionary<int,PetState>(),true,new HashSet<int>{1001});
        Assert.All(rows,r=>Assert.False(r.Nearby));
    }
    [Fact]public void NearbyOnlyHasNoRegionalFarmHistoricalOrUnmeasuredFallback()
    {
        PetMapLocation[] places=Enumerable.Range(1,5).Select(id=>new PetMapLocation(id,1110,"Altgard","A",[])).ToArray();
        var guides=new Dictionary<int,PetTargetGuidance>{{1,new(7,7,1000,0,100,50)},{2,new(8,8,50000,0,100,50)},
            {3,new(0,9,100,0,100,50,null,true)},{4,new(9,10,100,0,100,50,null,false,true)}};
        var result=PetRecommendations.Select(places,1110,new Dictionary<int,PetState>(),true,new HashSet<int>{1,2,3,4,5},guidance:guides,nearbyOnly:true);
        Assert.Equal(1,Assert.Single(result).PetId);
        Assert.Empty(PetRecommendations.Select(places,1110,new Dictionary<int,PetState>(),true,new HashSet<int>(),nearbyOnly:true));
        Assert.Equal(5,PetRecommendations.Select(places,1110,new Dictionary<int,PetState>(),true,new HashSet<int>()).Count);
    }
    [Fact]public void ReportedPresenceSurvivesStillnessUntilEachSourceIsRemoved()
    {
        var collection=new PetCollection();collection.Observe(new SelfIdentifiedEvent(0,1,"Test",2304,GameClass.Cleric));
        collection.Observe(new ZoneChangedEvent(1,1110,"Altgard",false));
        collection.Observe(new NpcSeenEvent(1000,44966,2400734,X:233120,Y:-207296,Z:1391));
        collection.Observe(new NpcSeenEvent(1100,46945,2400734,X:229965,Y:-209716,Z:1309));
        var places=new[]{new PetMapLocation(1127,1110,"Altgard","Faded Odyle Spirit",[])};
        IReadOnlyList<PetRecommendation> Rows()=>PetRecommendations.Select(places,1110,new Dictionary<int,PetState>(),true,new HashSet<int>(),nearbyOnly:true,
            detectedPets:collection.Nearby().Npcs.Any(n=>n.NpcCode==2400734)?new HashSet<int>{1127}:new HashSet<int>());
        Assert.Single(Rows());
        // Twenty seconds without a new coordinate do not remove stationary sources.
        Assert.All(collection.Nearby().Npcs,n=>Assert.True(21000-n.TimeMs>PetNavigation.FreshMs));Assert.Single(Rows());
        var parser=new AionMeter.Core.Protocol.PacketParser(GameData.Empty,collection.Observe){TimeMs=22000};
        parser.Handle(AionMeter.Core.Protocol.Opcodes.Death,Convert.FromHexString("A6DF020001")); // captured exact actor-removal layout, actor 44966
        Assert.Single(Rows());Assert.DoesNotContain(collection.Nearby().Npcs,n=>n.ActorId==44966);
        collection.Observe(new DeathEvent(23000,46945));Assert.Empty(Rows());
        collection.Observe(new NpcSeenEvent(24000,44966,2400734,X:233120,Y:-207296,Z:1391));Assert.Single(Rows());
        collection.InvalidateLocation();Assert.Empty(Rows());
    }
    [Theory][InlineData(5)][InlineData(256)]
    public void FivePetCapStillPrioritizesNearbyAndExcludesCompleted(int requested)
    {
        var locations=Enumerable.Range(1001,10).Select(id=>new PetMapLocation(id,1010,"Verteron","Source",[]));
        var states=new Dictionary<int,PetState>{{1001,new(1001,3,0)}};
        var result=PetRecommendations.Select(locations,1010,states,true,new HashSet<int>{1010},requested,guidance:new Dictionary<int,PetTargetGuidance>{{1010,new(1,1,1000,0,100,50)}});
        Assert.Equal(5,result.Count);Assert.Equal(1010,result[0].PetId);Assert.DoesNotContain(result,r=>r.PetId==1001);
    }
    [Fact] public void ChangingRegionAndDisconnectingClearOldTargetsAndPositions()
    {
        var pets=new PetCollection();pets.Observe(new SelfIdentifiedEvent(1,1,"Self",2305,GameClass.Cleric));
        pets.Observe(new ZoneChangedEvent(2,1010,"Verteron",false));pets.Observe(new NpcSeenEvent(3,2,2100453));
        pets.Observe(new DamageEvent(4,1,2,17010000,1,HitFlags.None));pets.Observe(new ActorPositionEvent(5,1,10,20,30));
        pets.Observe(new ZoneChangedEvent(6,1110,"Altgard",false));
        Assert.Equal(1110,pets.MapId);Assert.Empty(pets.Nearby().Npcs);Assert.Null(pets.Nearby().Own);Assert.Empty(pets.AttackedNpcCodes());
        pets.InvalidateLocation();Assert.Equal(0,pets.MapId);
    }
    [Fact] public void CharacterSwitchDoesNotReusePreviousLocation()
    {
        var pets=new PetCollection();pets.Observe(new SelfIdentifiedEvent(1,1,"One",2305,GameClass.Cleric));pets.Observe(new ZoneChangedEvent(2,1010,"Verteron",false));
        pets.Observe(new SelfIdentifiedEvent(3,2,"Two",2305,GameClass.Cleric));Assert.Equal(0,pets.MapId);
    }
}
