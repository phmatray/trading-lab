using Shouldly;
using TradyStrat.Application.Indicators.Bollinger;
using TradyStrat.Application.Indicators.Ichimoku;
using TradyStrat.Application.Indicators.MovingAverage;
using TradyStrat.Application.Indicators.Rsi;
using TradyStrat.Domain;
using TradyStrat.Domain.Indicators.Services;
using TradyStrat.Domain.Shared.Money;
using Xunit;

namespace TradyStrat.Application.Tests.Indicators.Zones;

public class ZoneClassifierTests
{
    private static ZoneClassifier WithAll() => new(new IZoneRule[]
    {
        new BollingerZoneRule(),
        new RsiZoneRule(),
        new MovingAverageZoneRule(),
        new IchimokuZoneRule(),
    });

    [Fact]
    public void Returns_Hold_when_no_rules_apply()
    {
        var (zone, reasons) = WithAll().Classify(0m, IndicatorBundle.Empty);

        zone.ShouldBe(Zone.Hold);
        reasons.ShouldBeEmpty();
    }

    [Fact]
    public void Majority_vote_wins()
    {
        var bb  = new BollingerReading(5m, 4m, 3m, 1m);
        var ich = new IchimokuReading(0, 0, 0, 0, 0, IchimokuSignal.BelowCloud);
        var bundle = new IndicatorBundle(bb, Percentage.Of(25m), Sma50: 6m, Sma200: 7m, ich);

        var (zone, reasons) = WithAll().Classify(2m, bundle);

        zone.ShouldBe(Zone.Accumulate);
        reasons.Count.ShouldBe(4);
    }

    [Fact]
    public void Tie_resolves_to_Hold()
    {
        // 2 votes Accumulate (Bollinger below lower, RSI low), 2 votes Distribute (price above sma50, Ichimoku above)
        var bundle = new IndicatorBundle(
            new BollingerReading(5m, 4m, 3m, 1m),        // price 2 → Accumulate
            Percentage.Of(25m),                          // → Accumulate
            Sma50: 1m, Sma200: 0.5m,                     // price 2 above sma50 → Distribute
            new IchimokuReading(0, 0, 0, 0, 0, IchimokuSignal.AboveCloud));  // → Distribute

        var (zone, _) = WithAll().Classify(2m, bundle);

        zone.ShouldBe(Zone.Hold);
    }
}
