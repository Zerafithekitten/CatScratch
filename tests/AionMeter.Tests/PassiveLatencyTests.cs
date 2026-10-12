using System.Net;
using AionMeter.Core.Capture;
namespace AionMeter.Tests;
public class PassiveLatencyTests
{
    private static readonly FlowKey Client=new(IPAddress.Parse("10.0.0.1"),40000,IPAddress.Parse("10.0.0.2"),13328);
    private static readonly FlowKey Server=new(Client.Dst,Client.DstPort,Client.Src,Client.SrcPort);
    private static SegmentReader.Segment Send(long at,uint seq=100)=>new(at,Client,seq,false,false,new byte[10]);
    private static SegmentReader.Segment Ack(long at,uint ack=110)=>new(at,Server,1000,false,false,[],true,ack);
    [Fact] public void MeasuresClientSendToServerAckAndExpires()
    { var p=new PassiveLatency(); p.TrackServerFlow(Server); p.Observe(Send(100)); p.Observe(Ack(145)); Assert.Equal(45d,p.Read(150)); Assert.Null(p.Read(15146)); }
    [Fact] public void RetransmissionAndDuplicateAckDoNotProduceSamples()
    { var p=new PassiveLatency(); p.TrackServerFlow(Server); p.Observe(Send(100)); p.Observe(Send(120)); p.Observe(Ack(145)); p.Observe(Ack(155)); Assert.Null(p.Read(160)); }
    [Fact] public void UnconfirmedFlowsAndIncomingDataAloneAreIgnored()
    { var p=new PassiveLatency(); p.Observe(Send(100)); p.Observe(Ack(145)); Assert.Null(p.Read(150)); p.TrackServerFlow(Server); p.Observe(new(160,Server,1,false,false,new byte[10])); Assert.Null(p.Read(170)); }
    [Fact] public void SequenceWrapAndResetAreHandled()
    { var p=new PassiveLatency(); p.TrackServerFlow(Server); p.Observe(Send(100,uint.MaxValue-4)); p.Observe(Ack(130,5)); Assert.Equal(30d,p.Read(140)); p.Reset(); Assert.Null(p.Read(140)); }
}
