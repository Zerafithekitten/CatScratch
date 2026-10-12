namespace AionMeter.Core.Events;

/// <summary>Fresh identity from the live event source only. Restored caches and replay/demo events must bypass this.</summary>
public sealed class LiveIdentity
{
    private readonly object _gate = new();
    private SelfIdentifiedEvent? _current;
    private uint? _actorId;
    public uint? ActorId { get { lock (_gate) return _actorId; } }
    public SelfIdentifiedEvent? Current { get { lock (_gate) return _current; } }
    public void Observe(GameEvent value)
    {
        if (value is not SelfIdentifiedEvent { ActorId: > 0 } self) return;
        lock (_gate)
        {
            if (_actorId != self.ActorId) _current = null;
            _actorId = self.ActorId;
            if (!string.IsNullOrWhiteSpace(self.Name)) _current = self;
        }
    }
    public void Reset() { lock (_gate) { _current = null; _actorId = null; } }

    /// <summary>Routine capture updates and traffic pauses do not end the live game session.</summary>
    public void ObserveCaptureStatus(Capture.CaptureStatus status, Action invalidateTracking)
    {
        if (status.State != Capture.CaptureState.WaitingForGame || status.Message != "AION 2 is not running") return;
        Reset();
        invalidateTracking();
    }
}
