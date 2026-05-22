using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.PriceFeed.UseCases;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Application.UseCases;
using TradyStrat.Cli.Mcp.Tools;
using TradyStrat.Domain;
using TradyStrat.Domain.Shared;
using TradyStrat.Infrastructure.Settings;
using TradyStrat.TestKit;
using TradyStrat.TestKit.Specifications;
using TradyStrat.TestKit.Time;
using Xunit;

namespace TradyStrat.Cli.Tests.Mcp.Tools;

public sealed class PriceToolTests
{
    private static readonly DateOnly FixedToday = new(2026, 5, 18);
    private const string Ticker = "CON3.L";

    // ─── Fake use case ───────────────────────────────────────────────────────────

    private sealed class FakePriceUseCase(GetPriceSeriesOutput response)
        : IUseCase<GetPriceSeriesInput, GetPriceSeriesOutput>
    {
        public GetPriceSeriesInput? LastInput { get; private set; }

        public Task<GetPriceSeriesOutput> ExecuteAsync(GetPriceSeriesInput input, CancellationToken ct)
        {
            LastInput = input;
            return Task.FromResult(response);
        }
    }

    // ─── Fixture helpers ──────────────────────────────────────────────────────

    private static Instrument MakeInstrument(int id, string ticker)
        => Instrument.Existing(
            id:         new InstrumentId(id),
            ticker:     ticker,
            name:       ticker,
            currency:   Currency.Gbp,
            exchange:   Exchange.Of("LSE"),
            timezoneId: TimezoneId.Of("Europe/London"),
            kind:       InstrumentKind.Held,
            addedAt:    DateTime.UtcNow);

    private static PriceBar MakeBar(int id, string ticker, DateOnly date, decimal close = 100m) => new()
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

    private static async Task<(PriceTool tool, FakePriceUseCase fakeUseCase)> BuildAsync(
        string[] knownTickers,
        GetPriceSeriesOutput? response = null,
        DateOnly? clockDate = null,
        CancellationToken ct = default)
    {
        var db = InMemoryDb.Create();
        for (var i = 0; i < knownTickers.Length; i++)
            db.Instruments.Add(MakeInstrument(i + 1, knownTickers[i]));
        await db.SaveChangesAsync(ct);

        var repo = new EfInstrumentRepository(db);
        var listInstruments = new ListInstrumentsUseCase(repo, NullLogger<ListInstrumentsUseCase>.Instance);
        var guards = new Guards(listInstruments);

        var defaultResponse = response ?? new GetPriceSeriesOutput([], null);
        var fakeUseCase = new FakePriceUseCase(defaultResponse);
        var clock = new FakeClock((clockDate ?? FixedToday).ToDateTime(TimeOnly.MinValue));

        var tool = new PriceTool(fakeUseCase, guards, clock);
        return (tool, fakeUseCase);
    }

    // ─── Tests ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_bars_for_explicit_range()
    {
        var ct = TestContext.Current.CancellationToken;

        var bars = new[]
        {
            MakeBar(1, Ticker, new DateOnly(2026, 5, 1), 100m),
            MakeBar(2, Ticker, new DateOnly(2026, 5, 2), 101m),
            MakeBar(3, Ticker, new DateOnly(2026, 5, 3), 102m),
        };
        var response = new GetPriceSeriesOutput(bars, null);

        var (tool, useCase) = await BuildAsync([Ticker], response, ct: ct);

        var result = await tool.QueryPrices(
            instrument: Ticker,
            from: "2026-05-01",
            to: "2026-05-03",
            ct: ct);

        result.ShouldNotBeNull();
        result.Instrument.ShouldBe(Ticker);
        result.BarCount.ShouldBe(3);
        result.Bars.Count.ShouldBe(3);

        useCase.LastInput.ShouldNotBeNull();
        useCase.LastInput!.From.ShouldBe(new DateOnly(2026, 5, 1));
        useCase.LastInput.To.ShouldBe(new DateOnly(2026, 5, 3));
        useCase.LastInput.Ticker.ShouldBe(Ticker);
    }

    [Fact]
    public async Task Default_range_is_90_days_back_from_today()
    {
        var ct = TestContext.Current.CancellationToken;
        var (tool, useCase) = await BuildAsync([Ticker], ct: ct);

        await tool.QueryPrices(instrument: Ticker, ct: ct);

        useCase.LastInput.ShouldNotBeNull();
        useCase.LastInput!.To.ShouldBe(FixedToday);
        useCase.LastInput.From.ShouldBe(FixedToday.AddDays(-90));
    }

    [Fact]
    public async Task Window_over_365_days_throws_with_message_about_365()
    {
        var ct = TestContext.Current.CancellationToken;
        var (tool, _) = await BuildAsync([Ticker], ct: ct);

        var ex = await Should.ThrowAsync<ArgumentException>(
            () => tool.QueryPrices(
                instrument: Ticker,
                from: "2025-01-01",
                to: "2026-05-18",    // 503 days — over 365
                ct: ct));

        ex.Message.ShouldContain("365");
    }

    [Fact]
    public async Task Unknown_instrument_throws_via_Guards()
    {
        var ct = TestContext.Current.CancellationToken;
        var (tool, _) = await BuildAsync([Ticker], ct: ct);

        var ex = await Should.ThrowAsync<ArgumentException>(
            () => tool.QueryPrices(instrument: "NOPE", ct: ct));

        ex.Message.ShouldContain("NOPE");
        ex.Message.ShouldContain("list_instruments");
    }

    [Fact]
    public async Task WithIndicators_true_populates_indicator_arrays()
    {
        var ct = TestContext.Current.CancellationToken;

        var bars = new[] { MakeBar(1, Ticker, new DateOnly(2026, 5, 1)) };
        var indicators = new IndicatorArrays(
            Rsi:          [45m],
            BollingerMid: [100m],
            Sma200:       [95m],
            Ichimoku:     [100m]);
        var response = new GetPriceSeriesOutput(bars, indicators);

        var (tool, useCase) = await BuildAsync([Ticker], response, ct: ct);

        var result = await tool.QueryPrices(
            instrument: Ticker,
            from: "2026-05-01",
            to: "2026-05-01",
            withIndicators: true,
            ct: ct);

        useCase.LastInput!.WithIndicators.ShouldBeTrue();
        result.Indicators.ShouldNotBeNull();
        result.Indicators!.Rsi.ShouldNotBeEmpty();
    }
}
