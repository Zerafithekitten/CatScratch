using AionMeter.Core.Game;
using AionMeter.Core.Events;
using AionMeter.Core.Protocol;
namespace AionMeter.Tests;
public class DungeonEntryTests
{
    [Theory]
    [InlineData("000C01000000110303",1,17,3)]
    [InlineData("0008650000001203",101,0,18)]
    [InlineData("0008040000000303",4,0,3)]
    public void ConfirmedConsumptionUpdatesAreAbsolute(string hex,uint id,uint regular,uint extra)
    {
        var body=Convert.FromHexString(hex);var events=new List<GameEvent>();
        new PacketParser(GameData.Empty,events.Add).Handle(0x0C61,body);
        var e=Assert.IsType<DungeonEntriesEvent>(Assert.Single(events));
        Assert.Equal(new DungeonEntryValue(regular,extra,null),e.Values[id]);
        var store=new ResourceBalances();store.Observe(new DungeonEntriesEvent(1,new Dictionary<uint,DungeonEntryValue>{{id,new(regular+1,extra+1,21)}}));
        store.Observe(e);store.Observe(e);
        Assert.Equal(new DungeonEntryValue(regular,extra,21),store.ReadEntries()[id]);
        for(var n=0;n<body.Length;n++)Assert.Null(DungeonEntryPackets.Update(1,body.AsSpan(0,n)));
        body[^1]=2;Assert.Null(DungeonEntryPackets.Update(1,body));
    }
    [Fact] public void SnapshotKeepsAllThreeIndependentCountsAndServerCapacities()
    {
        var body=Convert.FromHexString("060C01000000120308040000000408650000001304068793031504048793030E04658793030E");
        var result=DungeonEntryPackets.Snapshot(1,body)!;
        Assert.Equal(new DungeonEntryValue(18,3,21),result.Values[1]);
        Assert.Equal(new DungeonEntryValue(0,4,14),result.Values[4]);
        Assert.Equal(new DungeonEntryValue(0,19,14),result.Values[101]);
        for(var n=0;n<body.Length;n++)Assert.Null(DungeonEntryPackets.Snapshot(1,body.AsSpan(0,n)));
        Assert.Null(DungeonEntryPackets.Snapshot(1,body.Concat(new byte[]{0}).ToArray()));
    }
    [Fact] public void MissingCapacityIsUnknownAndCharacterChangeClearsEntries()
    {
        var store=new ResourceBalances();store.Observe(new SelfIdentifiedEvent(1,1,"One",2305,GameClass.Cleric));
        store.Observe(DungeonEntryPackets.Update(2,Convert.FromHexString("0008040000000303"))!);
        Assert.Null(store.ReadEntries()[4].Capacity);
        store.Observe(new SelfIdentifiedEvent(3,2,"",0,GameClass.Unknown));Assert.Single(store.ReadEntries());
        store.Observe(new SelfIdentifiedEvent(4,2,"Two",2305,GameClass.Cleric));Assert.Empty(store.ReadEntries());
        Assert.Null(DungeonEntryPackets.Update(5,Convert.FromHexString("000C018793030FE50302")));
    }
    [Fact] public void MultiByteOdyleInSharedSnapshotDoesNotInvalidateDungeonCounts()
    {
        var body=Convert.FromHexString("050C01000000100304040000000E0865000000120C01879303A0018E02040687930315");
        var result=DungeonEntryPackets.Snapshot(1,body)!;
        Assert.Equal(new DungeonEntryValue(16,3,21),result.Values[1]);
        Assert.Equal(new DungeonEntryValue(14,0,null),result.Values[4]);
        Assert.Equal(new DungeonEntryValue(0,18,null),result.Values[101]);
    }
    [Fact] public void ShugoItemGrantReplacesApproximateCountsAndConfirmsOnlyShugo()
    {
        // Server packet from the 2026-10-20 Shugo item-use capture.
        var body=Convert.FromHexString("010C0100000012040101000000");
        var events=new List<GameEvent>();
        var parser=new PacketParser(GameData.Empty,events.Add){TimeMs=200};
        parser.Handle(0x0C61,body);
        var update=Assert.IsType<DungeonEntriesEvent>(Assert.Single(events));
        Assert.Equal(new DungeonEntryValue(18,4,null),update.Values[1]);
        var store=new ResourceBalances();
        store.Observe(new SelfIdentifiedEvent(1,1,"One",2305,GameClass.Cleric));
        store.RestoreProgress(new ResourceProgress("One",2305,100,null,new(),new()
            {{1,new(18,3,21)},{4,new(0,3,14)}}));
        Assert.Contains(1u,store.CacheMarkers().Entries);
        store.Observe(update);store.Observe(update);
        Assert.Equal(new DungeonEntryValue(18,4,21),store.ReadEntries()[1]);
        Assert.DoesNotContain(1u,store.CacheMarkers().Entries);
        Assert.Contains(4u,store.CacheMarkers().Entries);
        Assert.Equal(200,store.ConfirmedAt("dungeon:1"));
        for(var n=0;n<body.Length;n++)Assert.Null(DungeonEntryPackets.Update(1,body.AsSpan(0,n)));
        Assert.Null(DungeonEntryPackets.Update(1,body.Concat(new byte[]{0}).ToArray()));
        body[^5]=2;Assert.Null(DungeonEntryPackets.Update(1,body));
        body[^5]=1;body[^4]=0;Assert.Null(DungeonEntryPackets.Update(1,body));
        body[^4]=1;body[0]=2;Assert.Null(DungeonEntryPackets.Update(1,body));
    }
}
