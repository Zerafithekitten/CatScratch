using AionMeter.Core.Events;

namespace AionMeter.Tests;

public sealed class EventActivityTests
{
    [Theory]
    [InlineData("shugo",180)]
    [InlineData("invasion",180)]
    [InlineData("siege",180)]
    [InlineData("rift",600)]
    [InlineData("nahma",120)]
    [InlineData("kaira",120)]
    [InlineData("siege-bosses",120)]
    [InlineData("boss:2305:123",120)]
    public void Started_duration_and_expiration_are_exact(string id,int seconds)
    {
        var item = new EventStart(id,"Sample",DateTimeOffset.Parse("2026-10-15T14:00:00+02:00"));
        Assert.False(EventActivity.IsActive(item,item.StartsAt.AddTicks(-1)));
        Assert.True(EventActivity.IsActive(item,item.StartsAt));
        Assert.Equal(seconds,EventActivity.RemainingSeconds(item,item.StartsAt));
        Assert.Equal(seconds-1,EventActivity.RemainingSeconds(item,item.StartsAt.AddSeconds(1)));
        Assert.True(EventActivity.IsActive(item,item.StartsAt.AddSeconds(seconds).AddTicks(-1)));
        Assert.Equal(1,EventActivity.RemainingSeconds(item,item.StartsAt.AddSeconds(seconds).AddMilliseconds(-100)));
        Assert.False(EventActivity.IsActive(item,item.StartsAt.AddSeconds(seconds)));
        Assert.Equal(0,EventActivity.RemainingSeconds(item,item.StartsAt.AddSeconds(seconds+10)));
    }
}
