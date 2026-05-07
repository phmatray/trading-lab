using Shouldly;
using TradyStrat.Features.Indicators;
using TradyStrat.Shared.Domain;
using TradyStrat.Tests.Fx;
using TradyStrat.Tests.Specifications;
using Xunit;

namespace TradyStrat.Tests.Indicators;

public class IndicatorEngineHistoryTests
{
    [Fact]
    public async Task HistoryFor_returns_series_for_kind()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        for (int i = 0; i < 50; i++)
            db.PriceBars.Add(new PriceBar
            {
                Id = i + 1, Ticker = "T",
                Date = new DateOnly(2026, 1, 1).AddDays(i),
                Open = 100m, High = 101m, Low = 99m,
                Close = 100m + i * 0.5m, Volume = 1000,
            });
        await db.SaveChangesAsync(ct);

        var factory = new IndicatorHistoryProviderFactory([new RsiHistoryProvider()]);
        var engine = new IndicatorEngine(
            new TestRepo<PriceBar>(db),
            new ZoneClassifier([]),     // empty rule set fine for this test
            factory);

        var s = await engine.HistoryFor("T", IndicatorKind.Rsi, 20, ct);

        s.Values.Count.ShouldBeGreaterThan(0);
        s.ThresholdHi.ShouldBe(70m);
    }
}
