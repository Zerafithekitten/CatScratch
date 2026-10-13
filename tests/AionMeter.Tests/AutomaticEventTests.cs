using AionMeter.Core.Events;
namespace AionMeter.Tests;
public class AutomaticEventTests
{
    [Fact] public void StartedEventsRemainTogetherUntilTwoMinuteBoundary()
    {
        var start = DateTimeOffset.Parse("2026-10-13T14:00:00+02:00");
        var running = AutomaticEventSchedule.Next(start.AddSeconds(119),"Europe",["shugo","rift","invasion"],TimeSpan.FromMinutes(2));
        Assert.Equal(2,running.Count(e=>e.StartsAt==start));
        Assert.Contains(running,e=>e.Id=="invasion" && e.StartsAt==start.AddMinutes(30));
        Assert.Contains(running,e=>e.Id=="shugo" && e.StartsAt==start.AddHours(1));
        var future = AutomaticEventSchedule.Next(start.AddMinutes(2),"Europe",["shugo","rift","invasion"],TimeSpan.FromMinutes(2));
        Assert.Equal(2,future.Count);
        Assert.All(future,e=>Assert.True(e.StartsAt>start));
        var next = future[0];
        Assert.Equal("invasion",next.Id);
        Assert.Equal(start.AddMinutes(30),next.StartsAt);
    }
    [Fact] public void RecentStartsHandleMidnightAndRespectSelection()
    {
        var now = DateTimeOffset.Parse("2026-10-14T00:01:00+02:00");
        var starts = AutomaticEventSchedule.Next(now,"Europe",["shugo"],TimeSpan.FromMinutes(2));
        Assert.Equal(2,starts.Count);
        var item = starts[0];
        Assert.Equal(now.AddMinutes(-1),item.StartsAt);
        Assert.Empty(AutomaticEventSchedule.Next(now,"Europe",[],TimeSpan.FromMinutes(2)));
    }
    [Fact] public void DefaultScheduleDoesNotReturnPastStartsForSoundAlerts()
    {
        var now = DateTimeOffset.Parse("2026-10-13T14:01:00+02:00");
        Assert.All(AutomaticEventSchedule.Next(now,"Europe",["shugo","rift"]),e => Assert.True(e.StartsAt>=now));
    }
    [Fact] public void SimultaneousStartsAreShownTogether()
    { var next=AutomaticEventSchedule.Next(DateTimeOffset.Parse("2026-10-13T13:42:00+02:00"),"Europa",["shugo","rift"]); Assert.Equal(2,next.Count); Assert.Single(next.Select(e=>e.StartsAt).Distinct()); }
    [Fact] public void AllSelectedStartsWithinOneHourAreShownInTimeOrder()
    {
        var now=DateTimeOffset.Parse("2026-10-13T13:10:00+02:00");
        var next=AutomaticEventSchedule.Next(now,"Europa",["shugo","rift","invasion"]);
        Assert.Equal(3,next.Count); Assert.Equal("invasion",next[0].Id);
        Assert.Equal(now.AddMinutes(20),next[0].StartsAt);
        Assert.All(next.Skip(1),e=>Assert.Equal(now.AddMinutes(50),e.StartsAt));
        Assert.Equal(next.OrderBy(e=>e.StartsAt).Select(e=>e.StartsAt),next.Select(e=>e.StartsAt));
    }
    [Fact] public void BeyondHourAndDisabledEventsAreAbsent()
    { var now=DateTimeOffset.Parse("2026-10-13T12:00:00+02:00"); Assert.Empty(AutomaticEventSchedule.Next(now,"Europa",["rift"])); Assert.Empty(AutomaticEventSchedule.Next(now,"Europa",[])); }
    [Fact] public void OneHourBoundaryIsIncluded()
    { var next=AutomaticEventSchedule.Next(DateTimeOffset.Parse("2026-10-13T13:00:00+02:00"),"Europa",["rift"]); Assert.Single(next); }
    [Fact] public void LocalMidnightAndRegionWork()
    { var now=DateTimeOffset.Parse("2026-10-13T23:45:00+02:00"); Assert.Equal(0,Assert.Single(AutomaticEventSchedule.Next(now,"Europa",["shugo"])).StartsAt.Hour); Assert.Equal(18,Assert.Single(AutomaticEventSchedule.Next(now,"NA East",["shugo"])).StartsAt.Hour); }
    [Fact] public void DstRepeatedHourChoosesFutureOccurrence()
    { var now=DateTimeOffset.Parse("2026-10-25T02:40:00+02:00"); var next=Assert.Single(AutomaticEventSchedule.Next(now,"Europa",["rift"])); Assert.Equal(DateTimeOffset.Parse("2026-10-25T02:00:00+01:00"),next.StartsAt); }
    [Fact] public void WeekdaysRestrictSiege()
    { Assert.Empty(AutomaticEventSchedule.Next(DateTimeOffset.Parse("2026-10-13T20:30:00+02:00"),"Europa",["siege"])); Assert.Single(AutomaticEventSchedule.Next(DateTimeOffset.Parse("2026-10-12T20:30:00+02:00"),"Europa",["siege"])); }
}
