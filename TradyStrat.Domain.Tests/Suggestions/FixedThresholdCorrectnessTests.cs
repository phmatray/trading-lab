using TradyStrat.Domain.Suggestions;
using TradyStrat.Domain.Suggestions.Services;
using Shouldly;
using Xunit;

namespace TradyStrat.Domain.Tests.Suggestions;

public class FixedThresholdCorrectnessTests
{
    private static readonly FixedThresholdCorrectness Rule = new(2.0m);

    [Theory]
    [InlineData(SuggestionAction.Acquire,  3.0,  true)]   // above
    [InlineData(SuggestionAction.Acquire,  1.0,  false)]  // within
    [InlineData(SuggestionAction.Acquire, -3.0,  false)]  // below
    [InlineData(SuggestionAction.Trim,    -3.0,  true)]   // below (good for trim)
    [InlineData(SuggestionAction.Trim,    -1.0,  false)]  // within
    [InlineData(SuggestionAction.Trim,     3.0,  false)]  // above
    [InlineData(SuggestionAction.Hold,     1.0,  true)]   // within
    [InlineData(SuggestionAction.Hold,     3.0,  false)]  // above
    [InlineData(SuggestionAction.Hold,    -3.0,  false)]  // below
    [InlineData(SuggestionAction.Wait,     1.0,  true)]   // within
    [InlineData(SuggestionAction.Wait,     3.0,  false)]  // above
    [InlineData(SuggestionAction.Wait,    -3.0,  false)]  // below
    public void Evaluate_returns_expected(SuggestionAction action, double fwd, bool expected)
    {
        Rule.Evaluate(action, (decimal)fwd).ShouldBe(expected);
    }
}
