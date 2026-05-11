using System.Globalization;
using Shouldly;
using TradyStrat.Common.Domain;
using TradyStrat.Data;
using TradyStrat.Features.Settings.Config;
using TradyStrat.Tests.Common.Time;     // FakeClock
using TradyStrat.Tests.Fx;              // TestRepo<T>
using TradyStrat.Tests.Specifications;   // InMemoryDb
using Xunit;

namespace TradyStrat.Tests.Settings.Config;

public class SettingsServiceTests
{
    private static SettingsService NewService(AppDbContext db)
        => new(new TestRepo<SettingEntry>(db), new SettingsRegistry(), db,
               new FakeClock(new DateTime(2026, 5, 11, 12, 0, 0, DateTimeKind.Utc)));

    [Fact]
    public async Task Set_then_GetRaw_round_trips()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        var svc = NewService(db);

        await svc.SetAsync(SettingsKeys.AnthropicModel, "claude-3-5-haiku", ct);

        (await svc.GetRawAsync(SettingsKeys.AnthropicModel, ct)).ShouldBe("claude-3-5-haiku");
    }

    [Fact]
    public async Task Set_then_Set_again_updates_in_place()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        var svc = NewService(db);

        await svc.SetAsync(SettingsKeys.PolymarketMaxMarkets, "8", ct);
        await svc.SetAsync(SettingsKeys.PolymarketMaxMarkets, "12", ct);

        (await svc.GetAsync<int>(SettingsKeys.PolymarketMaxMarkets, ct)).ShouldBe(12);
        db.Settings.Count(e => e.Key == SettingsKeys.PolymarketMaxMarkets).ShouldBe(1);
    }

    [Fact]
    public async Task GetAsync_parses_typed_values()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        var svc = NewService(db);

        await svc.SetAsync(SettingsKeys.AnthropicMaxTokens, "2048", ct);
        await svc.SetAsync(SettingsKeys.PolymarketMinVolumeUsd, "75000", ct);
        await svc.SetAsync(SettingsKeys.PolymarketSearchQueries, """["btc","eth"]""", ct);

        (await svc.GetAsync<int>(SettingsKeys.AnthropicMaxTokens, ct)).ShouldBe(2048);
        (await svc.GetAsync<decimal>(SettingsKeys.PolymarketMinVolumeUsd, ct)).ShouldBe(75000m);
        (await svc.GetAsync<string[]>(SettingsKeys.PolymarketSearchQueries, ct)).ShouldBe(ExpectedQueries);   // see CA1861 note
    }

    private static readonly string[] ExpectedQueries = ["btc", "eth"];

    [Fact]
    public async Task Numeric_parse_is_culture_invariant()
    {
        var prev = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR"); // comma decimal separator
        try
        {
            await using var db = InMemoryDb.Create();
            var ct = TestContext.Current.CancellationToken;
            var svc = NewService(db);

            await svc.SetAsync(SettingsKeys.PolymarketMinVolumeUsd, "50000", ct);
            (await svc.GetAsync<decimal>(SettingsKeys.PolymarketMinVolumeUsd, ct)).ShouldBe(50000m);
        }
        finally { Thread.CurrentThread.CurrentCulture = prev; }
    }

    [Fact]
    public async Task GetRaw_missing_key_throws_InvalidOperationException()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        var svc = NewService(db);

        await Should.ThrowAsync<InvalidOperationException>(() => svc.GetRawAsync(SettingsKeys.TickersFocus, ct));
    }

    [Fact]
    public async Task LastUpdated_returns_max_over_keys()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        var clock = new FakeClock(new DateTime(2026, 5, 11, 9, 0, 0, DateTimeKind.Utc));
        var svc = new SettingsService(new TestRepo<SettingEntry>(db), new SettingsRegistry(), db, clock);

        await svc.SetAsync(SettingsKeys.PolymarketMaxMarkets, "8", ct);
        clock.Now = new DateTime(2026, 5, 11, 10, 30, 0, DateTimeKind.Utc);
        await svc.SetAsync(SettingsKeys.PolymarketMinVolumeUsd, "50000", ct);

        var last = await svc.LastUpdatedAsync(
            [SettingsKeys.PolymarketMaxMarkets, SettingsKeys.PolymarketMinVolumeUsd], ct);

        last.ShouldBe(new DateTime(2026, 5, 11, 10, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task LastUpdated_returns_null_when_no_rows_match()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        var svc = NewService(db);

        (await svc.LastUpdatedAsync([SettingsKeys.TickersFocus], ct)).ShouldBeNull();
    }
}
