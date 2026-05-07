using Shouldly;
using TradyStrat.Common.Domain;
using TradyStrat.Features.Indicators.MovingAverage;
using Xunit;

namespace TradyStrat.Tests.Indicators.MovingAverage;

public class Sma200HistoryProviderTests
{
    private static List<PriceBar> Bars(int n) =>
        Enumerable.Range(0, n).Select(i => new PriceBar
        {
            Id = i + 1, Ticker = "T",
            Date = new DateOnly(2025, 1, 1).AddDays(i),
            Open = 100m, High = 101m, Low = 99m, Close = 100m + i * 0.1m, Volume = 1000,
        }).ToList();

    [Fact]
    public void Kind_is_Sma200()
        => new Sma200HistoryProvider().Kind.ShouldBe(IndicatorKind.Sma200);

    [Fact]
    public void Returns_lastN_with_threshold_at_last_value()
    {
        var s = new Sma200HistoryProvider().Compute(Bars(250), 30);

        s.Values.Count.ShouldBe(30);
        s.ThresholdHi.ShouldBe(s.Values[^1]);
        s.ThresholdLo.ShouldBeNull();
    }

    [Fact]
    public void Returns_empty_when_fewer_than_period_bars()
        => new Sma200HistoryProvider().Compute(Bars(50), 30).Values.ShouldBeEmpty();
}
