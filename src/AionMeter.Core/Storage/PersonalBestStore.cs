using System.Text.Json;
using System.Text.Json.Serialization;
using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;

namespace AionMeter.Core.Storage;

public sealed record BossAttempt(Guid FightId, DateTimeOffset StartedAt, long CombatMs,
    EncounterEndReason Reason, double Dps, long Damage, long MaxHit, IReadOnlyList<int> DeathOffsetsMs,
    bool EligibleForBest, long Healing = 0, long DamageTaken = 0);

public sealed record BossPersonalStats(string Character, int ServerId, GameClass Class, int BossCode,
    int MapId, string BossName, string? Zone, int Attempts, int Kills, long Deaths,
    BossAttempt? BestDps, BossAttempt? BestDamage, BossAttempt? BestHit, IReadOnlyList<BossAttempt> RecentAttempts,
    BossAttempt? BestHealing = null, long Healing = 0, long DamageTaken = 0);

/// <summary>Local boss-only personal records, independent of the ten saved fight files.</summary>
public sealed class PersonalBestStore
{
    public const int RecentLimit = 10;
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
    private readonly object _gate = new();
    private readonly GameData _data;
    private readonly string? _path;
    private List<BossPersonalStats> _entries = [];

    public PersonalBestStore(GameData data, string? path = null)
    {
        _data = data;
        _path = path;
        if (path is null) return;
        try
        {
            _entries = (JsonSerializer.Deserialize<List<BossPersonalStats>>(File.ReadAllText(path), Json) ?? [])
                .Where(e => e is not null && !string.IsNullOrWhiteSpace(e.Character) && e.ServerId > 0
                    && IsBoss(e.BossCode) && e.RecentAttempts is not null
                    && e.RecentAttempts.All(a => a is not null && a.DeathOffsetsMs is not null)
                    && e.Attempts >= 0 && e.Kills >= 0 && e.Deaths >= 0)
                .Select(e => e with { RecentAttempts = e.RecentAttempts.OrderByDescending(a => a.StartedAt).Take(RecentLimit).ToArray() }).ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { }
    }

    public event Action? Changed;
    private bool IsBoss(int code) => _data.Npcs.TryGetValue(code, out var npc) && npc.IsBoss && !npc.IsDummy;

    public IReadOnlyList<BossPersonalStats> List()
    {
        lock (_gate) return _entries.OrderBy(e => e.Character, StringComparer.Ordinal)
            .ThenBy(e => e.BossName, StringComparer.Ordinal).ToArray();
    }

    /// <summary>Call once for each live finished encounter. Recordings and demo sources are excluded by the caller.</summary>
    public BossPersonalStats? Record(FightRecord record, bool captureClean = true)
    {
        var s = record.Summary;
        if (s.IsActive || s.Reason == EncounterEndReason.None || s.Boss is not { NpcCode: > 0 } boss
            || !IsBoss(boss.NpcCode) || s.CombatMs <= 0) return null;
        var ownRows = s.Combatants.Where(c => c.IsSelf && !c.IsUnknownSummons).Take(2).ToArray();
        var own = ownRows.Length == 1 ? ownRows[0] : null;
        if (own is null || string.IsNullOrWhiteSpace(own.Name) || own.Name.StartsWith('#')
            || own.ServerId <= 0 || own.Deaths is null || own.Damage < 0 || own.MaxHit < 0 || own.Healing < 0 || own.DamageTaken < 0
            || !double.IsFinite(own.Dps) || own.Dps < 0) return null;
        var detail = record.Details.FirstOrDefault(c => c.IsSelf && c.ActorId == own.ActorId);
        if (detail?.DeathOffsetsMs is not { } deaths || deaths.Count != own.Deaths
            || deaths.Any(d => d < 0) || (own.Damage == 0 && own.Healing == 0 && own.DamageTaken == 0 && deaths.Count == 0)) return null;
        var eligible = captureClean && s.Reason == EncounterEndReason.Kill && s.BossStartObserved
            && s.BossOnlyDamage && boss.MaxHpKnown && boss.MaxHp > 0 && boss.Hp == 0
            && boss.Uncounted == 0;
        var attempt = new BossAttempt(s.Id, s.StartedAt, s.CombatMs, s.Reason, own.Dps, own.Damage,
            own.MaxHit, deaths.ToArray(), eligible, own.Healing, own.DamageTaken);
        BossPersonalStats updated;
        lock (_gate)
        {
            var previous = _entries.FirstOrDefault(e => e.Character == own.Name && e.ServerId == own.ServerId
                && e.Class == own.Class && e.BossCode == boss.NpcCode && e.MapId == s.MapId);
            if (previous?.RecentAttempts.Any(a => a.FightId == s.Id) == true) return null;
            previous ??= new(own.Name, own.ServerId, own.Class, boss.NpcCode, s.MapId, boss.Name, s.Zone, 0, 0, 0, null, null, null, []);
            updated = previous with
            {
                BossName = boss.Name, Zone = s.Zone,
                Attempts = previous.Attempts + 1,
                Kills = previous.Kills + (s.Reason == EncounterEndReason.Kill ? 1 : 0),
                Deaths = previous.Deaths + deaths.Count,
                Healing = previous.Healing + own.Healing,
                DamageTaken = previous.DamageTaken + own.DamageTaken,
                BestDps = eligible && attempt.Dps > 0 && (previous.BestDps is null || attempt.Dps > previous.BestDps.Dps) ? attempt : previous.BestDps,
                BestDamage = eligible && attempt.Damage > 0 && (previous.BestDamage is null || attempt.Damage > previous.BestDamage.Damage) ? attempt : previous.BestDamage,
                BestHit = eligible && attempt.MaxHit > 0 && (previous.BestHit is null || attempt.MaxHit > previous.BestHit.MaxHit) ? attempt : previous.BestHit,
                BestHealing = eligible && attempt.Healing > 0 && (previous.BestHealing is null || attempt.Healing > previous.BestHealing.Healing) ? attempt : previous.BestHealing,
                RecentAttempts = previous.RecentAttempts.Append(attempt).OrderByDescending(a => a.StartedAt).Take(RecentLimit).ToArray(),
            };
            var next = _entries.Where(e => e != previous).Append(updated).ToList();
            if (_path is not null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
                var temporary = _path + ".tmp";
                File.WriteAllText(temporary, JsonSerializer.Serialize(next, Json));
                File.Move(temporary, _path, overwrite: true);
            }
            _entries = next;
        }
        Changed?.Invoke();
        return updated;
    }
}
