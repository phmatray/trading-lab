using Shouldly;
using TradyStrat.Features.Indicators;
using Xunit;

namespace TradyStrat.Tests.Indicators;

public class RsiTests
{
    [Fact]
    public void Returns_null_when_too_few_bars()
    {
        var bars = SeriesLoader.LoadCloses().Take(5).ToList();
        Rsi.LatestFor(bars).ShouldBeNull();
    }

    [Fact]
    public void Returns_value_between_0_and_100()
    {
        var rsi = Rsi.LatestFor(SeriesLoader.LoadCloses());
        rsi.ShouldNotBeNull();
        rsi.Value.ShouldBeInRange(0m, 100m);
    }
}
