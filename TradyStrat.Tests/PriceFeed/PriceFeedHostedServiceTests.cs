using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Data;
using TradyStrat.Features.Fx;
using TradyStrat.Features.Fx.Providers;
using TradyStrat.Features.PriceFeed;
using TradyStrat.Features.PriceFeed.Providers;
using TradyStrat.Features.Settings.UseCases;
using TradyStrat.Tests.Common.Time;
using TradyStrat.Tests.Fx;                  // TestRepo<T>
using Xunit;

namespace TradyStrat.Tests.PriceFeed;

public class PriceFeedHostedServiceTests
{
    // ListInstrumentsUseCase orders by Ticker. Ordinal: 'C' (0x43) < 'E' (0x45) < 'M' (0x4D).
    private static readonly string[] ExpectedTickers = ["CON3.L", "ETHE.PA", "MSFT"];
    private static readonly (string Base, string Quote)[] ExpectedFxPairs = [("EUR", "USD")];

    private sealed class RecordingPriceFeed : IPriceFeed
    {
        public List<string> WarmedTickers { get; } = [];

        public Task<IReadOnlyList<PriceBar>> FetchDailyAsync(
            string ticker, DateOnly from, DateOnly to, CancellationToken ct)
        {
            WarmedTickers.Add(ticker);
            return Task.FromResult<IReadOnlyList<PriceBar>>([]);
        }

        public Task<InstrumentMetadata> GetInstrumentMetadataAsync(string ticker, CancellationToken ct)
            => throw new NotImplementedException();
    }

    private sealed class RecordingFxProvider : IFxRateProvider
    {
        public List<(string Base, string Quote)> WarmedPairs { get; } = [];

        public Task<IReadOnlyList<FxRate>> FetchAsync(
            string @base, string quote, DateOnly from, DateOnly to, CancellationToken ct)
        {
            WarmedPairs.Add((@base, quote));
            return Task.FromResult<IReadOnlyList<FxRate>>([]);
        }
    }

    [Fact]
    public async Task Warms_each_instrument_and_one_fx_pair_per_distinct_currency()
    {
        // Shared in-memory DB name so every scope's AppDbContext sees the same store.
        var dbName = $"tradystrat-{Guid.NewGuid()}";
        var feed = new RecordingPriceFeed();
        var fxp  = new RecordingFxProvider();

        var sc = new ServiceCollection();
        sc.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(dbName));
        sc.AddScoped(typeof(IRepositoryBase<>),     typeof(TestRepo<>));
        sc.AddScoped(typeof(IReadRepositoryBase<>), typeof(TestRepo<>));
        sc.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)));
        sc.AddSingleton<IPriceFeed>(feed);
        sc.AddSingleton<IFxRateProvider>(fxp);
        sc.AddScoped<DailyPriceCache>();
        sc.AddScoped<DailyFxCache>();
        sc.AddScoped<ListInstrumentsUseCase>();
        sc.AddLogging();

        var sp = sc.BuildServiceProvider();

        // Seed in a separate scope so rows are committed before StartAsync resolves its own scope.
        using (var seed = sp.CreateScope())
        {
            var db = seed.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Instruments.AddRange(
                Inst("CON3.L",  "USD"),
                Inst("ETHE.PA", "EUR"),
                Inst("MSFT",    "USD"));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var sut = new PriceFeedHostedService(sp, NullLogger<PriceFeedHostedService>.Instance);
        await sut.StartAsync(TestContext.Current.CancellationToken);

        // Three instruments warmed in ascending Ticker order (driven by ListInstrumentsUseCase).
        feed.WarmedTickers.ShouldBe(ExpectedTickers);
        // Two non-EUR instruments (CON3.L, MSFT) share USD; ETHE.PA is EUR (skipped) → one FX pair.
        fxp.WarmedPairs.ShouldBe(ExpectedFxPairs);
    }

    private static Instrument Inst(string ticker, string currency) => new()
    {
        Id = 0, Ticker = ticker, Name = ticker, Currency = currency,
        Exchange = "X", TimezoneId = "Etc/UTC", Kind = InstrumentKind.Held,
        AddedAt = DateTime.UtcNow,
    };
}
