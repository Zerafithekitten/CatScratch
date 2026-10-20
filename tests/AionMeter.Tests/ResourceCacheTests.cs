using AionMeter.Core.Events;
using AionMeter.Core.Game;
namespace AionMeter.Tests;
public class ResourceCacheTests
{
    private static SelfIdentifiedEvent Own(string name="One",int server=2305)=>new(1,1,name,server,GameClass.Cleric);
    private static ResourceProgress Baseline()=>new("One",2305,100,new(100,160,270),new(){{930100024,3000},{930100003,4600}},new(){{1,new(16,3,21)},{4,new(14,0,14)},{101,new(0,18,14)}});
    [Fact] public void RestoreRequiresExactFreshCharacterAndServer()
    {
        var state=new ResourceBalances();Assert.False(state.RestoreProgress(Baseline()));
        state.Observe(Own(server:0));Assert.False(state.RestoreProgress(Baseline()));
        state.Observe(Own("Other"));Assert.False(state.RestoreProgress(Baseline()));
        state.Observe(Own(server:2306));Assert.False(state.RestoreProgress(Baseline()));
        state.Observe(Own());Assert.True(state.RestoreProgress(Baseline()));
        Assert.Equal(160u,state.Read().Energy!.Available);Assert.Equal(3000ul,state.Read().Balances[930100024]);
        var markers=state.CacheMarkers();Assert.True(markers.Energy);Assert.Equal(2,markers.Currencies.Count);Assert.Equal(3,markers.Entries.Count);
    }
    [Fact] public void FreshValuesAlwaysWinAndOnlyTheirApproximateMarkerIsRemoved()
    {
        var state=new ResourceBalances();state.Observe(Own());state.RestoreProgress(Baseline());
        state.Observe(new ResourceBalanceSnapshot(200,new Dictionary<int,ulong>{{930100024,1500}}));
        state.Observe(new DungeonEntriesEvent(201,new Dictionary<uint,DungeonEntryValue>{{101,new(0,17,null)}}));
        state.Observe(new OdyleEnergyEvent(202,175,270));
        state.RestoreProgress(Baseline());
        Assert.Equal(1500ul,state.Read().Balances[930100024]);Assert.Equal(175u,state.Read().Energy!.Available);
        Assert.Equal(new DungeonEntryValue(0,17,14),state.ReadEntries()[101]);
        Assert.False(state.CacheMarkers().Energy);Assert.DoesNotContain(930100024,state.CacheMarkers().Currencies);
        Assert.DoesNotContain(101u,state.CacheMarkers().Entries);Assert.Contains(4u,state.CacheMarkers().Entries);
    }
    [Fact] public void RepeatedConsumptionDoesNotDeductTwiceAndZoneAloneNeverSpends()
    {
        var state=new ResourceBalances();state.Observe(Own());state.RestoreProgress(Baseline());
        state.Observe(new ZoneChangedEvent(200,600001,"Dungeon",true));Assert.Equal(16u,state.ReadEntries()[1].Available);
        var update=new DungeonEntriesEvent(201,new Dictionary<uint,DungeonEntryValue>{{1,new(15,3,null)}});
        state.Observe(update);state.Observe(update);state.Observe(Own());Assert.Equal(15u,state.ReadEntries()[1].Available);
    }
    [Fact] public void CharacterChangeAndResetNeverShowAnotherCharactersCache()
    {
        var state=new ResourceBalances();state.Observe(Own());state.RestoreProgress(Baseline());
        state.Observe(Own("Other"));Assert.Empty(state.Read().Balances);Assert.Empty(state.ReadEntries());Assert.Null(state.Read().Energy);
        state.Observe(Own());state.RestoreProgress(Baseline());state.Reset();Assert.Null(state.SavedProgress());Assert.Empty(state.CacheMarkers().Entries);
    }
    [Fact] public void MarkCachedRetainsDataDuringCaptureRestartAndEqualLiveValuesConfirmIt()
    {
        var state=new ResourceBalances();state.Observe(Own());state.Observe(new ResourceBalanceSnapshot(2,new Dictionary<int,ulong>{{930100024,0}}));
        state.MarkCached();Assert.Equal(0ul,state.Read().Balances[930100024]);Assert.Contains(930100024,state.CacheMarkers().Currencies);
        state.Observe(new ResourceBalanceSnapshot(3,new Dictionary<int,ulong>{{930100024,0}}));Assert.Empty(state.CacheMarkers().Currencies);
    }
    [Fact] public void AmbiguousLiveRecordsCannotBeReplacedWithAnOldCachedBalance()
    {
        var state=new ResourceBalances();state.Observe(Own());state.RestoreProgress(Baseline());
        state.Observe(new ResourceBalanceSnapshot(200,new Dictionary<int,ulong>(),new HashSet<int>{930100024}));
        state.RestoreProgress(Baseline());Assert.False(state.Read().Balances.ContainsKey(930100024));
    }
    [Fact] public void LoginDataBeforeNewCharacterIdentityIsAppliedToTheNewCharacter()
    {
        var state=new ResourceBalances();state.Observe(Own());state.RestoreProgress(Baseline());
        state.Observe(new ZoneChangedEvent(200,1110,"New map",false));
        state.Observe(new ResourceBalanceSnapshot(201,new Dictionary<int,ulong>{{930100003,100}}));
        state.Observe(new DungeonEntriesEvent(202,new Dictionary<uint,DungeonEntryValue>{{1,new(10,0,21)}}));
        Assert.Equal(4600ul,state.Read().Balances[930100003]);Assert.True(state.AwaitingIdentity);
        state.Observe(Own("Other"));
        Assert.Equal(100ul,state.Read().Balances[930100003]);Assert.Equal(10u,state.ReadEntries()[1].Available);
        Assert.False(state.AwaitingIdentity);Assert.False(state.RestoreProgress(Baseline()));
    }
    [Fact] public void OrdinaryTeleportDoesNotHoldUpConfirmedResourceUpdates()
    {
        var state=new ResourceBalances();state.Observe(Own());state.RestoreProgress(Baseline());
        state.Observe(new ZoneChangedEvent(200,1110,"Teleport",false,true));
        state.Observe(new ResourceBalanceSnapshot(201,new Dictionary<int,ulong>{{930100024,100}}));
        Assert.False(state.AwaitingIdentity);Assert.Equal(100ul,state.Read().Balances[930100024]);
    }
    [Fact] public void ConfirmationTimesArePerValueAndSurviveCacheWithoutInventingLegacyTimes()
    {
        var state=new ResourceBalances();state.Observe(Own());
        state.Observe(new OdyleEnergyEvent(100,160,270));
        state.Observe(new ResourceBalanceSnapshot(200,new Dictionary<int,ulong>{{930100024,3000}}));
        state.Observe(new DungeonEntriesEvent(300,new Dictionary<uint,DungeonEntryValue>{{1,new(16,3,21)}}));
        Assert.Equal(100,state.ConfirmedAt("odyle"));Assert.Equal(200,state.ConfirmedAt("currency:930100024"));Assert.Equal(300,state.ConfirmedAt("dungeon:1"));
        var restored=new ResourceBalances();restored.Observe(Own());restored.RestoreProgress(state.SavedProgress()!);
        Assert.Equal(100,restored.ConfirmedAt("odyle"));Assert.Equal(200,restored.ConfirmedAt("currency:930100024"));
        restored.Observe(new ResourceBalanceSnapshot(400,new Dictionary<int,ulong>{{930100024,1500}}));
        Assert.Equal(400,restored.ConfirmedAt("currency:930100024"));Assert.Equal(100,restored.ConfirmedAt("odyle"));
        var legacy=new ResourceBalances();legacy.Observe(Own());legacy.RestoreProgress(Baseline());Assert.Equal(0,legacy.ConfirmedAt("currency:930100024"));
    }

    [Fact] public void QuickSwitchQueuesLoginValuesBeforeSameMapAndKeepsAccentNamesDistinct()
    {
        var state=new ResourceBalances();var identity=new LiveIdentity();
        void Send(GameEvent e){state.Observe(identity.Observe(e)??e);}
        Send(new SelfIdentifiedEvent(1,99,"Zerafí",2305,GameClass.Cleric));
        Send(new OdyleEnergyEvent(2,60,525));Send(new ZoneChangedEvent(3,1110,"Same map",false));
        Send(new SelfIdentifiedEvent(4,99,"Zerafí",2305,GameClass.Cleric));
        Send(new ResourceSnapshotStartedEvent(5));
        Assert.Null(identity.Current);Assert.Null(identity.KnownCharacter);
        Send(new OdyleEnergyEvent(6,175,270));
        Send(new DungeonEntriesEvent(6,new Dictionary<uint,DungeonEntryValue>{{4,new(14,0,14)}}));
        Send(new ResourceBalanceSnapshot(6,new Dictionary<int,ulong>{{930100003,4600}}));
        Send(new ZoneChangedEvent(7,1110,"Same map",false));
        Assert.True(state.AwaitingIdentity);Assert.Equal(60u,state.Read().Energy!.Available);
        // An unrelated sighting of the previous character must not authorize the new login values.
        Send(new PlayerSeenEvent(8,99,"Zerafí",2305,GameClass.Cleric));Assert.Null(identity.Current);
        Send(new SelfIdentifiedEvent(9,102,"Zerafi",2305,GameClass.Elementalist));
        Assert.False(state.AwaitingIdentity);Assert.Equal("Zerafi",state.SavedProgress()!.Name);
        Assert.Equal(175u,state.Read().Energy!.Available);Assert.Equal(270u,state.Read().Energy!.Supplementary);
        Assert.Equal(4600ul,state.Read().Balances[930100003]);Assert.Equal(14u,state.ReadEntries()[4].Available);
    }
    [Fact] public void PendingLoginValuesSurviveTheLaterDifferentMapAnnouncement()
    {
        var state=new ResourceBalances();state.Observe(Own());state.RestoreProgress(Baseline());
        state.Observe(new ResourceSnapshotStartedEvent(200));state.Observe(new OdyleEnergyEvent(201,175,270));
        state.Observe(new ZoneChangedEvent(202,2220,"Another map",false));state.Observe(Own("Other"));
        Assert.Equal(175u,state.Read().Energy!.Available);Assert.Equal("Other",state.SavedProgress()!.Name);
    }
    [Fact] public void ParserAnnouncesFullLoginTableBeforeItsValuesButNotLiveUpdates()
    {
        var events=new List<GameEvent>();var parser=new AionMeter.Core.Protocol.PacketParser(GameData.Empty,events.Add);
        parser.Handle(0x0B61,Convert.FromHexString("050C01000000100304040000000E0865000000120C01879303A0018E02040687930315"));
        Assert.Collection(events,e=>Assert.IsType<ResourceSnapshotStartedEvent>(e),e=>Assert.IsType<OdyleEnergyEvent>(e),e=>Assert.IsType<DungeonEntriesEvent>(e));
        events.Clear();parser.Handle(0x0C61,Convert.FromHexString("000C01879303A0018E0202"));
        Assert.IsType<OdyleEnergyEvent>(Assert.Single(events));
    }
}
