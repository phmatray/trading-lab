using Shouldly;
using TradyStrat.Application.Fx;
using TradyStrat.Domain;
using TradyStrat.Domain.MarketData;
using TradyStrat.Domain.Shared.Money;
using TradyStrat.Infrastructure.Fx;
using TradyStrat.TestKit.Specifications;
using Xunit;

namespace TradyStrat.Tests.Fx;

public class FxConverterTests
{
    private static readonly CurrencyPair EurUsd = CurrencyPair.Of(Currency.Eur, Currency.Usd);

    [Fact]
    public async Task Converts_USD_to_EUR_using_latest_rate_at_or_before_date()
    {
        await using var db = InMemoryDb.Create();
        db.FxRates.AddRange(
            new FxRate(new(2026, 5, 5), EurUsd, 1.10m,   DateTime.UtcNow),
            new FxRate(new(2026, 5, 6), EurUsd, 1.0820m, DateTime.UtcNow));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var fx = new FxConverter(new EfFxRateReadRepository(db));

        var eur = await fx.ToEurAsync(216.40m, "USD", new(2026, 5, 6), TestContext.Current.CancellationToken);

        eur.ShouldBe(216.40m / 1.0820m, tolerance: 0.000001m);
    }

    [Fact]
    public async Task Throws_FxRateUnavailableException_when_no_rate_at_or_before()
    {
        await using var db = InMemoryDb.Create();
        var fx = new FxConverter(new EfFxRateReadRepository(db));

        await Should.ThrowAsync<FxRateUnavailableException>(() =>
            fx.ToEurAsync(100m, "USD", new(2026, 5, 6), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ToEurAsync_passes_through_when_currency_is_eur()
    {
        await using var db = InMemoryDb.Create();
        var fx = new FxConverter(new EfFxRateReadRepository(db));

        var result = await fx.ToEurAsync(123.45m, "EUR",
            new DateOnly(2026, 5, 1), TestContext.Current.CancellationToken);

        result.ShouldBe(123.45m);
    }
}
