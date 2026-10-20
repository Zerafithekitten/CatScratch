using AionMeter.Core.Game;
using AionMeter.Core.Events;
namespace AionMeter.Tests;
public class ResourceBalanceTests
{
    private static byte[] Inventory(params (int Id,ulong Count)[] values)
    {
        var body=new byte[4+values.Length*36];var at=4;
        foreach(var v in values){BitConverter.GetBytes(v.Id).CopyTo(body,at);BitConverter.GetBytes(v.Count).CopyTo(body,at+4);body[at+12]=1;at+=36;}
        return body;
    }
    [Fact] public void SupportedBalancesAreReadIndividually()
    {
        var snapshot=ResourceInventoryPackets.Snapshot(1,Inventory((930100003,3188),(930100011,1670),(930100024,1000),(930100017,160)))!;
        Assert.Equal(4,snapshot.Values.Count);Assert.Equal(3188ul,snapshot.Values[930100003]);Assert.Equal(160ul,snapshot.Values[930100017]);
    }
    [Fact] public void AmbiguousContainersAreNotSummed()
    {
        var snapshot=ResourceInventoryPackets.Snapshot(1,Inventory((930100003,100),(930100003,300),(930100017,160)))!;
        Assert.False(snapshot.Values.ContainsKey(930100003));Assert.Equal(160ul,snapshot.Values[930100017]);
    }
    [Fact] public void MissingBalancesAreUnknownNotZero()
    {
        var snapshot=ResourceInventoryPackets.Snapshot(1,Inventory((930100017,0)))!;
        Assert.Equal(0ul,snapshot.Values[930100017]);Assert.False(snapshot.Values.ContainsKey(930100003));
        Assert.Null(ResourceInventoryPackets.Snapshot(1,new byte[20]));
    }
    [Fact] public void StateKeepsSeparateEnergyValuesAndClearsOnCharacterSwitch()
    {
        var state=new ResourceBalances();state.Observe(new SelfIdentifiedEvent(1,1,"One",1,GameClass.Cleric));
        state.Observe(new OdyleEnergyEvent(2,15,495));state.Observe(new ResourceBalanceSnapshot(2,new Dictionary<int,ulong>{{930100003,3188}}));
        state.Observe(new ZoneChangedEvent(3,1110,"Map",false));Assert.Equal(495u,state.Read().Energy!.Supplementary);
        state.Observe(new SelfIdentifiedEvent(4,1,"Two",1,GameClass.Cleric));Assert.Null(state.Read().Energy);Assert.Empty(state.Read().Balances);
    }
    [Fact] public void ResetDiscardsValuesAndSnapshotsAreIndependentCopies()
    {
        var state=new ResourceBalances();state.Observe(new ResourceBalanceSnapshot(1,new Dictionary<int,ulong>{{930100003,10}}));
        var first=state.Read();state.Reset();Assert.Equal(10ul,first.Balances[930100003]);Assert.Empty(state.Read().Balances);
    }
    [Fact] public void PlaceholderAndServerRefinementDoNotEraseReceivedValues()
    {
        var state=new ResourceBalances();state.Observe(new SelfIdentifiedEvent(1,1,"One",0,GameClass.Cleric));
        state.Observe(new OdyleEnergyEvent(2,15,495));state.Observe(new ResourceBalanceSnapshot(2,new Dictionary<int,ulong>{{930100003,3188}}));
        state.Observe(new SelfIdentifiedEvent(3,2,"",0,GameClass.Unknown));
        state.Observe(new SelfIdentifiedEvent(4,2,"One",1,GameClass.Cleric));
        Assert.Equal(495u,state.Read().Energy!.Supplementary);Assert.Equal(3188ul,state.Read().Balances[930100003]);
        state.Observe(new SelfIdentifiedEvent(5,2,"One",0,GameClass.Cleric));
        state.Observe(new SelfIdentifiedEvent(6,2,"One",2,GameClass.Cleric));
        Assert.Null(state.Read().Energy);Assert.Empty(state.Read().Balances);
    }
    [Fact] public void PartialAndEmptyInventoryMessagesPreserveOtherResources()
    {
        var state=new ResourceBalances();state.Observe(new ResourceBalanceSnapshot(1,new Dictionary<int,ulong>{{930100003,3188},{930100017,160}}));
        state.Observe(new ResourceBalanceSnapshot(2,new Dictionary<int,ulong>{{930100017,150}}));
        state.Observe(new ResourceBalanceSnapshot(3,new Dictionary<int,ulong>()));
        Assert.Equal(3188ul,state.Read().Balances[930100003]);Assert.Equal(150ul,state.Read().Balances[930100017]);
        Assert.Null(ResourceInventoryPackets.Snapshot(4,new byte[40]));
    }
    [Fact] public void AmbiguousInventoryEntriesInvalidateOnlyTheAffectedResource()
    {
        var state=new ResourceBalances();state.Observe(new ResourceBalanceSnapshot(1,new Dictionary<int,ulong>{{930100003,3188},{930100017,160}}));
        state.Observe(ResourceInventoryPackets.Snapshot(2,Inventory((930100003,100),(930100003,200)))!);
        Assert.False(state.Read().Balances.ContainsKey(930100003));Assert.Equal(160ul,state.Read().Balances[930100017]);
    }
    [Fact] public void CenturyrootSpendingUsesAbsoluteBalanceAndPreservesOtherResources()
    {
        var state=new ResourceBalances();state.Observe(new ResourceBalanceSnapshot(1,new Dictionary<int,ulong>{{930100011,170},{930100003,7188}}));
        // Sanitized two-item purchase response; no captured character, actor or location fields.
        var body=Inventory((930100011,150),(531062167,1));body[0]=2;
        var parser=new AionMeter.Core.Protocol.PacketParser(GameData.Empty,state.Observe){TimeMs=2};
        parser.Handle(0x1B56,body);parser.Handle(0x1B56,body);
        Assert.Equal(150ul,state.Read().Balances[930100011]);Assert.Equal(7188ul,state.Read().Balances[930100003]);
        var unverified=Inventory((930100024,100));unverified[0]=1;Assert.Null(ResourceInventoryPackets.Update(3,unverified));
        Assert.Null(ResourceInventoryPackets.Update(3,body.AsSpan(0,20)));
    }
    [Fact] public void AbyssGainUpdatesEvenWithoutLoginSnapshotAndDoesNotAccumulateDuplicates()
    {
        // Sanitized single-item record shape from the gain response, with arbitrary item-instance fields.
        var body=new byte[41];body[0]=1;body[1]=1;
        BitConverter.GetBytes(930100003).CopyTo(body,11);BitConverter.GetBytes(8207ul).CopyTo(body,15);body[23]=1;body[24]=1;
        var state=new ResourceBalances();var parser=new AionMeter.Core.Protocol.PacketParser(GameData.Empty,state.Observe){TimeMs=1};
        parser.Handle(0x1B56,body);parser.Handle(0x1B56,body);
        Assert.Equal(8207ul,state.Read().Balances[930100003]);Assert.Single(state.Read().Balances);
        state.Observe(new ResourceBalanceSnapshot(2,new Dictionary<int,ulong>{{930100003,8188},{930100011,150}}));
        parser.Handle(0x1B56,body);Assert.Equal(8207ul,state.Read().Balances[930100003]);Assert.Equal(150ul,state.Read().Balances[930100011]);
        var unrelated=new ResourceBalances();var otherParser=new AionMeter.Core.Protocol.PacketParser(GameData.Empty,unrelated.Observe);
        otherParser.Handle(0x118D,body);Assert.Empty(unrelated.Read().Balances);
    }
    [Fact] public void FragmentUpdateUsesTheReceivedAbsoluteBalance()
    {
        var body=new byte[41];body[0]=1;body[1]=1;
        BitConverter.GetBytes(930100017).CopyTo(body,11);BitConverter.GetBytes(1165ul).CopyTo(body,15);body[23]=1;body[24]=6;
        var state=new ResourceBalances();state.Observe(new ResourceBalanceSnapshot(1,new Dictionary<int,ulong>{{930100017,160},{930100003,8207},{930100011,150}}));
        var parser=new AionMeter.Core.Protocol.PacketParser(GameData.Empty,state.Observe){TimeMs=2};
        parser.Handle(0x1B56,body);parser.Handle(0x1B56,body);
        Assert.Equal(1165ul,state.Read().Balances[930100017]);Assert.Equal(8207ul,state.Read().Balances[930100003]);Assert.Equal(150ul,state.Read().Balances[930100011]);
        body[23]=255;Assert.Null(ResourceInventoryPackets.Update(3,body));
    }
    [Fact] public void OathCoinsUseAbsoluteLiveBalanceAndPreserveOtherCurrencies()
    {
        var body=new byte[80];body[0]=2;body[1]=1;
        BitConverter.GetBytes(930100019).CopyTo(body,11);BitConverter.GetBytes(3750ul).CopyTo(body,15);body[23]=1;body[24]=9;
        var state=new ResourceBalances();state.Observe(new ResourceBalanceSnapshot(1,new Dictionary<int,ulong>{{930100019,27750},{930100003,8207}}));
        var parser=new AionMeter.Core.Protocol.PacketParser(GameData.Empty,state.Observe){TimeMs=2};
        parser.Handle(0x1B56,body);parser.Handle(0x1B56,body);
        Assert.Equal(3750ul,state.Read().Balances[930100019]);Assert.Equal(8207ul,state.Read().Balances[930100003]);
        Assert.Equal(3750ul,ResourceInventoryPackets.Snapshot(3,Inventory((930100019,3750)))!.Values[930100019]);
        body[23]=255;Assert.Null(ResourceInventoryPackets.Update(4,body));
    }
}
