namespace AionMeter.Core.Combat;

/// <summary>Compare like-for-like encounters without merging separate characters or instance variants.</summary>
public static class BossComparison
{
    public static EncounterSnapshot? Previous(EncounterSnapshot current,IEnumerable<EncounterSnapshot> fights)=>
        fights.Where(p=>p.StartedAt<current.StartedAt && Compatible(current,p)).OrderByDescending(p=>p.StartedAt).FirstOrDefault();
    public static bool Compatible(EncounterSnapshot current, EncounterSnapshot previous)
    {
        var a=current.Combatants.SingleOrDefault(c=>c.IsSelf);
        var b=previous.Combatants.SingleOrDefault(c=>c.IsSelf);
        return current.Id!=previous.Id && !current.IsActive && !previous.IsActive
            && current.Boss is { NpcCode: >0 } boss && previous.Boss?.NpcCode==boss.NpcCode
            && current.MapId>0 && current.MapId==previous.MapId
            && current.BossOnlyDamage==previous.BossOnlyDamage
            && current.Reason==previous.Reason && current.CombatMs>0 && previous.CombatMs>0
            && a is not null && b is not null && a.ServerId>0 && a.ServerId==b.ServerId && a.Class==b.Class
            && !string.IsNullOrWhiteSpace(a.Name) && string.Equals(a.Name,b.Name,StringComparison.Ordinal);
    }
}
