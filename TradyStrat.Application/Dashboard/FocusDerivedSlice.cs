using TradyStrat.Application.AiSuggestion.CallDiff;
using TradyStrat.Domain.Indicators;
using TradyStrat.Application.Indicators;
using TradyStrat.Domain.Suggestions;
using TradyStrat.Domain;

namespace TradyStrat.Application.Dashboard;

/// <summary>
/// Focus-specific derived data computed from a single Suggestion: the
/// call-diff against the prior suggestion, citation-keyed indicator
/// histories, and the deserialized market snapshot. Built late (after
/// the focus suggestion arrives) by <see cref="UseCases.BuildFocusDerivedSliceUseCase"/>.
/// </summary>
public sealed record FocusDerivedSlice(
    CallDiff CallDiff,
    IReadOnlyDictionary<(string Ticker, IndicatorKind Kind), IndicatorSeries> IndicatorHistories,
    MarketSnapshot MarketSnapshot)
{
    public static readonly FocusDerivedSlice Empty = new(
        CallDiff.None,
        new Dictionary<(string, IndicatorKind), IndicatorSeries>(),
        MarketSnapshot.Empty);
}
