using TradyStrat.Application.PriceFeed.UseCases;
using TradyStrat.Cli.Mcp.Dto;

namespace TradyStrat.Cli.Mcp.Mapping;

internal static class PriceMapper
{
    public static PriceSeries ToSeries(GetPriceSeriesOutput src, string ticker)
    {
        var bars = src.Bars
            .Select(b => new BarDto(b.Date, b.Open, b.High, b.Low, b.Close, b.Volume))
            .ToList();
        var from = bars.Count > 0 ? bars[0].Date : DateOnly.MinValue;
        var to   = bars.Count > 0 ? bars[^1].Date : DateOnly.MinValue;
        var indicators = src.Indicators is null ? null : new IndicatorArraysDto(
            Rsi: src.Indicators.Rsi,
            BollingerMid: src.Indicators.BollingerMid,
            Sma200: src.Indicators.Sma200,
            Ichimoku: src.Indicators.Ichimoku);
        return new PriceSeries(ticker, from, to, bars.Count, bars, indicators);
    }
}
