using AionMeter.Core.Events;
namespace AionMeter.Tests;

public class LiveIdentityTests
{
    [Fact] public void StartsUnconfirmed()
    {
        Assert.Null(new LiveIdentity().Current);
    }
    [Fact] public void SeeingAnotherPlayerDoesNotConfirmSelf()
    {
        var state = new LiveIdentity();
        state.Observe(new PlayerSeenEvent(1, 12, "Other", 2305, GameClass.Cleric));
        Assert.Null(state.Current);
    }
    [Theory]
    [InlineData(0, "Self")]
    [InlineData(12, "")]
    [InlineData(12, " ")]
    public void IncompleteIdentityDoesNotConfirmSelf(int actorId, string name)
    {
        var state = new LiveIdentity();
        state.Observe(new SelfIdentifiedEvent(1, (uint)actorId, name, 2305, GameClass.Cleric));
        Assert.Null(state.Current);
    }
    [Fact] public void NewSelfIdentityReplacesPreviousCharacter()
    {
        var state = new LiveIdentity();
        state.Observe(new SelfIdentifiedEvent(1, 12, "First", 2305, GameClass.Cleric));
        state.Observe(new SelfIdentifiedEvent(2, 13, "Second", 2306, GameClass.Assassin));
        Assert.Equal("Second", state.Current!.Name);
        Assert.Equal(2306, state.Current.ServerId);
    }
    [Fact] public void RestartRequiresFreshConfirmation()
    {
        var state = new LiveIdentity();
        state.Observe(new SelfIdentifiedEvent(1, 12, "Self", 2305, GameClass.Cleric));
        state.Reset();
        Assert.Null(state.Current);
    }
}
