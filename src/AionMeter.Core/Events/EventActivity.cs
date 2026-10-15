namespace AionMeter.Core.Events;

/// <summary>How long an occurrence remains in the started-event display.</summary>
public static class EventActivity
{
    public static TimeSpan Duration(string id) => TimeSpan.FromMinutes(id == "rift" ? 10 : EventKinds.For(id) == "BOSS" ? 2 : 3);
    public static bool IsActive(EventStart item, DateTimeOffset now) => item.StartsAt <= now && now - item.StartsAt < Duration(item.Id);
    public static int RemainingSeconds(EventStart item, DateTimeOffset now) =>
        Math.Max(0, (int)Math.Ceiling((Duration(item.Id) - (now - item.StartsAt)).TotalSeconds));
}
