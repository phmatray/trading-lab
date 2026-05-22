using System.Text.Json;
using Ardalis.Specification;
using TradyStrat.Application.AiSuggestion;
using TradyStrat.Application.AiSuggestion.CallDiff;
using TradyStrat.Application.AiSuggestion.Specifications;
using TradyStrat.Application.Indicators;
using TradyStrat.Application.PredictionMarkets;
using TradyStrat.Domain;

namespace TradyStrat.Application.Dashboard.UseCases;

/// <summary>
/// Computes the focus-only derived slice from a single suggestion:
/// CallDiff vs the immediately prior suggestion, citation-keyed indicator
/// histories, and the deserialized market snapshot. Designed to be called
/// after the focus suggestion arrives (i.e. after the dashboard skeleton
/// has already rendered).
/// </summary>
public sealed partial class BuildFocusDerivedSliceUseCase(
    IReadRepositoryBase<Suggestion> suggestionRepo,
    IIndicatorEngine indicators,
    ILogger<BuildFocusDerivedSliceUseCase> log)
{
    private const int SparklineWindow = 30;

    public async Task<FocusDerivedSlice> BuildAsync(
        Suggestion focus,
        DateOnly targetDate,
        CancellationToken ct)
    {
        // 1. Prior + CallDiff
        var prior = await suggestionRepo.FirstOrDefaultAsync(
            new PriorSuggestionSpec(targetDate, focus.InstrumentId), ct);

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

        // 3. Market snapshot — JSON is best-effort; malformed → Empty + log.
        var marketSnap = MarketSnapshot.Empty;
        if (focus.MarketSnapshotJson is { Length: > 0 } marketJson)
        {
            try
            {
                marketSnap = JsonSerializer.Deserialize<MarketSnapshot>(marketJson, JsonOpts.Strict)
                             ?? MarketSnapshot.Empty;
            }
            catch (JsonException ex)
            {
                LogMarketSnapshotMalformed(log, ex);
            }
        }

        return new FocusDerivedSlice(callDiff, histories, marketSnap);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "MarketSnapshotJson malformed; rail will not render")]
    private static partial void LogMarketSnapshotMalformed(ILogger logger, Exception ex);
}
