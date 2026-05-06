using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using TradyStrat.Shared.Domain;
using TradyStrat.Specifications.PriceBars;
using TradyStrat.Specifications.Suggestions;
using TradyStrat.Specifications.Trades;
using TradyStrat.Specifications.FxRates;
using Xunit;

namespace TradyStrat.Tests.Specifications;

public class SpecsRoundtripTests
{
    private static Trade Buy(int day, decimal qty = 1m, decimal price = 1m) => new()
    {
        Id = 0, ExecutedOn = new DateOnly(2026, 1, day), Side = TradeSide.Buy,
        Quantity = qty, PricePerShare = price, FeesEur = 0, Note = null,
        CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task TradesByDateRangeSpec_filters_inclusive()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        db.Trades.AddRange(Buy(1), Buy(5), Buy(10));
        await db.SaveChangesAsync(ct);

        var spec = new TradesByDateRangeSpec(new DateOnly(2026,1,2), new DateOnly(2026,1,7));
        var rows = await db.Trades.WithSpecification(spec).ToListAsync(ct);

        rows.Count.ShouldBe(1);
        rows[0].ExecutedOn.Day.ShouldBe(5);
    }

    [Fact]
    public async Task LatestPriceBarSpec_returns_most_recent_for_ticker()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        db.PriceBars.AddRange(
            new PriceBar { Id = 0, Ticker = "CON3.DE", Date = new(2026,1,1), Open = 1, High = 1, Low = 1, Close = 1, Volume = 1 },
            new PriceBar { Id = 0, Ticker = "CON3.DE", Date = new(2026,1,2), Open = 2, High = 2, Low = 2, Close = 2, Volume = 1 },
            new PriceBar { Id = 0, Ticker = "COIN",    Date = new(2026,1,3), Open = 9, High = 9, Low = 9, Close = 9, Volume = 1 });
        await db.SaveChangesAsync(ct);

        var bar = await db.PriceBars.WithSpecification(new LatestPriceBarSpec("CON3.DE")).FirstOrDefaultAsync(ct);

        bar.ShouldNotBeNull();
        bar.Date.ShouldBe(new DateOnly(2026,1,2));
    }

    [Fact]
    public async Task SuggestionForDateSpec_finds_exact_date_match()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        db.Suggestions.Add(new Suggestion {
            Id = 0, ForDate = new(2026,5,6), Action = SuggestionAction.Hold,
            Conviction = 3, Rationale = "x", CitationsJson = "[]",
            PromptHash = "h", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);

        var hit  = await db.Suggestions.WithSpecification(new SuggestionForDateSpec(new(2026,5,6))).FirstOrDefaultAsync(ct);
        var miss = await db.Suggestions.WithSpecification(new SuggestionForDateSpec(new(2026,5,7))).FirstOrDefaultAsync(ct);

        hit.ShouldNotBeNull();
        miss.ShouldBeNull();
    }

    [Fact]
    public async Task LatestFxRateSpec_returns_most_recent_at_or_before_date()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        db.FxRates.AddRange(
            new FxRate { Id = 0, Pair = "EURUSD", Date = new(2026,1,1), UsdPerEur = 1.05m, FetchedAt = DateTime.UtcNow },
            new FxRate { Id = 0, Pair = "EURUSD", Date = new(2026,1,3), UsdPerEur = 1.08m, FetchedAt = DateTime.UtcNow },
            new FxRate { Id = 0, Pair = "EURUSD", Date = new(2026,1,5), UsdPerEur = 1.10m, FetchedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);

        var on4 = await db.FxRates.WithSpecification(new LatestFxRateSpec("EURUSD", new(2026,1,4))).FirstOrDefaultAsync(ct);

        on4.ShouldNotBeNull();
        on4.Date.ShouldBe(new DateOnly(2026,1,3));
    }
}
