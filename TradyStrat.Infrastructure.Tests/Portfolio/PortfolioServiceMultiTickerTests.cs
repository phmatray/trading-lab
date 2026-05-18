using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Application.Portfolio;
using TradyStrat.TestKit;             // TestRepo<T>
using TradyStrat.TestKit.Specifications; // InMemoryDb
using Xunit;

namespace TradyStrat.Infrastructure.Tests.Portfolio;

public class PortfolioServiceMultiTickerTests
{
    [Fact]
    public async Task Builds_per_ticker_positions_summing_to_portfolio_totals()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();

        // Two instruments, two trades each, all buys.
        db.Trades.AddRange(
            Buy(instrumentId: 1, qty: 10m, price: 5m,   executedOn: new DateOnly(2026, 4, 1)),
            Buy(instrumentId: 1, qty: 10m, price: 6m,   executedOn: new DateOnly(2026, 4, 8)),
            Buy(instrumentId: 2, qty: 5m,  price: 100m, executedOn: new DateOnly(2026, 4, 2)),
            Buy(instrumentId: 2, qty: 5m,  price: 110m, executedOn: new DateOnly(2026, 4, 9)));
        await db.SaveChangesAsync(ct);

        var sut = new PortfolioService(new TestRepo<Trade>(db));

        var prices = new Dictionary<int, (decimal PriceEur, string Ticker, string Currency)>
        {
            [1] = (10m,  "AAA", "EUR"),  // 20 shares × 10 = 200 EUR
            [2] = (200m, "BBB", "EUR"),  // 10 shares × 200 = 2000 EUR
        };

        var snap = await sut.SnapshotAsync(prices, goalEur: 10000m, ct);

        snap.Positions.Count.ShouldBe(2);
        snap.Positions.Single(p => p.InstrumentId == 1).Quantity.ShouldBe(20m);
        snap.Positions.Single(p => p.InstrumentId == 2).Quantity.ShouldBe(10m);

        snap.CostBasisEur.ShouldBe(50m + 60m + 500m + 550m);   // 1160
        snap.CurrentValueEur.ShouldBe(200m + 2000m);            // 2200
        snap.UnrealizedPnLEur.ShouldBe(2200m - 1160m);          // 1040
        snap.ProgressPct.ShouldBe(22m);                         // 2200/10000*100

        // With multiple positions, legacy scalars are zero (Task 14 will remove these reads).
        snap.Shares.ShouldBe(0m);
        snap.AvgCostEur.ShouldBe(0m);
    }

    private static Trade Buy(int instrumentId, decimal qty, decimal price, DateOnly executedOn)
        => new()
        {
            Id = 0,
            InstrumentId = instrumentId,
            ExecutedOn = executedOn,
            Side = TradeSide.Buy,
            Quantity = qty,
            PricePerShare = price,
            FeesEur = 0m,
            Note = null,
            CreatedAt = DateTime.UtcNow,
        };
}
