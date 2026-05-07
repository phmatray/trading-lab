using Shouldly;
using Xunit;

namespace TradyStrat.Tests.Indicators.MovingAverage;

public class MovingAverageTests
{
    [Fact]
    public void Returns_null_when_period_exceeds_series_length()
    {
        var bars = SeriesLoader.LoadCloses().Take(40).ToList();
        Features.Indicators.MovingAverage.MovingAverage.LatestFor(bars, period: 50).ShouldBeNull();
    }

    [Fact]
    public void Sma50_close_to_average_of_last_50_closes()
    {
        var bars = SeriesLoader.LoadCloses();
        var sma  = Features.Indicators.MovingAverage.MovingAverage.LatestFor(bars, 50);
        var manual = bars.TakeLast(50).Average(b => b.Close);

        sma.ShouldNotBeNull();
        sma.Value.ShouldBe(manual, tolerance: 0.0001m);
    }
}
