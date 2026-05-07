using Shouldly;
using TradyStrat.Features.Fx;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Tests.Specifications;
using Xunit;

namespace TradyStrat.Tests.Fx;

public class FxConverterTests
{
    [Fact]
    public async Task Converts_USD_to_EUR_using_latest_rate_at_or_before_date()
    {
        await using var db = InMemoryDb.Create();
        db.FxRates.AddRange(
            new FxRate { Id = 0, Pair = "EURUSD", Date = new(2026,5,5), UsdPerEur = 1.10m, FetchedAt = DateTime.UtcNow },
            new FxRate { Id = 0, Pair = "EURUSD", Date = new(2026,5,6), UsdPerEur = 1.0820m, FetchedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var fx = new FxConverter(new TestRepo<FxRate>(db));

        var eur = await fx.UsdToEurAsync(216.40m, new(2026,5,6), TestContext.Current.CancellationToken);

        eur.ShouldBe(216.40m / 1.0820m, tolerance: 0.000001m);
    }

    [Fact]
    public async Task Throws_FxRateUnavailableException_when_no_rate_at_or_before()
    {
        await using var db = InMemoryDb.Create();
        var fx = new FxConverter(new TestRepo<FxRate>(db));

        await Should.ThrowAsync<FxRateUnavailableException>(() =>
            fx.UsdToEurAsync(100m, new(2026,5,6), TestContext.Current.CancellationToken));
    }
}
