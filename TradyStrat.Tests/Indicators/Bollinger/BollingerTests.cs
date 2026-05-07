using Shouldly;
using TradyStrat.Features.Indicators;
using Xunit;

namespace TradyStrat.Tests.Indicators;

public class BollingerTests
{
    [Fact]
    public void Returns_null_when_series_shorter_than_period()
    {
        var bars = SeriesLoader.LoadCloses().Take(10).ToList();
        Bollinger.LatestFor(bars).ShouldBeNull();
    }

    [Fact]
    public void Latest_band_lies_around_mean_with_positive_sigma()
    {
        var bars = SeriesLoader.LoadCloses();
        var bb = Bollinger.LatestFor(bars);

        bb.ShouldNotBeNull();
        bb.Lower.ShouldBeLessThan(bb.Middle);
        bb.Middle.ShouldBeLessThan(bb.Upper);
        bb.Sigma.ShouldBeGreaterThan(0m);
    }
}
