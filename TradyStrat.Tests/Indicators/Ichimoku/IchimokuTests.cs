using Shouldly;
using TradyStrat.Domain;
using Xunit;

namespace TradyStrat.Tests.Indicators.Ichimoku;

public class IchimokuTests
{
    private static PriceBar[] Series(IEnumerable<(decimal h, decimal l, decimal c)> rows)
        => rows.Select((r, i) => new PriceBar
        {
            Id = 0, Ticker = "X", Date = new DateOnly(2025,1,1).AddDays(i),
            Open = r.c, High = r.h, Low = r.l, Close = r.c, Volume = 1
        }).ToArray();

    [Fact]
    public void Returns_null_when_fewer_than_78_bars()
    {
        var bars = Series(Enumerable.Range(0, 50).Select(_ => (10m, 8m, 9m)));
        Application.Indicators.Ichimoku.Ichimoku.LatestFor(bars).ShouldBeNull();
    }

    [Fact]
    public void Computes_Tenkan_as_midpoint_of_last_9_high_low()
    {
        var bars = SeriesLoader.LoadCloses();
        var ichi = Application.Indicators.Ichimoku.Ichimoku.LatestFor(bars);
        ichi.ShouldNotBeNull();

        var last9 = bars.TakeLast(9).ToList();
        var expected = (last9.Max(b => b.High) + last9.Min(b => b.Low)) / 2m;

        ichi.Tenkan.ShouldBe(expected, tolerance: 0.0001m);
    }

    [Fact]
    public void Signal_AboveCloud_when_price_exceeds_max_of_spans()
    {
        var bars = Series(Enumerable.Range(0, 100).Select(i =>
            i < 99 ? (5m, 4m, 4.5m) : (50m, 49m, 49.5m)));

        var ichi = Application.Indicators.Ichimoku.Ichimoku.LatestFor(bars);

        ichi.ShouldNotBeNull();
        ichi.Signal.ShouldBe(IchimokuSignal.AboveCloud);
    }

    [Fact]
    public void Signal_BelowCloud_when_price_under_min_of_spans()
    {
        var bars = Series(Enumerable.Range(0, 100).Select(i =>
            i < 99 ? (50m, 49m, 49.5m) : (5m, 4m, 4.5m)));

        var ichi = Application.Indicators.Ichimoku.Ichimoku.LatestFor(bars);

        ichi.ShouldNotBeNull();
        ichi.Signal.ShouldBe(IchimokuSignal.BelowCloud);
    }
}
