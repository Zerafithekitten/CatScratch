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
}
