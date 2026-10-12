using AionMeter.Core.Events;
namespace AionMeter.Tests;
public class AutomaticEventTests
{
    [Fact] public void SimultaneousStartsAreShownTogether()
    { var next=AutomaticEventSchedule.Next(DateTimeOffset.Parse("2026-10-13T13:42:00+02:00"),"Europa",["shugo","rift"]); Assert.Equal(2,next.Count); Assert.Single(next.Select(e=>e.StartsAt).Distinct()); }
    [Fact] public void OnlyEarliestSelectedStartIsShown()
    { var next=AutomaticEventSchedule.Next(DateTimeOffset.Parse("2026-10-13T13:10:00+02:00"),"Europa",["shugo","rift","invasion"]); Assert.Equal("invasion",Assert.Single(next).Id); }
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
