using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Application.Indicators.Bollinger;
using Xunit;

namespace TradyStrat.Application.Tests.Indicators.Bollinger;

public class BollingerHistoryProviderTests
{
    private static List<PriceBar> Bars(int n) =>
        Enumerable.Range(0, n).Select(i => new PriceBar
        {
            Id = i + 1, Ticker = "T",
            Date = new DateOnly(2026, 1, 1).AddDays(i),
            Open = 100m, High = 101m, Low = 99m,
            Close = 100m + (decimal)Math.Sin(i * 0.4) * 2m,
            Volume = 1000,
        }).ToList();

    [Fact]
    public void Kind_is_Bollinger()
        => new BollingerHistoryProvider().Kind.ShouldBe(IndicatorKind.Bollinger);

    [Fact]
    public void Returns_middle_band_with_hi_lo_thresholds_at_last_bar()
    {
        var s = new BollingerHistoryProvider().Compute(Bars(60), 30);

        s.Values.Count.ShouldBe(30);
        s.ThresholdHi.ShouldNotBeNull();
        s.ThresholdLo.ShouldNotBeNull();
        s.ThresholdHi.Value.ShouldBeGreaterThan(s.ThresholdLo!.Value);
    }

    [Fact]
    public void Returns_empty_when_insufficient_bars()
        => new BollingerHistoryProvider().Compute(Bars(10), 30).Values.ShouldBeEmpty();
}
