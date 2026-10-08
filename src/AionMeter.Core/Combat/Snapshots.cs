using AionMeter.Core.Events;

namespace AionMeter.Core.Combat;

/// <summary>Immutable copies handed to the UI and written to history; safe to read off the capture thread.</summary>
/// <param name="MaxHpKnown">False when the meter never saw the boss appear: <paramref name="MaxHp"/> is then only the
/// highest HP it saw, and the percentage means little.</param>
/// <param name="Uncounted">HP the boss had lost before the meter began watching it: a fight joined late, or the meter
/// started mid-fight. (Fights saved before 0.2.6 took it from the damage, which in the open world is 4.5× off.)</param>
public sealed record BossSnapshot(uint ActorId, int NpcCode, string Name, long Hp, long MaxHp, bool MaxHpKnown = true, long Uncounted = 0)
{
    public double HpFraction => MaxHp > 0 && Hp >= 0 ? Math.Clamp((double)Hp / MaxHp, 0, 1) : 1;

    /// <summary>The boss's HP (fraction of max) when the meter's count began, when a sizeable part went uncounted.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public double? CountedFrom => MaxHpKnown && MaxHp > 0 && Uncounted >= MaxHp * 0.05 ? 1 - (double)Uncounted / MaxHp : null;
}

public sealed record CombatantSnapshot(
    uint ActorId,
    string Name,
    GameClass Class,
    bool IsSelf,
    long Damage,
    double Dps,
    double Share,
    int Hits,
    double CritRate,
    long MaxHit,
    long DamageTaken,
    int ServerId = 0,
    int GearScore = 0,
    long CombatPower = 0)
{
    /// <summary>The "summons, owner unknown" pseudo-row (see <see cref="Combatant.UnknownSummonsId"/>).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsUnknownSummons => ActorId == Combatant.UnknownSummonsId;
}

public sealed record EncounterSnapshot(
    Guid Id,
    string Title,
    string? Zone,
    DateTimeOffset StartedAt,
    long ClockMs,
    long CombatMs,
    bool IsActive,
    EncounterEndReason Reason,
    long TotalDamage,
    double PartyDps,
    BossSnapshot? Boss,
    IReadOnlyList<CombatantSnapshot> Combatants,
    int TargetCode = 0)
{
    /// <summary>Players who took part (the unknown-summons row is not one).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public int PlayerCount => Combatants.Count(c => !c.IsUnknownSummons);

    /// <summary>
    /// The server sent this client only its own hits: on a world boss with a crowd every other player shows up with
    /// nothing but damage over time, so their numbers — and every share — are far from the real ones. Read from the
    /// rows, so fights saved earlier tell it too.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool OnlyOwnHits
    {
        get
        {
            if (Boss is null) return false;
            int withHits = 0, dotOnly = 0;
            foreach (var c in Combatants)
            {
                if (c.IsUnknownSummons || c.Damage <= 0) continue;
                if (c.Hits > 0) withHits++;
                else dotOnly++;
            }
            return withHits > 0 && dotOnly >= 5 && withHits * 4 <= dotOnly; // someone's hits came: ours
        }
    }
}

public sealed record SkillRow(
    int SkillCode,
    string Name,
    int Hits,
    int DotTicks,
    long Damage,
    double Dps,
    double Share,
    long Average,
    long Min,
    long Max,
    double? CritRate,
    double? BackRate,
    double? FrontRate,
    double? PerfectRate,
    double? HeavyRate,
    double? MultiRate);

public sealed record HitQuality(
    int Hits,
    int DotTicks,
    double? CritRate,
    double? BackRate,
    double? FrontRate,
    double? PerfectRate,
    double? HeavyRate,
    double? MultiRate,
    long MaxHit,
    string? MaxHitSkill);

public sealed record CombatantDetail(
    uint ActorId,
    string Name,
    GameClass Class,
    bool IsSelf,
    long Damage,
    double Dps,
    double Share,
    long CombatMs,
    long DamageTaken,
    int HitsTaken,
    HitQuality Quality,
    IReadOnlyList<SkillRow> Skills,
    IReadOnlyList<long> PerSecond,
    IReadOnlyList<CastRecord> Casts,
    int ServerId = 0,
    int GearScore = 0,
    long CombatPower = 0);

public sealed record SegmentInfo(Guid Id, string Title, DateTimeOffset StartedAt, long CombatMs, bool IsActive, EncounterEndReason Reason, long TotalDamage);

public enum BossNoticeKind
{
    /// <summary>Came into view alive (a spawn, or walking into its area).</summary>
    Alive,
    /// <summary>Died while in view: the respawn countdown starts now.</summary>
    Killed,
    /// <summary>Came into view already dead: killed earlier, time unknown.</summary>
    SeenDead,
}

/// <summary>A world boss sighting for the respawn timers; the position is where it was seen (0 when unknown); the server is
/// the local character's (every server runs its own bosses).</summary>
public sealed record BossNotice(int NpcCode, int MapId, string? Zone, BossNoticeKind Kind, long TimeMs, float X = 0, float Y = 0, float Z = 0,
    int ServerId = 0);

/// <summary>A whole fight as saved to disk.</summary>
public sealed record FightRecord(int Version, EncounterSnapshot Summary, IReadOnlyList<CombatantDetail> Details);
