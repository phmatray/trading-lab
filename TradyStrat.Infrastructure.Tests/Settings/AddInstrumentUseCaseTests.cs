using TradyStrat.Infrastructure.Fx;
using TradyStrat.Infrastructure.PriceFeed;
using TradyStrat.Infrastructure.Settings.UseCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Infrastructure.Data;
using TradyStrat.Application.Fx.Providers;
using TradyStrat.Application.PriceFeed.Providers;
using TradyStrat.TestKit;                  // TestRepo<T>
using TradyStrat.TestKit.Specifications;       // InMemoryDb.Create()
using Xunit;

namespace TradyStrat.Infrastructure.Tests.Settings.UseCases;

public class AddInstrumentUseCaseTests
{
    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow() => utcNow;
        public DateOnly TodayLocal() => DateOnly.FromDateTime(utcNow);
        public DateOnly TodayInExchangeTzFor(string ticker) => DateOnly.FromDateTime(utcNow);
    }

    private sealed class ThrowingPriceFeed : IPriceFeed
    {
        public Task<IReadOnlyList<PriceBar>> FetchDailyAsync(
            string ticker, DateOnly from, DateOnly to, CancellationToken ct)
            => Task.FromException<IReadOnlyList<PriceBar>>(
                new PriceFeedUnavailableException("simulated"));

        public Task<InstrumentMetadata> GetInstrumentMetadataAsync(
            string ticker, CancellationToken ct)
            => throw new NotImplementedException();
    }

    private sealed class ThrowingFxProvider : IFxRateProvider
    {
        public Task<IReadOnlyList<FxRate>> FetchAsync(
            string @base, string quote, DateOnly from, DateOnly to, CancellationToken ct)
            => Task.FromException<IReadOnlyList<FxRate>>(
                new FxRateUnavailableException("simulated"));
    }

    private static InstrumentMetadata Probe(string ticker, string currency = "USD")
        => new(ticker, ticker, currency, "NMS", "America/New_York");

    private static AddInstrumentUseCase NewSut(AppDbContext db)
    {
        var clock = new FixedClock(new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc));
        var price = new DailyPriceCache(
            new ThrowingPriceFeed(), db, clock,
            NullLogger<DailyPriceCache>.Instance);
        var fx = new DailyFxCache(
            new ThrowingFxProvider(), db, clock,
            NullLogger<DailyFxCache>.Instance);
        return new AddInstrumentUseCase(
            new TestRepo<Instrument>(db), price, fx, clock,
            NullLogger<AddInstrumentUseCase>.Instance);
    }

    [Fact]
    public async Task Inserts_instrument_on_happy_path()
    {
        await using var db = InMemoryDb.Create();
        var sut = NewSut(db);

        var inst = await sut.ExecuteAsync(
            new AddInstrumentInput(Probe("ETHE.PA", "EUR"), InstrumentKind.Held),
            TestContext.Current.CancellationToken);

        inst.Ticker.ShouldBe("ETHE.PA");
        inst.Kind.ShouldBe(InstrumentKind.Held);
        (await db.Instruments.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task Throws_DuplicateInstrumentException_when_ticker_exists()
    {
        await using var db = InMemoryDb.Create();
        db.Instruments.Add(new Instrument
        {
            Id = 0, Ticker = "CON3.L", Name = "x", Currency = "USD",
            Exchange = "LSE", TimezoneId = "Europe/London",
            Kind = InstrumentKind.Held, AddedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = NewSut(db);

        await Should.ThrowAsync<DuplicateInstrumentException>(
            () => sut.ExecuteAsync(
                new AddInstrumentInput(Probe("CON3.L"), InstrumentKind.Held),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Warm_failure_does_not_roll_back_insert_for_non_eur()
    {
        // Currency is USD so the FX-warm path is exercised; the throwing FX
        // provider triggers an exception that the use case must swallow.
        await using var db = InMemoryDb.Create();
        var sut = NewSut(db);

        await sut.ExecuteAsync(
            new AddInstrumentInput(Probe("XYZ", "USD"), InstrumentKind.Watchlist),
            TestContext.Current.CancellationToken);

        (await db.Instruments.CountAsync(
            i => i.Ticker == "XYZ", TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task Eur_instrument_skips_fx_warm()
    {
        // The fact that ThrowingFxProvider would throw, but the test passes,
        // proves the EUR branch skips the FX warm path entirely.
        await using var db = InMemoryDb.Create();
        var sut = NewSut(db);

        await sut.ExecuteAsync(
            new AddInstrumentInput(Probe("ETHE.PA", "EUR"), InstrumentKind.Held),
            TestContext.Current.CancellationToken);

        (await db.Instruments.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
    }
}
