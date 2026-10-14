using System.Text.Json;
using System.Text.Json.Serialization;
using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;

namespace AionMeter.Core.Storage;

public sealed record BossBestValue(decimal Value, DateTimeOffset StartedAt, Guid FightId);

public sealed record BossPersonalStats(string Character, int ServerId, GameClass Class, int BossCode,
    string BossName, BossBestValue? BestDps, BossBestValue? BestDamage, BossBestValue? BestHit,
    BossBestValue? BestHealing, BossBestValue? FewestDeaths, BossBestValue? LeastDamageTaken);

/// <summary>Permanent independent best values for every real boss. No attempt history or retention limit.</summary>
public sealed class PersonalBestStore
{
    private sealed record StoreFile(int SchemaVersion, IReadOnlyList<BossPersonalStats> Records);
    private sealed record LegacyAttempt(Guid FightId, DateTimeOffset StartedAt, double Dps, long Damage,
        long MaxHit, IReadOnlyList<int>? DeathOffsetsMs, bool EligibleForBest, long Healing = 0, long DamageTaken = 0);
    private sealed record LegacyStats(string Character, int ServerId, GameClass Class, int BossCode, string BossName,
        LegacyAttempt? BestDps, LegacyAttempt? BestDamage, LegacyAttempt? BestHit, LegacyAttempt? BestHealing,
        IReadOnlyList<LegacyAttempt>? RecentAttempts);
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
            var contents = File.ReadAllText(path);
            using var document = JsonDocument.Parse(contents);
            var legacy = document.RootElement.ValueKind == JsonValueKind.Array;
            if (legacy)
            {
                foreach (var entry in JsonSerializer.Deserialize<List<LegacyStats>>(contents, Json) ?? [])
                {
                    if (entry is null || !ValidIdentity(entry.Character, entry.ServerId, entry.Class, entry.BossCode)) continue;
                    var stats = new BossPersonalStats(entry.Character, entry.ServerId, entry.Class, entry.BossCode, entry.BossName, null, null, null, null, null, null);
                    var attempts = new[] { entry.BestDps, entry.BestDamage, entry.BestHit, entry.BestHealing }
                        .Concat(entry.RecentAttempts ?? []).OfType<LegacyAttempt>();
                    foreach (var attempt in attempts)
                    {
                        if (!attempt.EligibleForBest || attempt.DeathOffsetsMs is null || attempt.DeathOffsetsMs.Any(d => d < 0)
                            || !ValidDps(attempt.Dps) || attempt.Damage < 0 || attempt.MaxHit < 0 || attempt.Healing < 0 || attempt.DamageTaken < 0) continue;
                        stats = Improve(stats, attempt.FightId, attempt.StartedAt, (decimal)attempt.Dps,
                            attempt.Damage, attempt.MaxHit, attempt.Healing, attempt.DeathOffsetsMs.Count, attempt.DamageTaken);
                    }
                    if (HasValues(stats)) MergeLoaded(stats);
                }
            }
            else if (JsonSerializer.Deserialize<StoreFile>(contents, Json) is { SchemaVersion: 2, Records: not null } saved)
            {
                foreach (var entry in saved.Records)
                    if (entry is not null && ValidIdentity(entry.Character, entry.ServerId, entry.Class, entry.BossCode)
                        && Values(entry).All(v => v is null || v.Value >= 0) && HasValues(entry)) MergeLoaded(entry);
            }
            if (legacy) Persist(_entries);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { }
    }

    public event Action? Changed;
    public IReadOnlyList<NpcDef> Catalog() => _data.Npcs.Values.Where(n => n.IsBoss && !n.IsDummy)
        .OrderBy(n => n.Name, StringComparer.Ordinal).ThenBy(n => n.Code).ToArray();
    private bool IsBoss(int code) => _data.Npcs.TryGetValue(code, out var npc) && npc.IsBoss && !npc.IsDummy;
    private static bool ValidIdentity(string? name, int server, GameClass job, int code) =>
        !string.IsNullOrWhiteSpace(name) && server > 0 && code > 0 && Enum.IsDefined(job);
    private static bool ValidDps(double dps) => double.IsFinite(dps) && dps >= 0 && dps < (double)decimal.MaxValue;
    private static IEnumerable<BossBestValue?> Values(BossPersonalStats stats) =>
        [stats.BestDps, stats.BestDamage, stats.BestHit, stats.BestHealing, stats.FewestDeaths, stats.LeastDamageTaken];
    private static bool HasValues(BossPersonalStats stats) => Values(stats).Any(v => v is not null);
    private static bool SameBoss(BossPersonalStats a, BossPersonalStats b) =>
        a.Character == b.Character && a.ServerId == b.ServerId && a.Class == b.Class && a.BossCode == b.BossCode;
    private static BossBestValue? Better(BossBestValue? old, BossBestValue? next, bool lower = false) =>
        next is not null && (old is null || (lower ? next.Value < old.Value : next.Value > old.Value)) ? next : old;
    private void MergeLoaded(BossPersonalStats entry)
    {
        var old = _entries.FirstOrDefault(e => SameBoss(e, entry));
        if (old is null) { _entries.Add(entry); return; }
        _entries.Remove(old);
        _entries.Add(old with
        {
            BestDps = Better(old.BestDps, entry.BestDps), BestDamage = Better(old.BestDamage, entry.BestDamage),
            BestHit = Better(old.BestHit, entry.BestHit), BestHealing = Better(old.BestHealing, entry.BestHealing),
            FewestDeaths = Better(old.FewestDeaths, entry.FewestDeaths, true), LeastDamageTaken = Better(old.LeastDamageTaken, entry.LeastDamageTaken, true),
        });
    }
    private static BossPersonalStats Improve(BossPersonalStats old, Guid fight, DateTimeOffset at, decimal dps,
        long damage, long hit, long healing, int deaths, long taken) => old with
    {
        BestDps = Better(old.BestDps, new(dps, at, fight)),
        BestDamage = Better(old.BestDamage, new(damage, at, fight)),
        BestHit = Better(old.BestHit, new(hit, at, fight)),
        BestHealing = Better(old.BestHealing, new(healing, at, fight)),
        FewestDeaths = Better(old.FewestDeaths, new(deaths, at, fight), true),
        LeastDamageTaken = Better(old.LeastDamageTaken, new(taken, at, fight), true),
    };

    public IReadOnlyList<BossPersonalStats> List()
    {
        lock (_gate) return _entries.OrderBy(e => e.Character, StringComparer.Ordinal)
            .ThenBy(e => e.BossName, StringComparer.Ordinal).ThenBy(e => e.BossCode).ToArray();
    }

    /// <summary>Only qualifying live boss victories can improve values. Equal or worse results do not write a file.</summary>
    public BossPersonalStats? Record(FightRecord record, bool captureClean = true)
    {
        var s = record.Summary;
        if (!captureClean || s.IsActive || s.Reason != EncounterEndReason.Kill || s.CombatMs <= 0
            || s.Boss is not { NpcCode: > 0, MaxHpKnown: true, MaxHp: > 0, Hp: 0, Uncounted: 0 } boss
            || !IsBoss(boss.NpcCode) || !s.BossStartObserved || !s.BossOnlyDamage) return null;
        var ownRows = s.Combatants.Where(c => c.IsSelf && !c.IsUnknownSummons).Take(2).ToArray();
        var own = ownRows.Length == 1 ? ownRows[0] : null;
        if (own is null || !ValidIdentity(own.Name, own.ServerId, own.Class, boss.NpcCode) || own.Name.StartsWith('#')
            || own.Deaths is null || own.Damage < 0 || own.MaxHit < 0 || own.Healing < 0 || own.DamageTaken < 0 || !ValidDps(own.Dps)) return null;
        var detail = record.Details.FirstOrDefault(c => c.IsSelf && c.ActorId == own.ActorId);
        if (detail?.DeathOffsetsMs is not { } deaths || deaths.Count != own.Deaths || deaths.Any(d => d < 0)
            || (own.Damage == 0 && own.Healing == 0 && own.DamageTaken == 0 && deaths.Count == 0)) return null;
        BossPersonalStats updated;
        lock (_gate)
        {
            var identity = new BossPersonalStats(own.Name, own.ServerId, own.Class, boss.NpcCode, boss.Name, null, null, null, null, null, null);
            var previous = _entries.FirstOrDefault(e => SameBoss(e, identity));
            updated = Improve(previous ?? identity, s.Id, s.StartedAt, (decimal)own.Dps, own.Damage, own.MaxHit, own.Healing, deaths.Count, own.DamageTaken);
            if (updated == previous) return null;
            var next = _entries.Where(e => !SameBoss(e, identity)).Append(updated).ToList();
            Persist(next);
            _entries = next;
        }
        Changed?.Invoke();
        return updated;
    }
    private void Persist(IReadOnlyList<BossPersonalStats> entries)
    {
        if (_path is null) return;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
        var temporary = _path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(new StoreFile(2, entries), Json));
        File.Move(temporary, _path, overwrite: true);
    }
}