using Microsoft.EntityFrameworkCore;
using TradyStrat.Tests.Fx.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Features.Fx;
using TradyStrat.Common.Domain;
using TradyStrat.Tests.Specifications;
using TradyStrat.Tests.Time;
using Xunit;

namespace TradyStrat.Tests.Fx;

public class DailyFxCacheTests
{
    private static FxRate Rate(DateOnly d, decimal v) => new()
    {
        Id = 0, Pair = "EURUSD", Date = d, UsdPerEur = v, FetchedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task Skips_fetch_if_today_present()
    {
        await using var db = InMemoryDb.Create();
        var clock = new FakeClock(new DateTime(2026,5,6,0,0,0,DateTimeKind.Utc));
        db.FxRates.Add(Rate(new(2026,5,6), 1.0820m));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var prov = new StubFxProvider([]);
        var cache = new DailyFxCache(prov, db, clock, NullLogger<DailyFxCache>.Instance);

        await cache.EnsureFreshAsync("EURUSD", TestContext.Current.CancellationToken);

        prov.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Fetches_and_persists_when_stale()
    {
        await using var db = InMemoryDb.Create();
        var clock = new FakeClock(new DateTime(2026,5,6,0,0,0,DateTimeKind.Utc));
        var prov = new StubFxProvider([Rate(new(2026,5,5), 1.08m), Rate(new(2026,5,6), 1.09m)]);
        var cache = new DailyFxCache(prov, db, clock, NullLogger<DailyFxCache>.Instance);

        await cache.EnsureFreshAsync("EURUSD", TestContext.Current.CancellationToken);

        (await db.FxRates.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(2);
    }
}
