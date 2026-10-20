using AionMeter.Core.Game;
using AionMeter.Core.Protocol;
using AionMeter.Core.Events;
namespace AionMeter.Tests;

public class OdyleEnergyTests
{
    private static byte[] Before=>Convert.FromHexString("010801879303E503");
    private static byte[] After=>Convert.FromHexString("010C018793030FE503");
    private static byte[] Update=>Convert.FromHexString("000C018793030FE50302");
    [Fact] public void RegenerationChangesAvailableEnergyAndPreservesSupplementaryEnergy()
    {
        Assert.Equal(new OdyleEnergyEvent(1,0,485),OdyleEnergyPackets.Snapshot(1,Before));
        Assert.Equal(new OdyleEnergyEvent(2,15,485),OdyleEnergyPackets.Update(2,Update));
        Assert.Equal(new OdyleEnergyEvent(3,15,485),OdyleEnergyPackets.Snapshot(3,After));
    }
    [Fact] public void ItemUseUpdatesSupplementaryEnergyWithoutAddingToAvailableEnergy()
    {
        var body=Convert.FromHexString("010C018793030FEF03010A000000");
        Assert.Equal(new OdyleEnergyEvent(4,15,495),OdyleEnergyPackets.Update(4,body));
        for(var i=0;i<body.Length;i++)Assert.Null(OdyleEnergyPackets.Update(4,body.AsSpan(0,i)));
        var events=new List<GameEvent>();var parser=new PacketParser(GameData.Empty,events.Add){TimeMs=4};
        parser.Handle(0x0C61,body);
        Assert.Equal(new OdyleEnergyEvent(4,15,495),Assert.Single(events));
        // A malformed source/reason must not replace the current balance.
        body[^5]=2;Assert.Null(OdyleEnergyPackets.Update(4,body));
    }
    [Fact] public void OtherPropertyUpdatesAreNotOdyle()
        =>Assert.Null(OdyleEnergyPackets.Update(1,Convert.FromHexString("00040A0000000E02")));
    [Fact] public void TruncationAndUnknownLayoutsAreRejected()
    {
        for(var i=0;i<Before.Length;i++)Assert.Null(OdyleEnergyPackets.Snapshot(1,Before.AsSpan(0,i)));
        for(var i=0;i<Update.Length;i++)Assert.Null(OdyleEnergyPackets.Update(1,Update.AsSpan(0,i)));
        Assert.Null(OdyleEnergyPackets.Snapshot(1,After.Concat(new byte[]{0}).ToArray()));
        var invalid=Update;invalid[^1]=3;Assert.Null(OdyleEnergyPackets.Update(1,invalid));
    }
    [Fact] public void LaterMalformedFieldsCannotProduceAPartialSnapshot()
    {
        var body=After.Concat(new byte[]{255}).ToArray();body[0]=2;
        Assert.Null(OdyleEnergyPackets.Snapshot(1,body));
    }
    [Fact] public void ActualParserEmitsSnapshotAndLiveUpdate()
    {
        var events=new List<GameEvent>();var parser=new PacketParser(GameData.Empty,events.Add){TimeMs=1};
        parser.Handle(0x0B61,Before);parser.TimeMs=2;parser.Handle(0x0C61,Update);parser.TimeMs=3;parser.Handle(0x0B61,After);
        Assert.Equal(new GameEvent[]{new OdyleEnergyEvent(1,0,485),new OdyleEnergyEvent(2,15,485),new OdyleEnergyEvent(3,15,485)},events);
    }
    [Fact] public void MultiByteAvailableEnergyDecodesWithoutLosingFollowingProperties()
    {
        // Second-character snapshot: available160 (+270); another multi-byte property follows.
        var body=Convert.FromHexString("020C01879303A0018E020407879303AC02");
        Assert.Equal(new OdyleEnergyEvent(1,160,270),OdyleEnergyPackets.Snapshot(1,body));
        var update=Convert.FromHexString("000C01879303A0018E0202");
        Assert.Equal(new OdyleEnergyEvent(2,160,270),OdyleEnergyPackets.Update(2,update));
        for(var n=0;n<body.Length;n++)Assert.Null(OdyleEnergyPackets.Snapshot(1,body.AsSpan(0,n)));
        for(var n=0;n<update.Length;n++)Assert.Null(OdyleEnergyPackets.Update(1,update.AsSpan(0,n)));
    }
    [Theory]
    [InlineData("7F",127u)]
    [InlineData("8001",128u)]
    [InlineData("A001",160u)]
    [InlineData("FF01",255u)]
    [InlineData("8002",256u)]
    [InlineData("C806",840u)]
    [InlineData("FFFFFFFF0F",uint.MaxValue)]
    public void AvailableEnergySupportsAllUnsignedVarintWidths(string encoded,uint amount)
    {
        var entry="0C01879303"+encoded+"8E02";
        Assert.Equal(new OdyleEnergyEvent(1,amount,270),OdyleEnergyPackets.Snapshot(1,Convert.FromHexString("01"+entry)));
        Assert.Equal(new OdyleEnergyEvent(2,amount,270),OdyleEnergyPackets.Update(2,Convert.FromHexString("00"+entry+"02")));
    }
}
