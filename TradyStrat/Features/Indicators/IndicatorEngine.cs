using Ardalis.Specification;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;
using TradyStrat.Specifications.PriceBars;

namespace TradyStrat.Features.Indicators;

public sealed class IndicatorEngine(
    IReadRepositoryBase<PriceBar> bars,
    ZoneClassifier classifier)
{
    public async Task<IndicatorReading> ComputeFor(string ticker, CancellationToken ct)
    {
        var series = await bars.ListAsync(new PriceBarsForTickerSpec(ticker), ct);
        if (series.Count == 0)
            throw new IndicatorComputationException($"No price bars for {ticker}");

        var price  = series[^1].Close;
        var bundle = new IndicatorBundle(
            Bollinger.LatestFor(series),
            Rsi.LatestFor(series),
            MovingAverage.LatestFor(series, 50),
            MovingAverage.LatestFor(series, 200),
            Ichimoku.LatestFor(series));

        var (zone, reasons) = classifier.Classify(price, bundle);

        return new IndicatorReading(
            ticker, price,
            bundle.Bollinger, bundle.Rsi, bundle.Sma50, bundle.Sma200, bundle.Ichimoku,
            zone, reasons);
    }
}
