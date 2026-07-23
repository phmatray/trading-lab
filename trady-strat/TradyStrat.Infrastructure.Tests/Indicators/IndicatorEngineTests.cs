using Shouldly;
using TradyStrat.Application.Indicators.Bollinger;
using TradyStrat.Application.Indicators.History;
using TradyStrat.Application.Indicators.Ichimoku;
using TradyStrat.Application.Indicators.MovingAverage;
using TradyStrat.Application.Indicators.Rsi;
using TradyStrat.Domain;
using TradyStrat.Domain.Indicators;
using TradyStrat.Domain.Indicators.Services;
using TradyStrat.Infrastructure.PriceFeed;
using TradyStrat.TestKit.Indicators;
using TradyStrat.TestKit.Specifications;
using Xunit;

namespace TradyStrat.Infrastructure.Tests.Indicators;

public class IndicatorEngineTests
{
    [Fact]
    public async Task ComputeFor_throws_when_no_bars_for_ticker()
    {
        await using var db = InMemoryDb.Create();
        var repo = new EfPriceBarReadRepository(db);
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

        var repo = new EfPriceBarReadRepository(db);
        var classifier = new ZoneClassifier(new IZoneRule[]
        {
            new BollingerZoneRule(), new RsiZoneRule(),
            new MovingAverageZoneRule(), new IchimokuZoneRule()
        });

        var engine = new IndicatorEngine(repo, classifier, new IndicatorHistoryProviderFactory([]));

        var reading = await engine.ComputeFor("CON3.DE", TestContext.Current.CancellationToken);

        reading.Ticker.ShouldBe("CON3.DE");
        reading.Price.ShouldBe(bars[^1].Close);
        reading.Bollinger.IsEmpty.ShouldBeFalse();
        reading.Rsi.IsEmpty.ShouldBeFalse();
        reading.Sma50.ShouldBeGreaterThan(0m);
        reading.Sma200.ShouldBeGreaterThan(0m);
        reading.Ichimoku.IsEmpty.ShouldBeFalse();
        reading.Reasons.Count.ShouldBeGreaterThan(0);
    }
}
