using TradyStrat.Domain.Suggestions;
using TradyStrat.Domain;

namespace TradyStrat.Application.AiSuggestion.UseCases;

public sealed record ReplayReport(
    int InstrumentId,
    DateOnly Since,
    DateOnly Until,
    IReadOnlyList<ReplayedSuggestion> Rows,
    IReadOnlyDictionary<SuggestionAction, ActionAggregate> PerAction,
    ActionAggregate Overall,
    decimal ConvictionWeightedScore,
    IReadOnlyList<string> DistinctPromptVersionHashes);

public sealed record ReplayedSuggestion(
    DateOnly ForDate,
    SuggestionAction Action,
    int Conviction,
    decimal FwdReturnPct,
    bool WasCorrect,
    bool IsForwardWindowComplete,
    string PromptVersionHash);

public sealed record ActionAggregate(
    int Count,
    decimal HitRatePct,
    decimal AvgFwdReturnPct,
    decimal AvgConviction);
