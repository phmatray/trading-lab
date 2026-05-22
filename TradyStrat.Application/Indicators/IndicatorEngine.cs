using Ardalis.Specification;
using TradyStrat.Application.Indicators.Zones;
using TradyStrat.Application.Indicators.History;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Shared;
using TradyStrat.Application.PriceFeed.Specifications;

namespace TradyStrat.Application.Indicators;

public sealed class IndicatorEngine(
    IReadRepositoryBase<PriceBar> bars,
    ZoneClassifier classifier,
    IIndicatorHistoryProviderFactory historyFactory) : IIndicatorEngine
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
        var rawRsi = Rsi.Rsi.LatestFor(series);
        var bundle = new IndicatorBundle(
            Bollinger.Bollinger.LatestFor(series)            ?? BollingerReading.Empty,
            rawRsi is { } r ? Percentage.Of(r) : Percentage.Empty,
            MovingAverage.MovingAverage.LatestFor(series, 50)  ?? 0m,
            MovingAverage.MovingAverage.LatestFor(series, 200) ?? 0m,
            Ichimoku.Ichimoku.LatestFor(series)              ?? IchimokuReading.Empty);

        var (zone, reasons) = classifier.Classify(price, bundle);
        return new IndicatorReading(ticker, price,
            bundle.Bollinger, bundle.Rsi, bundle.Sma50, bundle.Sma200, bundle.Ichimoku,
            zone, reasons);
    }
}
