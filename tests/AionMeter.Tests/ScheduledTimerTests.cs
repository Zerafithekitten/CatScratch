using AionMeter.Core.Events;
namespace AionMeter.Tests;

public class ScheduledTimerTests
{
    private static readonly DateTimeOffset Start = DateTimeOffset.Parse("2026-10-12T14:00:00+02:00");
    [Fact] public void OnceKeepsItsOriginalTimeAfterDeadline()
    {
        var timer = new ScheduledTimer("a", "Event", "EVENT", Start);
        Assert.Equal(Start, timer.NextAt(Start.AddDays(3)));
    }
    [Fact] public void BeforeFirstOccurrenceReturnsFirstTime()
    {
        var timer = new ScheduledTimer("a", "Event", "EVENT", Start, 60);
        Assert.Equal(Start, timer.NextAt(Start.AddMinutes(-1)));
    }
    [Fact] public void ExactOccurrenceDoesNotSkipCurrentDeadline()
    {
        var timer = new ScheduledTimer("a", "Event", "EVENT", Start, 60);
        Assert.Equal(Start.AddHours(3), timer.NextAt(Start.AddHours(3)));
    }
    [Fact] public void MissedOccurrencesAdvanceToNextDeadline()
    {
        var timer = new ScheduledTimer("a", "Event", "EVENT", Start, 60);
        Assert.Equal(Start.AddHours(4), timer.NextAt(Start.AddHours(3).AddSeconds(1)));
    }
    [Fact] public void DifferentOffsetsReferToTheSameInstant()
    {
        var timer = new ScheduledTimer("a", "Event", "EVENT", Start, 60);
        Assert.Equal(Start.AddHours(1), timer.NextAt(Start.ToUniversalTime().AddMinutes(1)));
    }
}
