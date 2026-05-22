using TradyStrat.Application.AiSuggestion;
using TradyStrat.Domain.Indicators.Services;
using TradyStrat.Domain.Indicators;
using TradyStrat.Application.AiSuggestion.CallDiff;
using TradyStrat.Application.Indicators;
using TradyStrat.Domain;
using TradyStrat.Domain.Suggestions;

namespace TradyStrat.Application.Dashboard.UseCases;

/// <summary>
/// Computes the focus-only derived slice from a single suggestion:
/// CallDiff vs the immediately prior suggestion, citation-keyed indicator
/// histories, and the market snapshot. Designed to be called after the focus
/// suggestion arrives (i.e. after the dashboard skeleton has already rendered).
/// </summary>
public sealed class BuildFocusDerivedSliceUseCase(
    ISuggestionRepository suggestions,
    IIndicatorEngine indicators)
{
    private const int SparklineWindow = 30;

    public async Task<FocusDerivedSlice> BuildAsync(
        Suggestion focus,
        DateOnly targetDate,
        CancellationToken ct)
    {
        // 1. Prior + CallDiff
        var prior = await suggestions.PriorToAsync(focus.InstrumentId, targetDate, ct);

        var callDiff = new CallDiffBuilder()
            .WithToday(focus)
            .WithPrior(prior)
            .Build();

        // 2. Indicator histories per citation (de-duped by (ticker, kind))
        var histories = new Dictionary<(string Ticker, IndicatorKind Kind), IndicatorSeries>();
        foreach (var c in focus.Citations)
        {
            var kind = IndicatorKindParser.From(c.Indicator);
            if (kind is null) continue;
            var key = (c.Ticker, kind.Value);
            if (histories.ContainsKey(key)) continue;
            histories[key] = await indicators.HistoryFor(c.Ticker, kind.Value, SparklineWindow, targetDate, ct);
        }

        // 3. Market snapshot — VO field on the AR, no more JSON deserialization.
        return new FocusDerivedSlice(callDiff, histories, focus.Snapshot);
    }
}
