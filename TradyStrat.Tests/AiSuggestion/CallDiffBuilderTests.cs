using Shouldly;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Common.Domain;
using Xunit;

namespace TradyStrat.Tests.AiSuggestion;

public class CallDiffBuilderTests
{
    private static readonly System.Text.Json.JsonSerializerOptions CitationOpts = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(
            System.Text.Json.JsonNamingPolicy.SnakeCaseLower) },
    };

    private static Suggestion Make(
        SuggestionAction action, int conviction, params (string Indicator, string Ticker, string Value)[] cits)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(
            cits.Select(c => new Citation("", c.Indicator, c.Ticker, c.Value)).ToList(),
            CitationOpts);
        return new Suggestion
        {
            Id = 0, ForDate = new DateOnly(2026, 5, 7),
            Action = action, Conviction = conviction,
            Rationale = "", CitationsJson = json, PromptHash = "h",
            CreatedAt = DateTime.UtcNow,
        };
    }

    [Fact]
    public void Returns_None_when_prior_is_null()
        => new CallDiffBuilder()
            .WithToday(Make(SuggestionAction.Hold, 5))
            .WithPrior(null)
            .Build().ShouldBe(CallDiff.None);

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
