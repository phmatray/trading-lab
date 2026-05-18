using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Application.Indicators.Ichimoku;
using Xunit;

namespace TradyStrat.Tests.Indicators.Ichimoku;

public class IchimokuHistoryProviderTests
{
    private static List<PriceBar> Bars(int n) =>
        Enumerable.Range(0, n).Select(i => new PriceBar
        {
            Id = i + 1, Ticker = "T",
            Date = new DateOnly(2026, 1, 1).AddDays(i),
            Open = 100m, High = 101m, Low = 99m,
            Close = 100m + i * 0.1m,
            Volume = 1000,
        }).ToList();

    [Fact]
    public void Kind_is_Ichimoku()
        => new IchimokuHistoryProvider().Kind.ShouldBe(IndicatorKind.Ichimoku);

    [Fact]
    public void Returns_lastN_close_prices_with_no_thresholds()
    {
        var s = new IchimokuHistoryProvider().Compute(Bars(80), 30);

        s.Values.Count.ShouldBe(30);
        s.ThresholdHi.ShouldBeNull();
        s.ThresholdLo.ShouldBeNull();
        s.Values[^1].ShouldBe(100m + 79 * 0.1m);  // last close price
    }

    [Fact]
    public void Returns_empty_when_no_bars()
        => new IchimokuHistoryProvider().Compute([], 30).Values.ShouldBeEmpty();
}
