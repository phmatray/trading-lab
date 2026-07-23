using Shouldly;
using TradyStrat.Application.AiSuggestion.UseCases;
using TradyStrat.Cli.Mcp.Mapping;
using TradyStrat.Domain;
using TradyStrat.Domain.Suggestions;
using Xunit;

namespace TradyStrat.Cli.Tests.Mcp.Mapping;

public class SuggestionMapperTests
{
    private static readonly DateOnly From = new(2026, 1, 1);
    private static readonly DateOnly To   = new(2026, 5, 18);
    private const string Instrument = "CON3.L";

    private static QueriedSuggestion MakeSuggestion(
        string? envelopeHash = "abcdef1234567890",
        string? promptVersionHash = "feedbeef12345678")
        => new(
            Date: new DateOnly(2026, 5, 1),
            Action: SuggestionAction.Acquire,
            Conviction: 7,
            Reasoning: "Bullish momentum",
            EnvelopeHash: envelopeHash,
            PromptVersionHash: promptVersionHash,
            ForwardReturnPct: 3.14m,
            Correct: true);

    [Fact]
    public void Maps_query_output_to_page_with_truncated_hashes()
    {
        var output = new QuerySuggestionsOutput(Items: [MakeSuggestion()]);

        var page = SuggestionMapper.ToPage(output, Instrument, From, To);

        page.Instrument.ShouldBe(Instrument);
        page.From.ShouldBe(From);
        page.To.ShouldBe(To);
        page.Count.ShouldBe(1);
        page.Items.Count.ShouldBe(1);

        var row = page.Items[0];
        row.Date.ShouldBe(new DateOnly(2026, 5, 1));
        row.Action.ShouldBe("Acquire");
        row.Conviction.ShouldBe(7);
        row.Reasoning.ShouldBe("Bullish momentum");
        row.EnvelopeHash.ShouldBe("abcdef12");
        row.PromptVersionHash.ShouldBe("feedbeef");
        row.ForwardReturnPct.ShouldBe(3.14m);
        row.Correct.ShouldBe(true);
    }

    [Fact]
    public void Null_hashes_stay_null()
    {
        var output = new QuerySuggestionsOutput(Items: [MakeSuggestion(envelopeHash: null, promptVersionHash: null)]);

        var page = SuggestionMapper.ToPage(output, Instrument, From, To);

        var row = page.Items[0];
        row.EnvelopeHash.ShouldBeNull();
        row.PromptVersionHash.ShouldBeNull();
    }
}
