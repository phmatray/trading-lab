using Shouldly;
using TradyStrat.Application.AiSuggestion.CallDiff;
using TradyStrat.Domain.Shared;
using TradyStrat.Domain.Suggestions;
using Xunit;

namespace TradyStrat.Application.Tests.AiSuggestion.CallDiff;

public class CallDiffBuilderTests
{
    private static Suggestion Make(
        SuggestionAction action, int conviction, params (string Indicator, string Ticker, string Value)[] cits)
    {
        var citations = cits.Select(c => new Citation("", c.Indicator, c.Ticker, c.Value)).ToList();
        return Suggestion.From(
            instrumentId: new InstrumentId(1),
            forDate:      new DateOnly(2026, 5, 7),
            action:       action,
            quantityHint: Quantity.None,
            maxPriceHint: Price.None(Currency.Eur),
            conviction:   Conviction.Of(conviction),
            rationale:    "x",
            citations:    citations,
            snapshot:     MarketSnapshot.Empty,
            fingerprint:  PromptFingerprint.Of("h", "", ""),
            thinkingText: "",
            createdAt:    DateTime.UtcNow);
    }

    [Fact]
    public void Returns_None_when_prior_is_null()
        => new CallDiffBuilder()
            .WithToday(Make(SuggestionAction.Hold, 5))
            .WithPrior(null)
            .Build().ShouldBe(Application.AiSuggestion.CallDiff.CallDiff.None);

    [Fact]
    public void Detects_action_change()
    {
        var diff = new CallDiffBuilder()
            .WithToday(Make(SuggestionAction.Hold, 5))
            .WithPrior(Make(SuggestionAction.Trim, 4))
            .Build();

        diff.ActionChanged.ShouldBeTrue();
        diff.PriorAction.ShouldBe(SuggestionAction.Trim);
        diff.ConvictionDelta.ShouldBe(1);
    }

    [Fact]
    public void Detects_no_action_change_no_conviction_delta()
    {
        var diff = new CallDiffBuilder()
            .WithToday(Make(SuggestionAction.Hold, 5))
            .WithPrior(Make(SuggestionAction.Hold, 5))
            .Build();

        diff.ActionChanged.ShouldBeFalse();
        diff.ConvictionDelta.ShouldBe(0);
        diff.AddedCitationKeys.ShouldBeEmpty();
        diff.RemovedCitationKeys.ShouldBeEmpty();
        diff.ChangedCitations.ShouldBeEmpty();
    }

    [Fact]
    public void Detects_added_removed_changed_citations()
    {
        var today = Make(SuggestionAction.Hold, 5,
            ("RSI(14)", "CON3.L", "51"),
            ("Ichimoku", "CON3.L", "Inside"),
            ("RSI(14)", "BTC-USD", "70"));   // added
        var prior = Make(SuggestionAction.Hold, 5,
            ("RSI(14)", "CON3.L", "49"),     // value changed
            ("Ichimoku", "CON3.L", "Below"), // value changed
            ("Bollinger", "CON3.L", "Inside"));  // removed

        var diff = new CallDiffBuilder().WithToday(today).WithPrior(prior).Build();

        diff.AddedCitationKeys.ShouldContain("RSI(14):BTC-USD");
        diff.RemovedCitationKeys.ShouldContain("Bollinger:CON3.L");
        diff.ChangedCitations.Select(c => c.Key).ShouldContain("RSI(14):CON3.L");
        diff.ChangedCitations.Select(c => c.Key).ShouldContain("Ichimoku:CON3.L");
    }

    [Fact]
    public void Summary_paragraph_mentions_changes()
    {
        var diff = new CallDiffBuilder()
            .WithToday(Make(SuggestionAction.Hold, 5, ("RSI(14)", "BTC-USD", "70")))
            .WithPrior(Make(SuggestionAction.Trim, 4))
            .Build();

        diff.SummaryParagraph.ShouldContain("Trim");
        diff.SummaryParagraph.ShouldContain("Hold");
    }
}
