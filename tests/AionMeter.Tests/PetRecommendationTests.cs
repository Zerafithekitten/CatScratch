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
        var result=PetRecommendations.Select(Locations,1010,states,true,new HashSet<int>{1002});
        Assert.Equal(new[]{1002,1003},result.Select(r=>r.PetId));Assert.Equal(15,result[0].Needed);Assert.True(result[0].Nearby);
    }
    [Fact] public void UnknownMapDoesNotRecommendOtherRegions()=>Assert.Empty(PetRecommendations.Select(Locations,0,new Dictionary<int,PetState>(),true,new HashSet<int>()));
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
