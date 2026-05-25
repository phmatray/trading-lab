using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.Fx.Providers;
using TradyStrat.Application.PriceFeed.Providers;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Shared.Money;
using TradyStrat.Domain.Shared.Market;
using Xunit;

namespace TradyStrat.Application.Tests.Settings.UseCases;

public class ProbeInstrumentUseCaseTests
{
    private sealed class StubClock(DateTime now) : IClock
    {
        public DateTime UtcNow() => now;
        public DateOnly TodayLocal() => DateOnly.FromDateTime(now);
        public DateOnly TodayInExchangeTzFor(string ticker) => DateOnly.FromDateTime(now);
    }

    private static readonly StubClock _clock = new(new DateTime(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc));

    private sealed class FakePriceFeed(Instrument? probed = null,
                                       Exception? throwOnProbe = null) : IPriceFeed
    {
        public Task<IReadOnlyList<PriceBar>> FetchDailyAsync(
            string ticker, DateOnly from, DateOnly to, CancellationToken ct)
            => throw new NotImplementedException();

        public Task<Instrument> ProbeAsync(string ticker, CancellationToken ct)
            => throwOnProbe is null
                ? Task.FromResult(probed!)
                : Task.FromException<Instrument>(throwOnProbe);
    }

    private sealed class FakeFxProvider : IFxRateProvider
    {
        public Task<IReadOnlyList<FxRate>> FetchAsync(
            string @base, string quote, DateOnly from, DateOnly to, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<FxRate>>(
                [new FxRate(
                    from,
                    CurrencyPair.Of(Currency.Parse(@base), Currency.Parse(quote)),
                    1m,
                    DateTime.UtcNow)]);
    }

    [Fact]
    public async Task Returns_probed_instrument_for_eur_instrument()
    {
        var probed = Instrument.Probed(
            ticker:     "ETHE.PA",
            name:       "WisdomTree Physical Ethereum",
            currency:   Currency.Eur,
            exchange:   Exchange.Of("Euronext Paris"),
            timezoneId: TimezoneId.Of("Europe/Paris"),
            kind:       InstrumentKind.Held,
            now:        _clock.UtcNow());
        var sut = new ProbeInstrumentUseCase(
            new FakePriceFeed(probed), new FakeFxProvider(), _clock,
            NullLogger<ProbeInstrumentUseCase>.Instance);

        var result = await sut.ExecuteAsync(
            new ProbeInstrumentInput("ethe.pa", InstrumentKind.Held),
            TestContext.Current.CancellationToken);

        result.Ticker.ShouldBe("ETHE.PA");
        result.Currency.ShouldBe(Currency.Eur);
        result.Kind.ShouldBe(InstrumentKind.Held);
    }

    [Fact]
    public async Task Throws_InstrumentNotFoundException_when_ticker_is_blank()
    {
        var sut = new ProbeInstrumentUseCase(
            new FakePriceFeed(), new FakeFxProvider(), _clock,
            NullLogger<ProbeInstrumentUseCase>.Instance);

        await Should.ThrowAsync<InstrumentNotFoundException>(
            () => sut.ExecuteAsync(
                new ProbeInstrumentInput("  ", InstrumentKind.Held),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Bubbles_provider_exception_unchanged()
    {
        var sut = new ProbeInstrumentUseCase(
            new FakePriceFeed(throwOnProbe: new InstrumentNotFoundException("nope")),
            new FakeFxProvider(), _clock,
            NullLogger<ProbeInstrumentUseCase>.Instance);

        await Should.ThrowAsync<InstrumentNotFoundException>(
            () => sut.ExecuteAsync(
                new ProbeInstrumentInput("XYZ", InstrumentKind.Held),
                TestContext.Current.CancellationToken));
    }
}
