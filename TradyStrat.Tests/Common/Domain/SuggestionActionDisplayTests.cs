using Shouldly;
using TradyStrat.Common.Domain;
using Xunit;

namespace TradyStrat.Tests.Common.Domain;

public class SuggestionActionDisplayTests
{
    [Theory]
    [InlineData(SuggestionAction.Acquire, "Acquire", "acquire")]
    [InlineData(SuggestionAction.Hold,    "Hold",    "hold")]
    [InlineData(SuggestionAction.Trim,    "Trim",    "trim")]
    [InlineData(SuggestionAction.Wait,    "Wait",    "wait")]
    public void KnownActions(SuggestionAction a, string verb, string stem)
    {
        SuggestionActionDisplay.Verb(a).ShouldBe(verb);
        SuggestionActionDisplay.Stem(a).ShouldBe(stem);
    }

    [Fact]
    public void NullAction_FallsBackToDash()
    {
        SuggestionActionDisplay.Verb(null).ShouldBe("—");
        SuggestionActionDisplay.Stem(null).ShouldBe("none");
    }
}
