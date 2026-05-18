using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Features.Indicators.Rsi;
using Xunit;

namespace TradyStrat.Tests.Indicators.Rsi;

public class RsiHistoryProviderTests
{
    private static List<PriceBar> Bars(int n)
    {
        var list = new List<PriceBar>(n);
        var d = new DateOnly(2026, 1, 1);
        for (int i = 0; i < n; i++)
        {
            // Small alternating walk to give RSI something to compute.
            var c = 100m + (i % 2 == 0 ? i * 0.5m : -i * 0.3m);
            list.Add(new PriceBar
            {
                Id = i + 1,
                Ticker = "T",
                Date = d.AddDays(i),
                Open = c, High = c + 1m, Low = c - 1m, Close = c, Volume = 1000,
            });
        }
        return list;
    }

    [Fact]
    public void Kind_is_Rsi()
        => new RsiHistoryProvider().Kind.ShouldBe(IndicatorKind.Rsi);

    [Fact]
    public void Returns_lastN_values_with_70_30_thresholds()
    {
        var s = new RsiHistoryProvider().Compute(Bars(50), 20);

        s.Values.Count.ShouldBe(20);
        s.ThresholdHi.ShouldBe(70m);
        s.ThresholdLo.ShouldBe(30m);
        s.Values.ShouldAllBe(v => v >= 0m && v <= 100m);
    }

    [Fact]
    public void Returns_truncated_when_insufficient_history()
    {
        // Only 10 bars: RSI(14) cannot produce 20 values.
        var s = new RsiHistoryProvider().Compute(Bars(10), 20);
        s.Values.Count.ShouldBeLessThan(20);
    }

    [Fact]
    public void Returns_empty_when_no_bars()
        => new RsiHistoryProvider().Compute([], 20).Values.ShouldBeEmpty();
}
