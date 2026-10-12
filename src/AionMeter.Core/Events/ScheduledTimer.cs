namespace AionMeter.Core.Events;

/// <summary>A user-supplied appointment. RepeatMinutes is an elapsed interval, not a server calendar rule.</summary>
public sealed record ScheduledTimer(string Id, string Name, string Kind, DateTimeOffset StartsAt, int RepeatMinutes = 0)
{
    public DateTimeOffset NextAt(DateTimeOffset now)
    {
        if (RepeatMinutes <= 0 || now <= StartsAt) return StartsAt;
        var interval = TimeSpan.FromMinutes(RepeatMinutes).Ticks;
        var steps = (now - StartsAt).Ticks / interval;
        if ((now - StartsAt).Ticks % interval != 0) steps++;
        return StartsAt.AddTicks(checked(steps * interval));
    }
}
