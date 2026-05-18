using TradyStrat.Domain;

namespace TradyStrat.Application.AiSuggestion.UseCases;

public sealed record QuerySuggestionsOutput(IReadOnlyList<QueriedSuggestion> Items);

public sealed record QueriedSuggestion(
    DateOnly Date,
    SuggestionAction Action,
    int Conviction,
    string Reasoning,
    string? EnvelopeHash,
    string? PromptVersionHash,
    decimal? ForwardReturnPct,
    bool? Correct);
