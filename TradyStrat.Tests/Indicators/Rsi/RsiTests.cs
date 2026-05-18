using Shouldly;
using Xunit;

namespace TradyStrat.Tests.Indicators.Rsi;

public class RsiTests
{
    [Fact]
    public void Returns_null_when_too_few_bars()
    {
        var bars = SeriesLoader.LoadCloses().Take(5).ToList();
        Application.Indicators.Rsi.Rsi.LatestFor(bars).ShouldBeNull();
    }

    [Fact]
    public void Returns_value_between_0_and_100()
    {
        var rsi = Application.Indicators.Rsi.Rsi.LatestFor(SeriesLoader.LoadCloses());
        rsi.ShouldNotBeNull();
        rsi.Value.ShouldBeInRange(0m, 100m);
    }
}
