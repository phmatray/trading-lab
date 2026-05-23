using Shouldly;
using TradyStrat.Application.Settings.Config;
using TradyStrat.Domain;
using TradyStrat.Domain.Settings.Polymarket;
using TradyStrat.Infrastructure.Settings;
using TradyStrat.TestKit.Specifications;
using TradyStrat.TestKit.Time;
using Xunit;

namespace TradyStrat.Infrastructure.Tests.Settings;

public class EfPolymarketSettingsRepositoryTests
{
    private static readonly string[] SeededQueries = ["bitcoin", "ethereum"];

    [Fact]
    public async Task RoundTrips_through_VOs()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        Seed(db, SettingsKeys.PolymarketSearchQueries, "[\"bitcoin\",\"ethereum\"]");
        Seed(db, SettingsKeys.PolymarketMaxMarkets, "10");
        Seed(db, SettingsKeys.PolymarketMinVolumeUsd, "5000");
        Seed(db, SettingsKeys.PolymarketMaxHorizonDays, "30");
        await db.SaveChangesAsync(ct);

        var repo = new EfPolymarketSettingsRepository(db, new FakeClock(DateTime.UtcNow));
        var loaded = await repo.GetAsync(ct);

        loaded.SearchQueries.Values.ShouldBe(SeededQueries);
        loaded.MaxMarkets.Value.ShouldBe(10);
        loaded.MinVolumeUsd.Value.ShouldBe(5000m);
        loaded.MaxHorizonDays.Value.ShouldBe(30);

        var updated = loaded with { MaxMarkets = MaxMarkets.Of(25) };
        await repo.SaveAsync(updated, ct);

        var reloaded = await new EfPolymarketSettingsRepository(db, new FakeClock(DateTime.UtcNow)).GetAsync(ct);
        reloaded.MaxMarkets.Value.ShouldBe(25);
        // Assert an untouched field also survives the round-trip (catches null-overwrite regressions).
        reloaded.SearchQueries.Values.ShouldBe(SeededQueries);
        reloaded.MinVolumeUsd.Value.ShouldBe(5000m);
    }

    private static void Seed(Infrastructure.Data.AppDbContext db, string key, string value)
        => db.Add(new SettingEntry { Key = key, Value = value, UpdatedAt = DateTime.UtcNow });
}
