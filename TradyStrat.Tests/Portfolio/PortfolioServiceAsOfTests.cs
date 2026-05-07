using Shouldly;
using TradyStrat.Features.Portfolio;
using TradyStrat.Common.Domain;
using TradyStrat.Tests.Fx;
using TradyStrat.Tests.Specifications;
using Xunit;

namespace TradyStrat.Tests.Portfolio;

public class PortfolioServiceAsOfTests
{
    [Fact]
    public async Task Excludes_trades_after_asOf()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        db.Trades.AddRange(
            new Trade { Id = 1, ExecutedOn = new DateOnly(2026, 4, 1), Side = TradeSide.Buy,
                Quantity = 100m, PricePerShare = 1m, FeesEur = 0m, Note = "", CreatedAt = DateTime.UtcNow },
            new Trade { Id = 2, ExecutedOn = new DateOnly(2026, 5, 5), Side = TradeSide.Buy,
                Quantity = 200m, PricePerShare = 1m, FeesEur = 0m, Note = "", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);

        var svc = new PortfolioService(new TestRepo<Trade>(db));

        var snap = await svc.SnapshotAsync(
            asOf: new DateOnly(2026, 4, 30),
            currentPriceEur: 2m, goalEur: 1000m, ct: ct);

        snap.Shares.ShouldBe(100m);   // only the April trade counted
    }
}
