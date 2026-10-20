using System.Buffers.Binary;
using AionMeter.Core.Game;
using AionMeter.Core.Events;
using AionMeter.Core.Protocol;
namespace AionMeter.Tests;
public class HiddenCubeKeyTests
{
    private static byte[] Update(ulong instance,uint amount,byte operation=1)
    {
        var body=new byte[41];body[0]=1;body[1]=operation;BinaryPrimitives.WriteUInt64LittleEndian(body.AsSpan(3),instance);
        BinaryPrimitives.WriteInt32LittleEndian(body.AsSpan(11),HiddenCubeKeyPackets.BoundKeyId);BinaryPrimitives.WriteUInt64LittleEndian(body.AsSpan(15),amount);body[23]=1;body[24]=73;return body;
    }
    private static byte[] Snapshot(params (ulong Id,uint Amount)[] stacks)=>new byte[]{0x10,2,(byte)stacks.Length}.Concat(stacks.SelectMany(s=>Update(s.Id,s.Amount).Skip(2))).ToArray();
    private static SelfIdentifiedEvent Own(string name="One")=>new(1,1,name,2305,GameClass.Cleric);
    [Fact] public void ConfirmedLoginStackReadsTwentyAndKeepsBoundKeyScope()
    {
        var events=new List<GameEvent>();new PacketParser(GameData.Empty,events.Add).Handle(0x1156,Snapshot((0x0002d90000075459,20)));
        var stock=Assert.IsType<CubeKeyInventoryEvent>(Assert.Single(events));var state=new ResourceBalances();state.Observe(Own());state.Observe(stock);
        Assert.Equal(20UL,state.ReadCubeKeys().Count);Assert.False(state.ReadCubeKeys().Cached);
    }
    [Fact] public void RepeatedUpdatesReplaceTheirOwnStackAndNeverSubtractTwice()
    {
        var state=new ResourceBalances();state.Observe(Own());state.Observe(HiddenCubeKeyPackets.Snapshot(10,Snapshot((1,20),(2,8)))!);
        var update=HiddenCubeKeyPackets.Update(20,Update(1,19))!;state.Observe(update);state.Observe(update);Assert.Equal(27UL,state.ReadCubeKeys().Count);
        state.Observe(HiddenCubeKeyPackets.Update(30,Update(2,0,2))!);Assert.Equal(19UL,state.ReadCubeKeys().Count);
        state.Observe(HiddenCubeKeyPackets.Update(31,Update(1,0,2))!);Assert.Equal(0UL,state.ReadCubeKeys().Count);
    }
    [Fact] public void PartialUpdateOrMissingTemplateCannotInventAnInitialTotalOrZero()
    {
        var state=new ResourceBalances();state.Observe(Own());state.Observe(HiddenCubeKeyPackets.Update(2,Update(1,9))!);Assert.Null(state.ReadCubeKeys().Count);
        var other=Snapshot((1,20));BinaryPrimitives.WriteInt32LittleEndian(other.AsSpan(12),530116010);Assert.Null(HiddenCubeKeyPackets.Snapshot(3,other));
        Assert.Null(HiddenCubeKeyPackets.Snapshot(3,new byte[100]));
    }
    [Fact] public void CachedKeysRequireMatchingCharacterAndOnlyFreshStacksLoseTheirMarker()
    {
        var original=new ResourceBalances();original.Observe(Own());original.Observe(HiddenCubeKeyPackets.Snapshot(10,Snapshot((1,20),(2,8)))!);var saved=original.SavedProgress()!;
        var state=new ResourceBalances();Assert.False(state.RestoreProgress(saved));state.Observe(Own());Assert.True(state.RestoreProgress(saved));Assert.True(state.ReadCubeKeys().Cached);
        state.Observe(HiddenCubeKeyPackets.Update(20,Update(1,19))!);Assert.Equal(27UL,state.ReadCubeKeys().Count);Assert.True(state.ReadCubeKeys().Cached);
        state.Observe(HiddenCubeKeyPackets.Update(21,Update(2,8))!);Assert.False(state.ReadCubeKeys().Cached);
        state.MarkCached();Assert.True(state.ReadCubeKeys().Cached);state.Observe(Own("Other"));Assert.Null(state.ReadCubeKeys().Count);
    }
    [Fact] public void CacheRestoreCannotOverwriteALiveUpdateThatArrivedBeforeIdentity()
    {
        var original=new ResourceBalances();original.Observe(Own());original.Observe(HiddenCubeKeyPackets.Snapshot(10,Snapshot((1,20)))!);
        var state=new ResourceBalances();state.Observe(new ResourceSnapshotStartedEvent(15));state.Observe(HiddenCubeKeyPackets.Update(20,Update(1,19))!);state.Observe(Own());
        Assert.True(state.RestoreProgress(original.SavedProgress()!));Assert.Equal(19UL,state.ReadCubeKeys().Count);Assert.False(state.ReadCubeKeys().Cached);Assert.Equal(20,state.ReadCubeKeys().ConfirmedAt);
    }
    [Fact] public void TruncatedForeignAndConflictingInventoryRecordsAreRejected()
    {
        var body=Update(1,20);for(var length=0;length<body.Length;length++)Assert.Null(HiddenCubeKeyPackets.Update(1,body.AsSpan(0,length)));
        Assert.Null(HiddenCubeKeyPackets.Update(1,Update(1,20,2)));Assert.Null(HiddenCubeKeyPackets.Snapshot(1,Snapshot((1,20),(1,21))));
        Assert.Null(HiddenCubeKeyPackets.Update(1,Update(0,20)));Assert.Null(HiddenCubeKeyPackets.Update(1,Update(1,101)));
        var foreign=Snapshot((1,20));foreign[1]=3;Assert.Null(HiddenCubeKeyPackets.Snapshot(1,foreign));
    }
}
