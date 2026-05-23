using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Application.Dashboard.Navigation;
using TradyStrat.Infrastructure.PriceFeed;
using TradyStrat.TestKit;            // shared TestRepo<T>
using TradyStrat.TestKit.Settings;   // FakeFocusTickerRepository
using TradyStrat.TestKit.Specifications; // InMemoryDb
using Xunit;

namespace TradyStrat.Infrastructure.Tests.Dashboard.Navigation;

public class EntryNavigationServiceTests
{
    // Trading-day calendar:
    //   Mon 13, Tue 14, Wed 15, Thu 16, Fri 17, (Sat 18, Sun 19 — closed), Mon 20.
    private static readonly DateOnly Mon13 = new(2026, 4, 13);
    private static readonly DateOnly Wed15 = new(2026, 4, 15);
    private static readonly DateOnly Fri17 = new(2026, 4, 17);
    private static readonly DateOnly Sun19 = new(2026, 4, 19);
    private static readonly DateOnly Mon20 = new(2026, 4, 20);

    private static PriceBar Bar(DateOnly d) => new()
    {
        Id = 0, Ticker = "CON3.L", Date = d,
        Open = 1, High = 1, Low = 1, Close = 1, Volume = 1,
    };

    private static async Task<EntryNavigationService> SeedAsync(
        TradyStrat.Infrastructure.Data.AppDbContext db, params DateOnly[] dates)
    {
        foreach (var d in dates) db.PriceBars.Add(Bar(d));
        await db.SaveChangesAsync();
        return new EntryNavigationService(new EfPriceBarReadRepository(db), new FakeFocusTickerRepository("CON3.L"));
    }

    [Fact]
    public async Task EarliestAsync_returns_min_date()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        var sut = await SeedAsync(db, Mon13, Wed15, Fri17, Mon20);

        var result = await sut.EarliestAsync(ct);
        result.ShouldBe(Mon13);
    }

    [Fact]
    public async Task LatestAsync_returns_max_date()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        var sut = await SeedAsync(db, Mon13, Wed15, Fri17, Mon20);

        var result = await sut.LatestAsync(ct);
        result.ShouldBe(Mon20);
    }

    [Fact]
    public async Task PreviousAsync_skips_weekend_gap()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        var sut = await SeedAsync(db, Mon13, Wed15, Fri17, Mon20);

        var result = await sut.PreviousAsync(Mon20, ct);
        result.ShouldBe(Fri17);
    }

    [Fact]
    public async Task PreviousAsync_returns_null_at_floor()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        var sut = await SeedAsync(db, Mon13, Wed15, Fri17, Mon20);

        var result = await sut.PreviousAsync(Mon13, ct);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task NextAsync_skips_weekend_gap()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        var sut = await SeedAsync(db, Mon13, Wed15, Fri17, Mon20);

        var result = await sut.NextAsync(Fri17, ct);
        result.ShouldBe(Mon20);
    }

    [Fact]
    public async Task NextAsync_returns_null_at_ceiling()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        var sut = await SeedAsync(db, Mon13, Wed15, Fri17, Mon20);

        var result = await sut.NextAsync(Mon20, ct);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveOrFallbackAsync_returns_input_when_trading_day()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        var sut = await SeedAsync(db, Mon13, Wed15, Fri17, Mon20);

        var result = await sut.ResolveOrFallbackAsync(Wed15, ct);
        result.ShouldBe(Wed15);
    }

    [Fact]
    public async Task ResolveOrFallbackAsync_returns_nearest_earlier_on_closed_day()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        var sut = await SeedAsync(db, Mon13, Wed15, Fri17, Mon20);

        var result = await sut.ResolveOrFallbackAsync(Sun19, ct);
        result.ShouldBe(Fri17);
    }

    [Fact]
    public async Task ResolveOrFallbackAsync_throws_when_nothing_earlier_exists()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        var sut = await SeedAsync(db, Mon20); // only Mon20 seeded

        await Should.ThrowAsync<NoTradingDaysException>(
            async () => await sut.ResolveOrFallbackAsync(Mon13, ct));
    }

    [Fact]
    public async Task EarliestAsync_throws_when_db_empty()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        var sut = await SeedAsync(db); // no dates

        await Should.ThrowAsync<NoTradingDaysException>(
            async () => await sut.EarliestAsync(ct));
    }

    [Fact]
    public async Task LatestAsync_throws_when_db_empty()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        var sut = await SeedAsync(db);

        await Should.ThrowAsync<NoTradingDaysException>(
            async () => await sut.LatestAsync(ct));
    }
}
