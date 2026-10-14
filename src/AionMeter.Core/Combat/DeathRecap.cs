using AionMeter.Core.Events;

namespace AionMeter.Core.Combat;

public sealed record DeathRecapHit(long TimeMs, uint SourceId, string SourceName, bool IsPlayer,
    int SkillCode, string SkillName, long Damage, HitFlags Flags, int HitCount);
public sealed record DeathRecapSnapshot(long DiedAtMs, IReadOnlyList<DeathRecapHit> Hits, bool Truncated);

public sealed partial class CombatTracker
{
    public const int DeathRecapSeconds = 10;
    private const int MaxRecapHits = 4096;
    private readonly List<DamageEvent> _incomingHits = [];
    private uint _recapActor;
    private long _recapClock, _recapDeath, _recapDroppedAt;
    private DamageEvent[]? _deathHits;
    private bool _recapTruncated;

    private void ClearDeathRecap()
    {
        _incomingHits.Clear(); _deathHits = null;
        _recapActor = 0; _recapClock = _recapDeath = _recapDroppedAt = 0;
        _recapTruncated = false;
    }

    // Shares the combat lock; this history is independent of fight filters, kills and idle timeouts.
    private void ObserveDeathRecap(GameEvent e)
    {
        // Respawning also reloads the SAME map. Keep the fight, but discard the previous life’s recap.
        if (e is ZoneChangedEvent) { ClearDeathRecap(); return; }
        if (!_freshSelf || _entities.SelfId is not { } self || self == 0) return;
        if (_recapActor != self) { ClearDeathRecap(); _recapActor = self; }
        _recapClock = Math.Max(_recapClock, e.TimeMs);
        if (e is NpcHpEvent hp && hp.ActorId == self && !hp.IsNpc && hp.CurrentHp > 0 && hp.TimeMs > _recapDeath)
        {
            if (_deathHits is not null) { _deathHits = null; _incomingHits.Clear(); _recapDroppedAt = 0; }
            return;
        }
        if (e is DamageEvent d && d.TargetId == self && d.Damage > 0 && _deathHits is null
            && d.TimeMs >= _recapClock - DeathRecapSeconds * 1000)
        {
            _incomingHits.RemoveAll(h => h.TimeMs < _recapClock - DeathRecapSeconds * 1000);
            _incomingHits.Add(d);
            if (_incomingHits.Count > MaxRecapHits)
            {
                var oldest = _incomingHits.MinBy(h => h.TimeMs)!;
                _recapDroppedAt = Math.Max(_recapDroppedAt, oldest.TimeMs); _incomingHits.Remove(oldest);
            }
        }
        var deathAt = e switch
        {
            DeathEvent { AlreadyDead: false } death when death.ActorId == self => death.TimeMs,
            NpcHpEvent { IsNpc: false, CurrentHp: 0 } zero when zero.ActorId == self => zero.TimeMs,
            _ => 0
        };
        if (deathAt <= _recapDeath || _deathHits is not null) return;
        _recapDeath = deathAt;
        _deathHits = _incomingHits.Where(h => h.TimeMs >= deathAt - DeathRecapSeconds * 1000 && h.TimeMs <= deathAt)
            .OrderByDescending(h => h.TimeMs).ToArray();
        _recapTruncated = _recapDroppedAt >= deathAt - DeathRecapSeconds * 1000 && _recapDroppedAt > 0;
    }

    public DeathRecapSnapshot? DeathRecap()
    {
        lock (_gate)
        {
            if (_deathHits is null) return null;
            return new(_recapDeath, _deathHits.Select(h =>
            {
                var player = _entities.TryGetPlayer(h.SourceId, out var p);
                return new DeathRecapHit(h.TimeMs, h.SourceId, player ? p.Name : _entities.NpcName(h.SourceId), player,
                    h.SkillCode, _data.SkillName(h.SkillCode), h.Damage, h.Flags, h.HitCount);
            }).ToArray(), _recapTruncated);
        }
    }

    public long DeathRecapAtMs { get { lock (_gate) return _deathHits is null ? 0 : _recapDeath; } }
}
