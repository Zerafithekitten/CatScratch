namespace AionMeter.Core.Events;

public static class EventKinds
{
    public static string For(string id) => id switch
    {
        "rift" or "siege" => "PVP",
        "nahma" or "kaira" or "siege-bosses" => "BOSS",
        _ => "EVENT"
    };
}

/// <summary>One reminder per scheduled occurrence, with a small grace period for polling across its start.</summary>
public sealed class EventAlerts
{
    private readonly Dictionary<string, DateTimeOffset> _announced = [];
    public IReadOnlyList<EventStart> TakeDue(DateTimeOffset now, string region, IEnumerable<string> enabled, int leadMinutes)
    {
        var due = new List<EventStart>();
        foreach (var id in enabled.Distinct())
        {
            var start = AutomaticEventSchedule.Next(now.AddSeconds(-2), region, [id]).FirstOrDefault();
            if (start is null || start.StartsAt - now > TimeSpan.FromMinutes(Math.Clamp(leadMinutes, 0, 10)) ||
                _announced.GetValueOrDefault(id) == start.StartsAt) continue;
            _announced[id] = start.StartsAt;
            due.Add(start);
        }
        return due;
    }
}
