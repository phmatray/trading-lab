using Shouldly;
using TradyStrat.Features.Indicators.Rules;
using TradyStrat.Shared.Domain;
using Xunit;

namespace TradyStrat.Tests.Indicators.Rules;

public class ZoneRuleTests
{
    private static IndicatorBundle Bundle(BollingerReading? bb = null,
        decimal? rsi = null, decimal? sma50 = null, decimal? sma200 = null,
        IchimokuReading? ich = null) => new(bb, rsi, sma50, sma200, ich);

    [Fact]
    public void Bollinger_below_lower_votes_Accumulate()
    {
        var bb = new BollingerReading(Upper: 5m, Middle: 4m, Lower: 3m, Sigma: 1m);
        var v = new BollingerZoneRule().Apply(price: 2.5m, Bundle(bb: bb));
        v.ShouldNotBeNull();
        v.Vote.ShouldBe(Zone.Accumulate);
    }

    [Fact]
    public void Bollinger_above_upper_votes_Distribute()
    {
        var bb = new BollingerReading(5m, 4m, 3m, 1m);
        new BollingerZoneRule().Apply(5.5m, Bundle(bb: bb))!.Vote.ShouldBe(Zone.Distribute);
    }

    [Fact]
    public void Bollinger_returns_null_when_reading_missing()
    {
        new BollingerZoneRule().Apply(4m, Bundle()).ShouldBeNull();
    }

    [Theory]
    [InlineData(20, Zone.Accumulate)]
    [InlineData(50, Zone.Hold)]
    [InlineData(80, Zone.Distribute)]
    public void Rsi_thresholds(int rsi, Zone expected)
    {
        new RsiZoneRule().Apply(0m, Bundle(rsi: rsi))!.Vote.ShouldBe(expected);
    }

    [Fact]
    public void MovingAverage_below_200_votes_Accumulate()
    {
        new MovingAverageZoneRule().Apply(3m, Bundle(sma50: 4m, sma200: 5m))!.Vote.ShouldBe(Zone.Accumulate);
    }

    [Fact]
    public void MovingAverage_above_50_votes_Distribute()
    {
        new MovingAverageZoneRule().Apply(6m, Bundle(sma50: 4m, sma200: 5m))!.Vote.ShouldBe(Zone.Distribute);
    }

    [Fact]
    public void MovingAverage_between_holds()
    {
        new MovingAverageZoneRule().Apply(4.5m, Bundle(sma50: 5m, sma200: 4m))!.Vote.ShouldBe(Zone.Hold);
    }

    [Theory]
    [InlineData(IchimokuSignal.AboveCloud, Zone.Distribute)]
    [InlineData(IchimokuSignal.BelowCloud, Zone.Accumulate)]
    [InlineData(IchimokuSignal.InCloud,    Zone.Hold)]
    public void Ichimoku_maps_signal_to_zone(IchimokuSignal s, Zone z)
    {
        var ich = new IchimokuReading(0,0,0,0,0, s);
        new IchimokuZoneRule().Apply(0m, Bundle(ich: ich))!.Vote.ShouldBe(z);
    }
}
