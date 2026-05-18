using TradyStrat.Infrastructure.Data;
using Shouldly;
using TradyStrat.Application.Portfolio;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.TestKit;             // TestRepo<T>
using TradyStrat.TestKit.Specifications; // InMemoryDb
using Xunit;

namespace TradyStrat.Infrastructure.Tests.Portfolio;

public class PortfolioServiceTests
{
    private static Trade Buy(int day, decimal qty, decimal price, decimal fees = 0m) => new()
    {
        Id = 0, InstrumentId = 1, ExecutedOn = new DateOnly(2026,1,day), Side = TradeSide.Buy,
        Quantity = qty, PricePerShare = price, FeesEur = fees, Note = null,
        CreatedAt = DateTime.UtcNow,
    };
    private static Trade Sell(int day, decimal qty, decimal price, decimal fees = 0m) =>
        Buy(day, qty, price, fees) with { Side = TradeSide.Sell };

    private static PortfolioService NewService(TradyStrat.Infrastructure.Data.AppDbContext db)
        => new(new TestRepo<Trade>(db));

    private static Dictionary<int, (decimal PriceEur, string Ticker, string Currency)>
        PriceMap(decimal priceEur, int instrumentId = 1)
        => new()
        {
            [instrumentId] = (priceEur, "CON3.L", "USD"),
        };

    [Fact]
    public async Task Empty_trade_log_returns_zero_snapshot()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        var snap = await NewService(db).SnapshotAsync(PriceMap(5m), goalEur: 1_000_000m, ct: ct);

        snap.Positions.Count.ShouldBe(0);
        snap.Shares.ShouldBe(0m);
        snap.CurrentValueEur.ShouldBe(0m);
        snap.UnrealizedPnLEur.ShouldBe(0m);
        snap.RealizedPnLEur.ShouldBe(0m);
        snap.ProgressPct.ShouldBe(0m);
    }

    [Fact]
    public async Task Single_buy_avg_cost_includes_fees()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        db.Trades.Add(Buy(1, qty: 10m, price: 4.00m, fees: 2.00m));
        await db.SaveChangesAsync(ct);

        var snap = await NewService(db).SnapshotAsync(PriceMap(5m), 1_000_000m, ct);

        snap.Shares.ShouldBe(10m);
        // Avg cost = (10*4 + 2) / 10 = 4.20
        snap.AvgCostEur.ShouldBe(4.20m);
        snap.CurrentValueEur.ShouldBe(50m);
        snap.UnrealizedPnLEur.ShouldBe(50m - 42m);
    }

    [Fact]
    public async Task FIFO_sell_realizes_oldest_lot_first()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        db.Trades.AddRange(
            Buy(1, qty: 10m, price: 4.00m),    // lot @ 4.00
            Buy(5, qty: 10m, price: 5.00m),    // lot @ 5.00
            Sell(8, qty: 5m, price: 6.00m));   // sell 5 → realize 5*(6-4)=10
        await db.SaveChangesAsync(ct);

        var snap = await NewService(db).SnapshotAsync(PriceMap(7m), 1_000_000m, ct);

        snap.Shares.ShouldBe(15m);
        snap.RealizedPnLEur.ShouldBe(10m);
        // Remaining: 5 @ 4.00, 10 @ 5.00 → avg = (5*4 + 10*5) / 15 = 70/15
        snap.AvgCostEur.ShouldBe(70m / 15m, tolerance: 0.0001m);
    }

    [Fact]
    public async Task Progress_pct_computed_against_goal()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        db.Trades.Add(Buy(1, qty: 100m, price: 1m));
        await db.SaveChangesAsync(ct);

        var snap = await NewService(db).SnapshotAsync(PriceMap(5m), goalEur: 1000m, ct: ct);

        snap.CurrentValueEur.ShouldBe(500m);
        snap.ProgressPct.ShouldBe(50m);
    }

    [Fact]
    public async Task Sell_more_than_held_throws()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        db.Trades.AddRange(Buy(1, 5m, 4m), Sell(2, 10m, 5m));
        await db.SaveChangesAsync(ct);

        await Should.ThrowAsync<TradeValidationException>(() =>
            NewService(db).SnapshotAsync(PriceMap(5m), 1_000_000m, ct));
    }
}
