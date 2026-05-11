using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Time;
using TradyStrat.Data;
using TradyStrat.Features.Settings.Config;
using TradyStrat.Tests.Common.Time;     // FakeClock
using TradyStrat.Tests.Specifications;   // InMemoryDb
using Xunit;

namespace TradyStrat.Tests.Settings.Config;

public class SettingsSeederTests
{
    // Wire a minimal DI container that hands the seeder a scope yielding the given AppDbContext + a FakeClock.
    private static SettingsSeederHostedService BuildSeeder(AppDbContext db)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc)));
        var sp = services.BuildServiceProvider();
        return new SettingsSeederHostedService(sp.GetRequiredService<IServiceScopeFactory>(), new SettingsRegistry());
    }

    [Fact]
    public async Task Seeds_all_keys_with_documented_defaults()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;

        await BuildSeeder(db).StartAsync(ct);

        var rows = db.Settings.ToDictionary(e => e.Key, e => e.Value);
        rows[SettingsKeys.AnthropicModel].ShouldBe("claude-opus-4-7");
        rows[SettingsKeys.AnthropicMaxTokens].ShouldBe("1500");
        rows[SettingsKeys.PolymarketSearchQueries].ShouldBe("""["bitcoin","ethereum","coinbase","fed"]""");
        rows[SettingsKeys.PolymarketMaxMarkets].ShouldBe("8");
        rows[SettingsKeys.PolymarketMinVolumeUsd].ShouldBe("50000");
        rows[SettingsKeys.PolymarketMaxHorizonDays].ShouldBe("365");
        rows[SettingsKeys.TickersFocus].ShouldBe("CON3.L");
        rows.Count.ShouldBe(7);
    }

    [Fact]
    public async Task Is_idempotent_and_does_not_overwrite_existing_rows()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        db.Settings.Add(new SettingEntry { Key = SettingsKeys.TickersFocus, Value = "COIN", UpdatedAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc) });
        await db.SaveChangesAsync(ct);

        await BuildSeeder(db).StartAsync(ct);
        await BuildSeeder(db).StartAsync(ct);   // run twice

        db.Settings.Count().ShouldBe(7);
        db.Settings.Single(e => e.Key == SettingsKeys.TickersFocus).Value.ShouldBe("COIN");   // preserved
    }
}
