using AionMeter.Core.Events;
namespace AionMeter.Tests;

public sealed class EventAlertTests
{
    private static DateTimeOffset At(int minute, int second = 0) => new(2026,10,13,13,minute,second,TimeSpan.FromHours(2));
    [Fact]
    public void Reminder_is_once_per_occurrence_and_only_within_lead_window()
    {
        var alerts = new EventAlerts();
        Assert.Empty(alerts.TakeDue(At(50), "Europe", ["shugo"], 2));
        Assert.Single(alerts.TakeDue(At(58), "Europe", ["shugo"], 2));
        Assert.Empty(alerts.TakeDue(At(59), "Europe", ["shugo"], 2));
        Assert.Single(alerts.TakeDue(At(58).AddHours(1), "Europe", ["shugo"], 2));
    }
    [Fact]
    public void At_start_reminders_survive_polling_across_the_start()
    {
        var alerts = new EventAlerts();
        Assert.Empty(alerts.TakeDue(At(59,59), "Europe", ["shugo"], 0));
        var at = At(59,59).AddSeconds(1.5);
        Assert.Single(alerts.TakeDue(at, "Europe", ["shugo"], 0));
        Assert.Empty(alerts.TakeDue(at.AddSeconds(1), "Europe", ["shugo"], 0));
        Assert.Empty(new EventAlerts().TakeDue(at.AddSeconds(10), "Europe", ["shugo"], 0));
    }
    [Fact]
    public void Simultaneous_selected_events_are_all_announced()
    {
        var alerts = new EventAlerts();
        Assert.Equal(2, alerts.TakeDue(At(58), "Europe", ["shugo","rift"], 2).Count);
        Assert.Empty(alerts.TakeDue(At(59), "Europe", [], 2));
    }
    [Theory]
    [InlineData("kaira", "BOSS")]
    [InlineData("siege-bosses", "BOSS")]
    [InlineData("rift", "PVP")]
    [InlineData("shugo", "EVENT")]
    public void Classification_is_shared_by_attached_and_detached_cards(string id, string expected) => Assert.Equal(expected, EventKinds.For(id));
}
