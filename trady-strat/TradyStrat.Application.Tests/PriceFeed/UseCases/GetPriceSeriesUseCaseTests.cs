using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.Indicators;
using TradyStrat.Application.Indicators.History;
using TradyStrat.Domain.Indicators;
using TradyStrat.Domain.Indicators.Services;
using TradyStrat.Application.Indicators.Bollinger;
using TradyStrat.Application.Indicators.Ichimoku;
using TradyStrat.Application.Indicators.MovingAverage;
using TradyStrat.Application.Indicators.Rsi;
using TradyStrat.Application.PriceFeed.UseCases;
using TradyStrat.Domain;
using TradyStrat.Infrastructure.PriceFeed;
using TradyStrat.TestKit;
using TradyStrat.TestKit.Specifications;
using Xunit;

namespace TradyStrat.Application.Tests.PriceFeed.UseCases;

public class GetPriceSeriesUseCaseTests
{
    private const string Ticker = "TEST";

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static PriceBar Bar(int id, string ticker, DateOnly date, decimal close) =>
        new()
        {
            Id = id, Ticker = ticker, Date = date,
            Open = close, High = close + 1m, Low = close - 1m, Close = close, Volume = 1_000,
        };

    /// <summary>
    /// Builds the real <see cref="IndicatorEngine"/> wired with all registered providers,
    /// backed by the given in-memory DB.
    /// </summary>
    private static IndicatorEngine BuildIndicatorEngine(TradyStrat.Infrastructure.Data.AppDbContext db)
    {
        var repo = new EfPriceBarReadRepository(db);
        IEnumerable<IIndicatorHistoryProvider> providers =
        [
            new RsiHistoryProvider(),
            new BollingerHistoryProvider(),
            new IchimokuHistoryProvider(),
            new Sma200HistoryProvider(),
        ];
        var factory    = new IndicatorHistoryProviderFactory(providers);
        var classifier = new ZoneClassifier([]);   // zone rules not needed here
        return new IndicatorEngine(repo, classifier, factory);
    }

    private static GetPriceSeriesUseCase BuildUseCase(TradyStrat.Infrastructure.Data.AppDbContext db) =>
        new(
            new EfPriceBarReadRepository(db),
            BuildIndicatorEngine(db),
            NullLogger<GetPriceSeriesUseCase>.Instance);

    // ──────────────────────────────────────────────────────────────────────────
    // Test 1 — Happy path WITHOUT indicators
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_bars_in_range_without_indicators()
    {
        await using var db = InMemoryDb.Create();

        var base_ = new DateOnly(2026, 1, 1);
        for (int i = 0; i < 5; i++)
            db.PriceBars.Add(Bar(i + 1, Ticker, base_.AddDays(i), 100m + i));

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut    = BuildUseCase(db);
        var input  = new GetPriceSeriesInput(Ticker, base_.AddDays(1), base_.AddDays(3), WithIndicators: false);
        var result = await sut.ExecuteAsync(input, TestContext.Current.CancellationToken);

        result.Bars.Count.ShouldBe(3);
        result.Bars.Select(b => b.Date).ShouldBe(
            [base_.AddDays(1), base_.AddDays(2), base_.AddDays(3)]);
        result.Indicators.ShouldBeNull();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Test 2 — Happy path WITH indicators
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_bars_with_indicator_arrays_same_length_as_bars()
    {
        // Seed enough bars so that at least the simpler indicators (RSI-14, Bollinger-20)
        // can produce some non-null values.  We use 50 total bars, then request the last 30.
        await using var db = InMemoryDb.Create();

        var base_ = new DateOnly(2026, 1, 1);
        const int Total = 50;
        const int Window = 30;

        for (int i = 0; i < Total; i++)
        {
            // Alternating close prices to give RSI something to compute.
            var close = 100m + (i % 2 == 0 ? i * 0.5m : -(i * 0.3m));
            db.PriceBars.Add(Bar(i + 1, Ticker, base_.AddDays(i), close));
        }

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var from   = base_.AddDays(Total - Window);  // day 20
        var to     = base_.AddDays(Total - 1);        // day 49
        var sut    = BuildUseCase(db);
        var input  = new GetPriceSeriesInput(Ticker, from, to, WithIndicators: true);
        var result = await sut.ExecuteAsync(input, TestContext.Current.CancellationToken);

        result.Bars.Count.ShouldBe(Window);
        result.Indicators.ShouldNotBeNull();

        var ind = result.Indicators!;
        ind.Rsi.Count.ShouldBe(Window);
        ind.BollingerMid.Count.ShouldBe(Window);
        ind.Sma200.Count.ShouldBe(Window);
        ind.Ichimoku.Count.ShouldBe(Window);

        // Ichimoku provider returns Close prices — all non-null.
        ind.Ichimoku.ShouldAllBe(v => v.HasValue);

        // RSI(14): with 50 total bars we have 50-14=36 computed values; since we
        // request the last 30 bars, all 30 should have a computed value.
        ind.Rsi.ShouldAllBe(v => v.HasValue);

        // BollingerMid(20): 50-20+1=31 computed values available; last 30 all non-null.
        ind.BollingerMid.ShouldAllBe(v => v.HasValue);

        // Sma200: requires 200 bars — with only 50, all values are null.
        ind.Sma200.ShouldAllBe(v => !v.HasValue);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Test 3 — Empty range
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Empty_range_returns_empty_bars_and_null_indicators()
    {
        await using var db = InMemoryDb.Create();

        var base_ = new DateOnly(2026, 1, 1);
        db.PriceBars.Add(Bar(1, Ticker, base_, 100m));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut    = BuildUseCase(db);
        // Request a date range that has no bars.
        var input  = new GetPriceSeriesInput(Ticker, new DateOnly(2027, 1, 1), new DateOnly(2027, 1, 31), WithIndicators: true);
        var result = await sut.ExecuteAsync(input, TestContext.Current.CancellationToken);

        result.Bars.ShouldBeEmpty();
        result.Indicators.ShouldBeNull();
    }
}
