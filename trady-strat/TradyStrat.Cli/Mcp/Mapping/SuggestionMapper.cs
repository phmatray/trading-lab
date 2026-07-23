using TradyStrat.Application.AiSuggestion.UseCases;
using TradyStrat.Cli.Mcp.Dto;

namespace TradyStrat.Cli.Mcp.Mapping;

internal static class SuggestionMapper
{
    public static SuggestionPage ToPage(QuerySuggestionsOutput src, string instrument, DateOnly from, DateOnly to)
        => new(
            Instrument: instrument,
            From: from,
            To: to,
            Count: src.Items.Count,
            Items: src.Items.Select(ToRow).ToList());

    private static SuggestionRow ToRow(QueriedSuggestion s)
        => new(
            Date: s.Date,
            Action: s.Action.ToString(),
            Conviction: s.Conviction,
            EnvelopeHash: Truncate(s.EnvelopeHash),
            PromptVersionHash: Truncate(s.PromptVersionHash),
            Reasoning: s.Reasoning,
            ForwardReturnPct: s.ForwardReturnPct,
            Correct: s.Correct);

    private static string? Truncate(string? h) => h is null ? null : (h.Length >= 8 ? h[..8] : h);
}
