using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Application.Fx.Providers;
using TradyStrat.Application.PriceFeed.Providers;
using TradyStrat.Application.Settings.UseCases;
using Xunit;

namespace TradyStrat.Infrastructure.Tests.Settings.UseCases;

public class ProbeInstrumentUseCaseTests
{
    private sealed class FakePriceFeed(InstrumentMetadata? meta = null,
                                       Exception? throwOnMeta = null) : IPriceFeed
    {
        public Task<IReadOnlyList<PriceBar>> FetchDailyAsync(
            string ticker, DateOnly from, DateOnly to, CancellationToken ct)
            => throw new NotImplementedException();

        public Task<InstrumentMetadata> GetInstrumentMetadataAsync(string ticker, CancellationToken ct)
            => throwOnMeta is null
                ? Task.FromResult(meta!)
                : Task.FromException<InstrumentMetadata>(throwOnMeta);
    }

    private sealed class FakeFxProvider : IFxRateProvider
    {
        public Task<IReadOnlyList<FxRate>> FetchAsync(
            string @base, string quote, DateOnly from, DateOnly to, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<FxRate>>(
                [new FxRate { Id = 0, Base = @base, Quote = quote, Date = from, Rate = 1m, FetchedAt = DateTime.UtcNow }]);
    }

    [Fact]
    public async Task Returns_metadata_for_eur_instrument()
    {
        var meta = new InstrumentMetadata(
            "ETHE.PA", "WisdomTree Physical Ethereum", "EUR", "Euronext Paris", "Europe/Paris");
        var sut = new ProbeInstrumentUseCase(
            new FakePriceFeed(meta), new FakeFxProvider(),
            NullLogger<ProbeInstrumentUseCase>.Instance);

        var result = await sut.ExecuteAsync(new ProbeInstrumentInput("ethe.pa"), TestContext.Current.CancellationToken);

        result.ShouldBe(meta);
    }

    [Fact]
    public async Task Throws_InstrumentNotFoundException_when_ticker_is_blank()
    {
        var sut = new ProbeInstrumentUseCase(
            new FakePriceFeed(), new FakeFxProvider(),
            NullLogger<ProbeInstrumentUseCase>.Instance);

        await Should.ThrowAsync<InstrumentNotFoundException>(
            () => sut.ExecuteAsync(new ProbeInstrumentInput("  "), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Bubbles_provider_exception_unchanged()
    {
        var sut = new ProbeInstrumentUseCase(
            new FakePriceFeed(throwOnMeta: new InstrumentNotFoundException("nope")),
            new FakeFxProvider(),
            NullLogger<ProbeInstrumentUseCase>.Instance);

        await Should.ThrowAsync<InstrumentNotFoundException>(
            () => sut.ExecuteAsync(new ProbeInstrumentInput("XYZ"), TestContext.Current.CancellationToken));
    }
}
