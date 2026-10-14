using System.Net;
using AionMeter.Core.Capture;
using AionMeter.Core.Game;

namespace AionMeter.Tests;

public sealed class BetaCaptureTests
{
    [Fact]
    public void Retransmitted_syn_keeps_buffered_packets_until_lock()
    {
        var pipeline = new PacketPipeline(GameData.Empty, _ => { }) { LockThreshold = 2 };
        var key = new FlowKey(IPAddress.Loopback, PacketPipeline.GamePort, IPAddress.Loopback, 50000);
        byte[] heartbeat = [0x0e, 0, 0x36, 0, 0, 0, 0, 0, 0, 0, 0];
        pipeline.OnSegment(0, key, 99, true, false, []);
        pipeline.OnSegment(1, key, 100, false, false, heartbeat);
        pipeline.OnSegment(2, key, 99, true, false, []);
        pipeline.OnSegment(3, key, 111, false, false, heartbeat);
        Assert.Equal(1, pipeline.GameFlows);
        Assert.Equal(22, pipeline.GameBytes);
    }

    [Fact]
    public void Longer_retransmissions_share_one_pending_segment()
    {
        var gaps = 0;
        var stream = new TcpReassembler(_ => { }, () => gaps++);
        stream.Start(0);
        stream.Push(100, new byte[800_000], 0);
        stream.Push(100, new byte[800_001], 1);
        stream.Push(100, new byte[800_002], 2);
        Assert.Equal(0, gaps);
        stream.Push(0, new byte[100], 3);
        Assert.Equal(800_102, stream.Delivered);
    }

    [Fact]
    public void Syn_payload_and_fin_payload_are_both_processed()
    {
        var pipeline = new PacketPipeline(GameData.Empty, _ => { }) { LockThreshold = 1 };
        var key = new FlowKey(IPAddress.Loopback, PacketPipeline.GamePort, IPAddress.Loopback, 50000);
        byte[] heartbeat = [0x0e, 0, 0x36, 0, 0, 0, 0, 0, 0, 0, 0];
        pipeline.OnSegment(0, key, 99, true, false, heartbeat);
        Assert.Equal(1, pipeline.GameFlows);
        pipeline.OnSegment(1, key, 111, false, true, heartbeat);
        Assert.Equal(22, pipeline.GameBytes);
        Assert.Equal(0, pipeline.GameFlows);
    }

    [Fact]
    public void Restarting_reassembly_discards_segments_from_previous_connection()
    {
        var received = new List<byte>();
        var gaps = 0;
        var stream = new TcpReassembler(d => received.AddRange(d.ToArray()), () => gaps++);
        stream.Start(100);
        stream.Push(110, [99], 0);
        stream.Start(110);
        stream.Push(111, [42], 0);
        stream.Push(112, [43], 2000);
        Assert.Equal(new byte[] { 42, 43 }, received);
        Assert.Equal(1, gaps);
    }

    [Fact]
    public void Sequence_half_range_does_not_overflow_overlap_trimming()
    {
        var received = new List<byte>();
        var stream = new TcpReassembler(d => received.AddRange(d.ToArray()), () => { });
        stream.Start(0);
        stream.Push(0x80000000, [99], 0);
        stream.Push(0, [42], 1);
        Assert.Equal(new byte[] { 42 }, received);
    }

    [Fact]
    public void Sequence_wrap_delivers_each_byte_once()
    {
        var received = new List<byte>();
        var stream = new TcpReassembler(d => received.AddRange(d.ToArray()), () => { });
        stream.Start(uint.MaxValue - 1);
        stream.Push(1, [4], 0);
        stream.Push(uint.MaxValue - 1, [1, 2, 3], 1);
        stream.Push(uint.MaxValue, [2, 3, 4], 2);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, received);
    }

    [Fact]
    public void Reconnection_on_same_ports_relocks_and_delivers_new_heartbeat()
    {
        var pipeline = new PacketPipeline(GameData.Empty, _ => { }) { LockThreshold = 1 };
        var key = new FlowKey(IPAddress.Loopback, PacketPipeline.GamePort, IPAddress.Loopback, 50000);
        byte[] heartbeat = [0x0e, 0, 0x36, 0, 0, 0, 0, 0, 0, 0, 0];
        var locks = 0;
        pipeline.FlowLocked += _ => locks++;
        pipeline.OnSegment(0, key, 999, true, false, []);
        pipeline.OnSegment(1, key, 1000, false, false, heartbeat);
        pipeline.OnSegment(2, key, 99, true, false, []);
        pipeline.OnSegment(3, key, 100, false, false, heartbeat);
        Assert.Equal(2, locks);
        Assert.Equal(1, pipeline.GameFlows);
    }

    [Fact]
    public void Closing_game_connection_removes_locked_flow()
    {
        var pipeline = new PacketPipeline(GameData.Empty, _ => { }) { LockThreshold = 1 };
        var key = new FlowKey(IPAddress.Loopback, PacketPipeline.GamePort, IPAddress.Loopback, 50000);
        pipeline.OnSegment(0, key, 100, false, false, [0x0e, 0, 0x36, 0, 0, 0, 0, 0, 0, 0, 0]);
        pipeline.OnSegment(1, key, 111, false, true, []);
        Assert.Equal(0, pipeline.GameFlows);
    }
}
