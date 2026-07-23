using Shouldly;
using TradyStrat.Domain.Indicators;
using TradyStrat.TestKit.Indicators;
using Xunit;

namespace TradyStrat.Application.Tests.Indicators.Rsi;

public class RsiTests
{
    [Fact]
    public void Returns_null_when_too_few_bars()
    {
        var bars = SeriesLoader.LoadCloses().Take(5).ToList();
        global::TradyStrat.Domain.Indicators.Rsi.LatestFor(bars).ShouldBeNull();
    }

    [Fact]
    public void Returns_value_between_0_and_100()
    {
        var rsi = global::TradyStrat.Domain.Indicators.Rsi.LatestFor(SeriesLoader.LoadCloses());
        rsi.ShouldNotBeNull();
        rsi.Value.ShouldBeInRange(0m, 100m);
    }
}
