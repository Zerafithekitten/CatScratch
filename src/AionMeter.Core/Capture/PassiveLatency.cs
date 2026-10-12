namespace AionMeter.Core.Capture;

/// <summary>Estimates RTT from captured client data and server ACKs. Never sends packets.</summary>
public sealed class PassiveLatency
{
    private sealed record Pending(uint Start, uint End, long At, bool Retransmitted = false);
    private readonly object _gate = new();
    private readonly Dictionary<FlowKey, List<Pending>> _flows = [];
    private readonly Queue<(long At, long Ms)> _samples = [];
    public void TrackServerFlow(FlowKey serverFlow)
    {
        lock (_gate) _flows.TryAdd(new(serverFlow.Dst,serverFlow.DstPort,serverFlow.Src,serverFlow.SrcPort), []);
    }
    public void Reset() { lock (_gate) { _flows.Clear(); _samples.Clear(); } }
    public double? Read(long now)
    {
        lock (_gate)
        {
            while (_samples.TryPeek(out var sample) && now-sample.At>15_000) _samples.Dequeue();
            var values=_samples.Select(s=>s.Ms).Order().ToArray();
            return values.Length==0 ? null : values[values.Length/2];
        }
    }
    public void Observe(SegmentReader.Segment segment)
    {
        lock (_gate)
        {
            if (_flows.TryGetValue(segment.Key,out var sent))
            {
                sent.RemoveAll(p=>segment.TimeMs-p.At>10_000);
                if (segment.Syn || segment.FinOrRst) { sent.Clear(); return; }
                if (segment.Payload.Length==0) return;
                var end=unchecked(segment.Seq+(uint)segment.Payload.Length);
                var retransmission=false;
                for(var i=0;i<sent.Count;i++)
                    if (After(end,sent[i].Start) && After(sent[i].End,segment.Seq))
                    { sent[i]=sent[i] with { Retransmitted=true }; retransmission=true; }
                if (!retransmission) sent.Add(new(segment.Seq,end,segment.TimeMs));
                if (sent.Count>128) sent.RemoveAt(0);
                return;
            }
            var reverse=new FlowKey(segment.Key.Dst,segment.Key.DstPort,segment.Key.Src,segment.Key.SrcPort);
            if (!segment.Ack || !_flows.TryGetValue(reverse,out var pending)) return;
            var acknowledged=pending.Where(p=>!After(p.End,segment.AckNumber)).ToArray();
            if (acknowledged.Length==0) return;
            // Retransmissions make the acknowledged send time ambiguous (Karn's rule).
            if (!acknowledged.Any(p=>p.Retransmitted))
            {
                var elapsed=segment.TimeMs-acknowledged.Max(p=>p.At);
                if (elapsed>=0 && elapsed<=10_000)
                { _samples.Enqueue((segment.TimeMs,elapsed)); while(_samples.Count>15) _samples.Dequeue(); }
            }
            pending.RemoveAll(p=>!After(p.End,segment.AckNumber));
        }
    }
    private static bool After(uint a,uint b)=>unchecked((int)(a-b))>0;
}
