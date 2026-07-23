using TradyStrat.Domain.Suggestions;
using TradyStrat.Domain;

namespace TradyStrat.Application.AiSuggestion.Snapshot;

public sealed record PastSuggestionRow(
    DateOnly Date,
    SuggestionAction Action,
    int Conviction,
    decimal FwdReturnPct,
    bool WasCorrect,
    bool IsForwardWindowComplete,
    decimal? NetTradeFlowEur,
    string RationaleHeadline);
