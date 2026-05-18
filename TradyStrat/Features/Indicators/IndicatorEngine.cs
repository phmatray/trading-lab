using Ardalis.Specification;
using TradyStrat.Features.Indicators.Zones;
using TradyStrat.Features.Indicators.History;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Features.PriceFeed.Specifications;

namespace TradyStrat.Features.Indicators;

public sealed class IndicatorEngine(
    IReadRepositoryBase<PriceBar> bars,
    ZoneClassifier classifier,
    IIndicatorHistoryProviderFactory historyFactory)
{
    public async Task<IndicatorReading> ComputeFor(string ticker, CancellationToken ct)
    {
        var series = await bars.ListAsync(new PriceBarsForTickerSpec(ticker), ct);
        return ComputeFromSeries(ticker, series);
    }

    public async Task<IndicatorReading> ComputeFor(string ticker, DateOnly asOf, CancellationToken ct)
    {
        var series = await bars.ListAsync(new PriceBarsAsOfSpec(ticker, asOf), ct);
        return ComputeFromSeries(ticker, series);
    }

    public async Task<IndicatorSeries> HistoryFor(
        string ticker, IndicatorKind kind, int lastN, CancellationToken ct)
    {
        var series = await bars.ListAsync(new PriceBarsForTickerSpec(ticker), ct);
        return historyFactory.For(kind).Compute(series, lastN);
    }

    public async Task<IndicatorSeries> HistoryFor(
        string ticker, IndicatorKind kind, int lastN, DateOnly asOf, CancellationToken ct)
    {
        var series = await bars.ListAsync(new PriceBarsAsOfSpec(ticker, asOf), ct);
        return historyFactory.For(kind).Compute(series, lastN);
    }

    private IndicatorReading ComputeFromSeries(string ticker, List<PriceBar> series)
    {
        if (series.Count == 0)
            throw new IndicatorComputationException($"No price bars for {ticker}");

        var price  = series[^1].Close;
        var bundle = new IndicatorBundle(
            Bollinger.Bollinger.LatestFor(series),
            Rsi.Rsi.LatestFor(series),
            MovingAverage.MovingAverage.LatestFor(series, 50),
            MovingAverage.MovingAverage.LatestFor(series, 200),
            Ichimoku.Ichimoku.LatestFor(series));

        var (zone, reasons) = classifier.Classify(price, bundle);
        return new IndicatorReading(ticker, price,
            bundle.Bollinger, bundle.Rsi, bundle.Sma50, bundle.Sma200, bundle.Ichimoku,
            zone, reasons);
    }
}
