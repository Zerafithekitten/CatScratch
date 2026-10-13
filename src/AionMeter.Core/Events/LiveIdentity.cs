namespace AionMeter.Core.Events;

/// <summary>Fresh identity from the live event source only. Restored caches and replay/demo events must bypass this.</summary>
public sealed class LiveIdentity
{
    private readonly object _gate = new();
    private SelfIdentifiedEvent? _current;
    private SelfIdentifiedEvent? _known;
    private uint? _actorId;
    private bool _awaitingZoneIdentity;
    private int _mapId = -1;
    public uint? ActorId { get { lock (_gate) return _actorId; } }
    public SelfIdentifiedEvent? Current { get { lock (_gate) return _current; } }
    public SelfIdentifiedEvent? KnownCharacter { get { lock (_gate) return _known; } }
    public SelfIdentifiedEvent? Observe(GameEvent value)
    {
        if (value is not (SelfIdentifiedEvent or PlayerSeenEvent or ZoneChangedEvent)) return null;
        lock (_gate)
        {
            if (value is ZoneChangedEvent zone)
            {
                var changed = !zone.IsTeleport && zone.MapId != _mapId;
                _mapId = zone.MapId;
                if (changed && _known is not null)
                { _actorId = null; _current = null; _awaitingZoneIdentity = true; }
                return null;
            }
            if (value is PlayerSeenEvent player)
            {
                if (_current is not null && _actorId == player.ActorId &&
                    (player.Name != _current.Name || (player.ServerId > 0 && player.ServerId != _current.ServerId)))
                { _current = _known = null; _actorId = null; _awaitingZoneIdentity = false; return null; }
                if (_current is not null || string.IsNullOrWhiteSpace(player.Name)) return null;
                var freshOwnActor = _actorId is { } actor && actor == player.ActorId;
                var exactCharacter = _known is { } known && player.Name == known.Name && player.ServerId > 0 && player.ServerId == known.ServerId;
                if (!freshOwnActor && !(_awaitingZoneIdentity && exactCharacter)) return null;
                value = new SelfIdentifiedEvent(player.TimeMs, player.ActorId, player.Name,
                    player.ServerId > 0 ? player.ServerId : exactCharacter ? _known!.ServerId : 0,
                    player.Class != GameClass.Unknown ? player.Class : exactCharacter ? _known!.Class : GameClass.Unknown,
                    exactCharacter ? _known!.Level : 0, exactCharacter ? _known!.CombatPower : 0, exactCharacter ? _known!.GearScore : 0);
            }
            if (value is not SelfIdentifiedEvent { ActorId: > 0 } self) return null;
            if (_actorId == self.ActorId && _current is { } prior &&
                (string.IsNullOrWhiteSpace(self.Name) || self.Name == prior.Name) &&
                (self.ServerId <= 0 || self.ServerId == prior.ServerId) &&
                (self.Class == GameClass.Unknown || prior.Class == GameClass.Unknown || self.Class == prior.Class))
                self = self with {
                    Name = string.IsNullOrWhiteSpace(self.Name) ? prior.Name : self.Name,
                    ServerId = self.ServerId > 0 ? self.ServerId : prior.ServerId,
                    Class = self.Class != GameClass.Unknown ? self.Class : prior.Class,
                    Level = self.Level > 0 ? self.Level : prior.Level,
                    CombatPower = self.CombatPower > 0 ? self.CombatPower : prior.CombatPower,
                    GearScore = self.GearScore > 0 ? self.GearScore : prior.GearScore
                };
            if (_actorId != self.ActorId || (_current is { } existing &&
                ((self.ServerId > 0 && existing.ServerId > 0 && self.ServerId != existing.ServerId) ||
                 (self.Class != GameClass.Unknown && existing.Class != GameClass.Unknown && self.Class != existing.Class))))
                _current = null;
            _actorId = self.ActorId;
            if (!string.IsNullOrWhiteSpace(self.Name)) { _current = _known = self; _awaitingZoneIdentity = false; }
            return self;
        }
    }
    public void Suspend() { lock (_gate) { _current = null; _actorId = null; _awaitingZoneIdentity = _known is not null; } }
    public void Reset() { lock (_gate) { _current = _known = null; _actorId = null; _awaitingZoneIdentity = false; _mapId = -1; } }

    /// <summary>Routine capture updates and traffic pauses do not end the live game session.</summary>
    public void ObserveCaptureStatus(Capture.CaptureStatus status, Action invalidateTracking)
    {
        if (status.State != Capture.CaptureState.WaitingForGame || status.Message != "AION 2 is not running") return;
        Reset();
        invalidateTracking();
    }
}
