using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Protocol;

namespace AionMeter.Tests;

public class PetCollectionTests
{
    private static byte[] Baseline=>Wire.FromHex("0000000001a9040000a90400000100000001a90400000a000000");
    private static byte[] Delta=>Wire.FromHex("01a904000001000000");
    private static SelfIdentifiedEvent Own(string name="Own")=>new(1,1,name,2305,GameClass.Cleric);
    [Fact] public void GlobalCollectionAndSoulDeltaReconstructTenToEleven()
    {
        var tracker=new PetCollection();tracker.Observe(Own());
        var full=PetPackets.Collection(10,Baseline);Assert.NotNull(full);tracker.Observe(full!);
        Assert.Equal(new PetState(1193,1,10),tracker.Snapshot().Single());
        tracker.Observe(PetPackets.Souls(11,Delta)!);
        Assert.Equal(new PetState(1193,1,11),tracker.Snapshot().Single());
        Assert.Equal(14,tracker.Snapshot().Single().Needed(2));
    }
    [Fact] public void EveryTruncatedPrefixAndMalformedDeltaIsRejected()
    {
        for(var length=0;length<Baseline.Length;length++)Assert.Null(PetPackets.Collection(0,Baseline.AsSpan(0,length)));
        Assert.Null(PetPackets.Souls(0,Delta[..^1]));Assert.Null(PetPackets.Souls(0,[..Delta,0]));
        var corrupt=(byte[])Baseline.Clone();corrupt[14]=255;Assert.Null(PetPackets.Collection(0,corrupt));
    }
    [Fact] public void ADeltaWithoutAConfirmedBaselineCannotInventProgress()
    {
        var tracker=new PetCollection();tracker.Observe(Own());tracker.Observe(PetPackets.Souls(1,Delta)!);
        Assert.False(tracker.Confirmed);Assert.Empty(tracker.Snapshot());
    }
    [Fact] public void LoadingAndActorChangesPreserveCollectionButChangingCharacterClearsIt()
    {
        var tracker=new PetCollection();tracker.Observe(Own());tracker.Observe(PetPackets.Collection(1,Baseline)!);
        tracker.Observe(new ZoneChangedEvent(2,600001,"Dungeon",true));tracker.Observe(Own() with {ActorId=2});
        Assert.Equal(10,tracker.Snapshot().Single().Souls);
        tracker.Observe(Own("Other"));Assert.Empty(tracker.Snapshot());Assert.False(tracker.Confirmed);
    }
    [Fact] public void CollectionArrivingBeforeIdentityIsBuffered()
    {
        var tracker=new PetCollection();tracker.Observe(PetPackets.Collection(1,Baseline)!);Assert.False(tracker.Confirmed);
        tracker.Observe(Own());Assert.True(tracker.Confirmed);Assert.Single(tracker.Snapshot());
    }
    [Theory][InlineData(0,0,1,5)][InlineData(0,2,3,103)][InlineData(1,11,2,14)][InlineData(1,11,3,89)][InlineData(2,70,3,5)][InlineData(3,0,3,0)]
    public void PhaseGoalsRespectGlobalThresholds(int level,int souls,int goal,int expected)=>Assert.Equal(expected,new PetState(1193,level,souls).Needed(goal));
    [Fact] public void EveryPetUnlocksAfterExactlyFiveSouls()
    {
        var tracker=new PetCollection();tracker.Observe(Own());tracker.Observe(new PetCollectionEvent(1,new Dictionary<int,PetState>()));
        tracker.Observe(new PetSoulEvent(2,new Dictionary<int,int>{{1193,4},{1176,4}}));
        Assert.All(tracker.Snapshot(),pet=>{Assert.Equal(0,pet.Level);Assert.Equal(1,pet.Needed(1));});
        tracker.Observe(new PetSoulEvent(3,new Dictionary<int,int>{{1193,1},{1176,1}}));
        Assert.All(tracker.Snapshot(),pet=>{Assert.Equal(1,pet.Level);Assert.Equal(0,pet.Souls);Assert.Equal(25,pet.Needed(2));});
    }
    [Fact] public void LevelTransitionsUseGlobalCostsAndStopAtThree()
    {
        var tracker=new PetCollection();tracker.Observe(Own());tracker.Observe(new PetCollectionEvent(1,new Dictionary<int,PetState>{{1193,new(1193,1,24)}}));
        tracker.Observe(PetPackets.Souls(2,Delta)!);Assert.Equal(new PetState(1193,2,0),tracker.Snapshot().Single());
        tracker.Observe(new PetSoulEvent(3,new Dictionary<int,int>{{1193,75}}));Assert.Equal(new PetState(1193,3,0),tracker.Snapshot().Single());
    }
    [Fact] public void AmbiguousLootDoesNotCreateAnInventedFarmSource()
    {
        var tracker=new PetCollection();tracker.Observe(Own());tracker.Observe(PetPackets.Collection(1,Baseline)!);
        tracker.Observe(new ZoneChangedEvent(2,1110,"Altgard",false));tracker.Observe(new NpcSeenEvent(3,7,2400455));
        tracker.Observe(new PetSoulDropEvent(10,7,534660209,1));tracker.Observe(new PetSoulDropEvent(11,8,534660210,1));
        tracker.Observe(PetPackets.Souls(12,Delta)!);Assert.Empty(tracker.Farms(1193));
    }
    [Fact] public void ConfirmedUniqueLootLearnsTheFarmAndDeadNpcsLeaveTheNearbyView()
    {
        var tracker=new PetCollection();tracker.Observe(Own());tracker.Observe(PetPackets.Collection(1,Baseline)!);
        tracker.Observe(new ZoneChangedEvent(2,1110,"Altgard",false));tracker.Observe(new NpcSeenEvent(3,7,2400455,X:100,Y:200));
        tracker.Observe(new PetSoulDropEvent(10,7,534660209,1));tracker.Observe(PetPackets.Souls(12,Delta)!);
        Assert.Equal(2400455,tracker.Farms(1193).Single().NpcCode);
        tracker.Observe(new DeathEvent(13,7));Assert.Empty(tracker.Nearby().Npcs);
    }
    [Fact] public void OwnAttackTracksSpeciesWithoutAnyLocationDataAndIgnoresNearbyAndOtherHits()
    {
        var pets=new PetCollection();pets.Observe(Own());
        pets.Observe(new NpcSeenEvent(2,7,2400455));pets.Observe(new NpcSeenEvent(2,8,2400707));
        Assert.Empty(pets.AttackedNpcCodes());
        pets.Observe(new DamageEvent(3,99,7,1,100,HitFlags.None));Assert.Empty(pets.AttackedNpcCodes());
        pets.Observe(new DamageEvent(4,1,7,1,100,HitFlags.None));Assert.Equal(new[]{2400455},pets.AttackedNpcCodes());
        pets.Observe(new DamageEvent(5,1,8,1,100,HitFlags.None));Assert.Equal(new[]{2400707,2400455},pets.AttackedNpcCodes());
    }
    [Fact] public void LateNpcIdentityResolvesAnOwnAttackAndRepeatedHitsDoNotRedraw()
    {
        var pets=new PetCollection();pets.Observe(Own());pets.Observe(new DamageEvent(2,1,7,1,100,HitFlags.None));
        Assert.Empty(pets.AttackedNpcCodes());pets.Observe(new NpcSeenEvent(3,7,2400455));Assert.Equal(2400455,pets.AttackedNpcCode);
        Assert.Equal(new[]{2400455},pets.AttackedNpcCodes());var revision=pets.Revision;
        pets.Observe(new DamageEvent(4,1,7,1,100,HitFlags.None));Assert.Equal(revision,pets.Revision);
    }
    [Fact] public void KillAndLoadingKeepAttackedPetProgressButCharacterChangeClearsIt()
    {
        var pets=new PetCollection();pets.Observe(Own());pets.Observe(PetPackets.Collection(1,Baseline)!);
        pets.Observe(new NpcSeenEvent(2,7,2400455));pets.Observe(new DamageEvent(3,1,7,1,100,HitFlags.None));
        pets.Observe(new DeathEvent(4,7));pets.Observe(PetPackets.Souls(5,Delta)!);
        Assert.Equal(new[]{2400455},pets.AttackedNpcCodes());Assert.Equal(11,pets.Snapshot().Single().Souls);
        pets.Observe(new ZoneChangedEvent(6,0,"",false));Assert.Equal(new[]{2400455},pets.AttackedNpcCodes());
        pets.Observe(Own("Other"));Assert.Empty(pets.AttackedNpcCodes());
    }
    [Fact] public void AttackedSpeciesAreBoundedAndDeduplicated()
    {
        var pets=new PetCollection();pets.Observe(Own());
        for(uint i=2;i<40;i++){pets.Observe(new NpcSeenEvent(i,i,(int)i+2400000));pets.Observe(new DamageEvent(i,1,i,1,100,HitFlags.None));}
        Assert.Equal(16,pets.AttackedNpcCodes().Length);Assert.Equal(2400039,pets.AttackedNpcCodes()[0]);
        pets.Observe(new DamageEvent(41,1,39,1,100,HitFlags.None));Assert.Equal(16,pets.AttackedNpcCodes().Length);
    }
    [Fact] public void UniqueSoulLootLearnsSourceWithoutAZone()
    {
        var pets=new PetCollection();pets.Observe(Own());pets.Observe(PetPackets.Collection(1,Baseline)!);
        pets.Observe(new NpcSeenEvent(2,7,2400455));pets.Observe(new PetSoulDropEvent(3,7,534660209,1));pets.Observe(PetPackets.Souls(4,Delta)!);
        Assert.Equal(2400455,pets.Farms(1193).Single().NpcCode);Assert.Equal("",pets.Farms(1193).Single().Zone);
    }
    [Fact] public void CachedProgressIsScopedToCharacterAndNeverReplacesFreshCounters()
    {
        var original=new PetCollection();original.Observe(Own());original.Observe(PetPackets.Collection(10,Baseline)!);
        var saved=original.SavedProgress()!;var restored=new PetCollection();restored.Observe(Own());
        Assert.False(restored.RestoreProgress(saved with {Name="Other"}));Assert.False(restored.RestoreProgress(saved with {ServerId=2201}));
        Assert.True(restored.RestoreProgress(saved));Assert.True(restored.FromCache);Assert.Equal(10,restored.Snapshot().Single().Souls);
        restored.Observe(PetPackets.Souls(11,Delta)!);Assert.Equal(11,restored.Snapshot().Single().Souls);Assert.True(restored.FromCache);
        restored.Observe(PetPackets.Collection(12,Baseline)!);Assert.False(restored.FromCache);Assert.Equal(10,restored.Snapshot().Single().Souls);
        Assert.False(restored.RestoreProgress(saved with {Pets=[new(1193,1,1)]}));
    }
    [Fact] public void CorruptCacheIsRejectedAndEqualTimestampDeltasStillAdvanceProgressVersion()
    {
        var pets=new PetCollection();pets.Observe(Own());
        Assert.False(pets.RestoreProgress(new("Own",2305,1,[new(1193,-1,0)])));
        Assert.False(pets.RestoreProgress(new("Own",2305,1,[new(1193,1,0),new(1193,1,0)])));
        pets.Observe(PetPackets.Collection(10,Baseline)!);var version=pets.ProgressVersion;
        pets.Observe(PetPackets.Souls(10,Delta)!);pets.Observe(PetPackets.Souls(10,Delta)!);
        Assert.Equal(version+2,pets.ProgressVersion);Assert.Equal(12,pets.Snapshot().Single().Souls);
    }
    [Fact] public void MovementDecodingIsOptInAndRejectsInvalidCoordinates()
    {
        var events=new List<GameEvent>();var parser=new PacketParser(GameData.Empty,events.Add);
        var body=Wire.FromHex("01040000004000004040000080400000000001");
        parser.Handle(0x1c37,body);Assert.Empty(events);parser.DecodePetPositions=true;parser.Handle(0x1c37,body);
        var pose=Assert.IsType<ActorPositionEvent>(Assert.Single(events));Assert.Equal(2,pose.X);Assert.Equal(3,pose.Y);
        events.Clear();body[2]=0;body[3]=0;body[4]=192;body[5]=127;parser.Handle(0x1c37,body);Assert.Empty(events);
    }
}
