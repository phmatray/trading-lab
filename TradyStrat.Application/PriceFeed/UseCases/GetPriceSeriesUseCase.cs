using Ardalis.Specification;
using Microsoft.Extensions.Logging;
using TradyStrat.Application.Indicators;
using TradyStrat.Application.PriceFeed.Specifications;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;

namespace TradyStrat.Application.PriceFeed.UseCases;

/// <summary>
/// Returns OHLCV bars for a ticker over a date range, optionally with indicator value arrays
/// aligned index-wise to the returned bars.
///
/// Alignment strategy: <see cref="IIndicatorEngine.HistoryFor(string,IndicatorKind,int,DateOnly,CancellationToken)"/>
/// fetches all bars for the ticker up to <c>asOf</c> (chronological order) and returns the
/// last <c>lastN</c> computed values.  By requesting <c>lastN = totalBarsUpToTo</c> and then
/// slicing the resulting array to the range window, we get values that correspond 1-to-1 to
/// the range bars — without needing per-date indexing that <see cref="IndicatorSeries"/> does
/// not expose.
///
/// Limitation: Sma50 is defined in <see cref="IndicatorKind"/> but has no registered history
/// provider; it is omitted from <see cref="IndicatorArrays"/>.
/// </summary>
public sealed class GetPriceSeriesUseCase(
    IReadRepositoryBase<PriceBar> bars,
    IIndicatorEngine indicators,
    ILogger<GetPriceSeriesUseCase> logger)
    : UseCaseBase<GetPriceSeriesInput, GetPriceSeriesOutput>(logger)
{
    protected override async Task<GetPriceSeriesOutput> ExecuteCore(
        GetPriceSeriesInput input, CancellationToken ct)
    {
        var range = await bars.ListAsync(
            new PriceBarsInRangeSpec(input.Ticker, input.From, input.To), ct);

        if (!input.WithIndicators || range.Count == 0)
            return new GetPriceSeriesOutput(range, Indicators: null);

        // Determine how many total bars exist for this ticker up to input.To.
        // We need at least this many values from HistoryFor so that the last
        // range.Count values align with our range bars.
        var totalBars = await bars.ListAsync(
            new PriceBarsAsOfSpec(input.Ticker, input.To), ct);
        var totalN = totalBars.Count;

        var rsi         = await AlignedAsync(input.Ticker, IndicatorKind.Rsi,      totalN, input.To, range.Count, ct);
        var bollingerMid= await AlignedAsync(input.Ticker, IndicatorKind.Bollinger, totalN, input.To, range.Count, ct);
        var sma200      = await AlignedAsync(input.Ticker, IndicatorKind.Sma200,   totalN, input.To, range.Count, ct);
        var ichimoku    = await AlignedAsync(input.Ticker, IndicatorKind.Ichimoku,  totalN, input.To, range.Count, ct);

        return new GetPriceSeriesOutput(range, new IndicatorArrays(
            Rsi:          rsi,
            BollingerMid: bollingerMid,
            Sma200:       sma200,
            Ichimoku:     ichimoku));
    }

    /// <summary>
    /// Fetches an indicator series for all bars up to <paramref name="asOf"/>, then returns
    /// a <paramref name="windowSize"/>-element array whose values correspond to the last
    /// <paramref name="windowSize"/> bars.  Positions without a computed value (warmup) are null.
    /// </summary>
    private async Task<IReadOnlyList<decimal?>> AlignedAsync(
        string ticker,
        IndicatorKind kind,
        int totalN,
        DateOnly asOf,
        int windowSize,
        CancellationToken ct)
    {
        // Request all computed values (up to totalN) so we don't truncate the history.
        var series = await indicators.HistoryFor(ticker, kind, totalN, asOf, ct);

        // series.Values contains the last min(totalN, nb) computed values in chronological order.
        // nb = total computed bars = totalN - warmup.
        // The computed values correspond to bars[warmup .. totalN-1] (0-based).
        // Our range is the last windowSize of the totalN bars, i.e. bars[totalN-windowSize .. totalN-1].
        // Overlap between computed values and range:
        //   computedStart = totalN - series.Values.Count   (index in total bar list)
        //   rangeStart    = totalN - windowSize
        // For each range position p in [0, windowSize):
        //   barIndex = rangeStart + p = totalN - windowSize + p
        //   computedOffset = barIndex - computedStart = p - (windowSize - series.Values.Count)

        var nb          = series.Values.Count;
        var computedStart = totalN - nb;          // first bar index that has a computed value
        var rangeStart    = totalN - windowSize;  // first bar index in the range

        var result = new decimal?[windowSize];
        for (int p = 0; p < windowSize; p++)
        {
            var barIndex      = rangeStart + p;
            var computedIndex = barIndex - computedStart;
            if (computedIndex >= 0 && computedIndex < nb)
                result[p] = series.Values[computedIndex];
            // else: stays null (warmup / beyond computed range)
        }

        return result;
    }
}
