using Shouldly;
using TradyStrat.Features.Indicators.Zones;
using TradyStrat.Features.Indicators.History;
using TradyStrat.Features.Indicators;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Features.Indicators.Bollinger;
using TradyStrat.Features.Indicators.Ichimoku;
using TradyStrat.Features.Indicators.MovingAverage;
using TradyStrat.Features.Indicators.Rsi;
using TradyStrat.Tests.Fx;          // TestRepo<T>
using TradyStrat.Tests.Specifications; // InMemoryDb
using Xunit;

namespace TradyStrat.Tests.Indicators;

public class IndicatorEngineTests
{
    [Fact]
    public async Task ComputeFor_throws_when_no_bars_for_ticker()
    {
        await using var db = InMemoryDb.Create();
        var repo = new TestRepo<PriceBar>(db);
        var engine = new IndicatorEngine(repo, new ZoneClassifier(Array.Empty<IZoneRule>()), new IndicatorHistoryProviderFactory([]));

        await Should.ThrowAsync<IndicatorComputationException>(() =>
            engine.ComputeFor("CON3.DE", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ComputeFor_returns_reading_with_price_and_zone()
    {
        await using var db = InMemoryDb.Create();
        var bars = SeriesLoader.LoadCloses("CON3.DE");
        db.PriceBars.AddRange(bars);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repo = new TestRepo<PriceBar>(db);
        var classifier = new ZoneClassifier(new IZoneRule[]
        {
            new BollingerZoneRule(), new RsiZoneRule(),
            new MovingAverageZoneRule(), new IchimokuZoneRule()
        });

        var engine = new IndicatorEngine(repo, classifier, new IndicatorHistoryProviderFactory([]));

        var reading = await engine.ComputeFor("CON3.DE", TestContext.Current.CancellationToken);

        reading.Ticker.ShouldBe("CON3.DE");
        reading.Price.ShouldBe(bars[^1].Close);
        reading.Bollinger.ShouldNotBeNull();
        reading.Rsi.ShouldNotBeNull();
        reading.Sma50.ShouldNotBeNull();
        reading.Sma200.ShouldNotBeNull();
        reading.Ichimoku.ShouldNotBeNull();
        reading.Reasons.Count.ShouldBeGreaterThan(0);
    }
}
