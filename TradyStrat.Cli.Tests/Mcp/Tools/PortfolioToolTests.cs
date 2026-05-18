using Shouldly;
using TradyStrat.Application.Portfolio;
using TradyStrat.Cli.Mcp.Tools;
using TradyStrat.Domain;
using TradyStrat.Infrastructure.Data;
using TradyStrat.TestKit;
using TradyStrat.TestKit.Specifications;
using TradyStrat.TestKit.Time;
using Xunit;

namespace TradyStrat.Cli.Tests.Mcp.Tools;

public sealed class PortfolioToolTests
{
    private static readonly DateOnly FixedToday = new(2026, 5, 18);
    private const string EurTicker = "ACWI";
    private const string GbpTicker = "CON3.L";

    // ─── Fixture helpers ──────────────────────────────────────────────────────

    private static Instrument MakeInstrument(int id, string ticker, string currency = "EUR",
        InstrumentKind kind = InstrumentKind.Held) => new()
    {
        Id = id,
        Ticker = ticker,
        Name = ticker,
        Currency = currency,
        Exchange = "LSE",
        TimezoneId = "Europe/London",
        Kind = kind,
        AddedAt = DateTime.UtcNow,
    };

    private static PriceBar MakeBar(int id, string ticker, DateOnly date, decimal close) => new()
    {
        Id = id,
        Ticker = ticker,
        Date = date,
        Open = close,
        High = close + 1m,
        Low = close - 1m,
        Close = close,
        Volume = 1_000_000,
    };

    private static Trade MakeBuyTrade(int id, int instrumentId, DateOnly date, decimal qty, decimal price) => new()
    {
        Id = id,
        InstrumentId = instrumentId,
        ExecutedOn = date,
        Side = TradeSide.Buy,
        Quantity = qty,
        PricePerShare = price,
        FeesEur = 0m,
        CreatedAt = DateTime.UtcNow,
    };

    private static FxRate MakeFxRate(int id, string quote, decimal rate, DateOnly date) => new()
    {
        Id = id,
        Date = date,
        Base = "EUR",
        Quote = quote,
        Rate = rate,
        FetchedAt = DateTime.UtcNow,
    };

    private static GoalConfig MakeGoal(decimal targetEur) => new()
    {
        Id = 1,
        TargetEur = targetEur,
        TargetDate = null,
        UpdatedAt = DateTime.UtcNow,
    };

    private static async Task<PortfolioTool> BuildAsync(
        AppDbContext db,
        DateOnly? clockDate = null,
        CancellationToken ct = default)
    {
        await db.SaveChangesAsync(ct);

        var instRepo  = new TestRepo<Instrument>(db);
        var tradeRepo = new TestRepo<Trade>(db);
        var barRepo   = new TestRepo<PriceBar>(db);
        var fxRepo    = new TestRepo<FxRate>(db);
        var goalRepo  = new TestRepo<GoalConfig>(db);

        var portfolioSvc = new PortfolioService(tradeRepo);
        var clock = new FakeClock((clockDate ?? FixedToday).ToDateTime(TimeOnly.MinValue));

        return new PortfolioTool(portfolioSvc, instRepo, tradeRepo, barRepo, fxRepo, goalRepo, clock);
    }

    // ─── Tests ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_portfolio_snapshot_with_aggregate_and_positions()
    {
        var ct = TestContext.Current.CancellationToken;
        var db = InMemoryDb.Create();

        db.Instruments.Add(MakeInstrument(1, EurTicker, "EUR"));
        db.PriceBars.Add(MakeBar(1, EurTicker, FixedToday, 100m));
        db.Trades.Add(MakeBuyTrade(1, instrumentId: 1, FixedToday.AddDays(-10), qty: 10m, price: 90m));
        db.Goals.Add(MakeGoal(10_000m));

        var tool = await BuildAsync(db, ct: ct);
        var result = await tool.GetPortfolio(ct: ct);

        result.ShouldNotBeNull();
        result.AsOfDate.ShouldBe(FixedToday);
        result.Positions.Count.ShouldBe(1);
        result.Positions[0].Ticker.ShouldBe(EurTicker);
        result.Positions[0].Qty.ShouldBe(10m);

        // Market value = 10 shares × 100 EUR close = 1000 EUR
        result.Aggregate.TotalValueEur.ShouldBe(1_000m);
        result.Aggregate.GoalEur.ShouldBe(10_000m);
        result.Aggregate.ProgressPct.ShouldBe(10m);
    }

    [Fact]
    public async Task Default_asOf_is_today()
    {
        var ct = TestContext.Current.CancellationToken;
        var db = InMemoryDb.Create();

        db.Instruments.Add(MakeInstrument(1, EurTicker, "EUR"));
        db.PriceBars.Add(MakeBar(1, EurTicker, FixedToday, 100m));
        db.Goals.Add(MakeGoal(10_000m));

        var tool = await BuildAsync(db, clockDate: FixedToday, ct: ct);
        var result = await tool.GetPortfolio(ct: ct);   // no asOf passed

        result.AsOfDate.ShouldBe(FixedToday);
    }

    [Fact]
    public async Task Explicit_asOf_passes_through_correctly()
    {
        var ct = TestContext.Current.CancellationToken;
        var db = InMemoryDb.Create();

        var historicalDate = new DateOnly(2026, 5, 1);
        db.Instruments.Add(MakeInstrument(1, EurTicker, "EUR"));
        // Only a bar on the historical date — should be picked up for that asOf
        db.PriceBars.Add(MakeBar(1, EurTicker, historicalDate, 80m));
        // A later bar that should NOT be used when asOf = historicalDate
        db.PriceBars.Add(MakeBar(2, EurTicker, FixedToday, 100m));
        db.Trades.Add(MakeBuyTrade(1, instrumentId: 1, historicalDate.AddDays(-5), qty: 5m, price: 75m));
        db.Goals.Add(MakeGoal(10_000m));

        var tool = await BuildAsync(db, ct: ct);
        var result = await tool.GetPortfolio(asOf: "2026-05-01", ct: ct);

        result.AsOfDate.ShouldBe(historicalDate);
        // Market value = 5 shares × 80 EUR = 400
        result.Aggregate.TotalValueEur.ShouldBe(400m);
    }

    [Fact]
    public async Task Invalid_asOf_format_throws()
    {
        var ct = TestContext.Current.CancellationToken;
        var db = InMemoryDb.Create();
        db.Instruments.Add(MakeInstrument(1, EurTicker, "EUR"));
        db.Goals.Add(MakeGoal(10_000m));

        var tool = await BuildAsync(db, ct: ct);

        var ex = await Should.ThrowAsync<ArgumentException>(
            () => tool.GetPortfolio(asOf: "18-05-2026", ct: ct));

        ex.Message.ShouldContain("18-05-2026");
        ex.Message.ShouldContain("YYYY-MM-DD");
    }
}
