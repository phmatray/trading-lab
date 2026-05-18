using Shouldly;
using TradyStrat.Application.PriceFeed.UseCases;
using TradyStrat.Cli.Mcp.Mapping;
using TradyStrat.Domain;
using Xunit;

namespace TradyStrat.Cli.Tests.Mcp.Mapping;

public class PriceMapperTests
{
    private const string Ticker = "CON3.L";

    private static PriceBar MakeBar(DateOnly date, decimal close = 100m, long volume = 1_000L)
        => new()
        {
            Id = 1,
            Ticker = Ticker,
            Date = date,
            Open = close - 1m,
            High = close + 2m,
            Low = close - 2m,
            Close = close,
            Volume = volume,
        };

    [Fact]
    public void Maps_bars_without_indicators()
    {
        var bar1 = MakeBar(new DateOnly(2026, 1, 2), close: 98m, volume: 2_000L);
        var bar2 = MakeBar(new DateOnly(2026, 1, 3), close: 102m, volume: 3_000L);
        var output = new GetPriceSeriesOutput(Bars: [bar1, bar2], Indicators: null);

        var series = PriceMapper.ToSeries(output, Ticker);

        series.Instrument.ShouldBe(Ticker);
        series.BarCount.ShouldBe(2);
        series.From.ShouldBe(new DateOnly(2026, 1, 2));
        series.To.ShouldBe(new DateOnly(2026, 1, 3));
        series.Indicators.ShouldBeNull();

        series.Bars[0].Date.ShouldBe(new DateOnly(2026, 1, 2));
        series.Bars[0].Open.ShouldBe(97m);
        series.Bars[0].High.ShouldBe(100m);
        series.Bars[0].Low.ShouldBe(96m);
        series.Bars[0].Close.ShouldBe(98m);
        series.Bars[0].Volume.ShouldBe(2_000L);

        series.Bars[1].Close.ShouldBe(102m);
        series.Bars[1].Volume.ShouldBe(3_000L);
    }

    [Fact]
    public void Maps_indicator_arrays_aligned_to_bars()
    {
        var bars = Enumerable.Range(1, 5)
            .Select(i => MakeBar(new DateOnly(2026, 1, i + 1), close: 100m + i))
            .ToArray();

        var rsi    = new decimal?[] { null, null, 45m, 50m, 55m };
        var bollMid = new decimal?[] { null, null, 99m, 101m, 103m };
        var sma200 = new decimal?[] { null, null, null, null, 92m };
        var ichimoku = new decimal?[] { 100m, 101m, 102m, 103m, 104m };

        var indicators = new IndicatorArrays(
            Rsi: rsi,
            BollingerMid: bollMid,
            Sma200: sma200,
            Ichimoku: ichimoku);

        var output = new GetPriceSeriesOutput(Bars: bars, Indicators: indicators);

        var series = PriceMapper.ToSeries(output, Ticker);

        series.BarCount.ShouldBe(5);
        series.Indicators.ShouldNotBeNull();
        series.Indicators!.Rsi.Count.ShouldBe(5);
        series.Indicators.BollingerMid.Count.ShouldBe(5);
        series.Indicators.Sma200.Count.ShouldBe(5);
        series.Indicators.Ichimoku.Count.ShouldBe(5);

        // Spot-check values
        series.Indicators.Rsi[0].ShouldBeNull();
        series.Indicators.Rsi[2].ShouldBe(45m);
        series.Indicators.Rsi[4].ShouldBe(55m);

        series.Indicators.BollingerMid[1].ShouldBeNull();
        series.Indicators.BollingerMid[3].ShouldBe(101m);

        series.Indicators.Sma200[3].ShouldBeNull();
        series.Indicators.Sma200[4].ShouldBe(92m);

        series.Indicators.Ichimoku[0].ShouldBe(100m);
        series.Indicators.Ichimoku[4].ShouldBe(104m);
    }
}
