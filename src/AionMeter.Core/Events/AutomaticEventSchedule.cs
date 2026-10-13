namespace AionMeter.Core.Events;

public sealed record AutomaticEvent(string Id, string Name, int[] Minutes, DayOfWeek[]? Days = null);
public sealed record EventStart(string Id, string Name, DateTimeOffset StartsAt);
public static class AutomaticEventSchedule
{
    public const string Source = "https://shugo.gg/timers";
    public const string Checked = "2026-10-12";
    public static IReadOnlyList<AutomaticEvent> Catalog { get; } =
    [
        new("shugo", "Shugo Festival", Enumerable.Range(0,24).Select(h=>h*60).ToArray()),
        new("rift", "Spacetime Rift", Enumerable.Range(0,8).Select(h=>(2+h*3)*60).ToArray()),
        new("invasion", "Dimensional Invasion", Enumerable.Range(0,24).Select(h=>h*60+30).ToArray()),
        new("siege", "Artifact Siege", [21*60], [DayOfWeek.Monday,DayOfWeek.Thursday,DayOfWeek.Saturday]),
        new("siege-bosses", "Siege Bosses", [21*60+30], [DayOfWeek.Monday,DayOfWeek.Thursday,DayOfWeek.Saturday]),
        new("nahma", "Guardian Lord Nahma", [21*60], [DayOfWeek.Friday,DayOfWeek.Sunday]),
        new("kaira", "Watcher Kaira", Enumerable.Range(0,8).Select(h=>(1+h*3)*60).ToArray())
    ];
    public static TimeZoneInfo Zone(string region) => TimeZoneInfo.FindSystemTimeZoneById(region switch
    { "NA East" => "Eastern Standard Time", "NA West" => "Pacific Standard Time", _ => "W. Europe Standard Time" });
    public static IReadOnlyList<EventStart> Next(DateTimeOffset now, string region, IEnumerable<string> enabled, TimeSpan? recentlyStarted = null)
    {
        var selected = enabled.ToHashSet();
        var zone = Zone(region);
        var day = TimeZoneInfo.ConvertTime(now,zone).Date;
        var candidates = new List<EventStart>();
        foreach(var e in Catalog.Where(e=>selected.Contains(e.Id)))
        foreach(var date in new[]{day.AddDays(-1),day,day.AddDays(1)})
        {
            if(e.Days is not null && !e.Days.Contains(date.DayOfWeek)) continue;
            foreach(var minute in e.Minutes)
            {
                var local = DateTime.SpecifyKind(date.AddMinutes(minute),DateTimeKind.Unspecified);
                if(zone.IsInvalidTime(local)) continue;
                var offsets = zone.IsAmbiguousTime(local) ? zone.GetAmbiguousTimeOffsets(local) : [zone.GetUtcOffset(local)];
                foreach(var offset in offsets)
                {
                    var at = new DateTimeOffset(local,offset);
                    if((at >= now || recentlyStarted is { } grace && at > now - grace) && at <= now.AddHours(1)) candidates.Add(new(e.Id,e.Name,at));
                }
            }
        }
        return candidates.OrderBy(e=>e.StartsAt).ThenBy(e=>e.Id)
            .DistinctBy(e=>(e.Id,e.StartsAt)).ToArray();
    }
}
