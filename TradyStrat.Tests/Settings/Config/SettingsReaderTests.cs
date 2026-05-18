using TradyStrat.Features.Settings.Config;
using TradyStrat.Infrastructure.Settings.Config;
using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Infrastructure.Data;
using TradyStrat.Application.Settings.Config;
using TradyStrat.Tests.Common.Time;     // FakeClock
using TradyStrat.Tests.Fx;              // TestRepo<T>
using TradyStrat.Tests.Specifications;   // InMemoryDb
using Xunit;

namespace TradyStrat.Tests.Settings.Config;

public class SettingsReaderTests
{
    private static readonly string[] ExpectedQueries = ["btc", "fed"];

    private static (ISettingsService svc, ISettingsReader reader) Build(AppDbContext db)
    {
        var svc = new SettingsService(new TestRepo<SettingEntry>(db), new SettingsRegistry(), db,
            new FakeClock(new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc)));
        return (svc, new SettingsReader(svc));
    }

    [Fact]
    public async Task Anthropic_reflects_current_db_state()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        var (svc, reader) = Build(db);

        await svc.SetAsync(SettingsKeys.AnthropicModel, "claude-sonnet-4-6", ct);
        await svc.SetAsync(SettingsKeys.AnthropicMaxTokens, "4096", ct);

        var ai = await reader.AnthropicAsync(ct);
        ai.Model.ShouldBe("claude-sonnet-4-6");
        ai.MaxTokens.ShouldBe(4096);
    }

    [Fact]
    public async Task Polymarket_reflects_current_db_state()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        var (svc, reader) = Build(db);

        await svc.SetAsync(SettingsKeys.PolymarketSearchQueries, """["btc","fed"]""", ct);
        await svc.SetAsync(SettingsKeys.PolymarketMaxMarkets, "5", ct);
        await svc.SetAsync(SettingsKeys.PolymarketMinVolumeUsd, "10000", ct);
        await svc.SetAsync(SettingsKeys.PolymarketMaxHorizonDays, "90", ct);

        var p = await reader.PolymarketAsync(ct);
        p.SearchQueries.ShouldBe(ExpectedQueries);   // CA1861-extracted
        p.MaxMarkets.ShouldBe(5);
        p.MinVolumeUsd.ShouldBe(10000m);
        p.MaxHorizonDays.ShouldBe(90);
    }

    [Fact]
    public async Task FocusTicker_reflects_current_db_state()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        var (svc, reader) = Build(db);

        await svc.SetAsync(SettingsKeys.TickersFocus, "COIN", ct);

        (await reader.FocusTickerAsync(ct)).ShouldBe("COIN");
    }

    [Fact]
    public async Task LastUpdated_delegates_to_the_service()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        var (svc, reader) = Build(db);

        await svc.SetAsync(SettingsKeys.AnthropicModel, "claude-x", ct);

        (await reader.LastUpdatedAsync([SettingsKeys.AnthropicModel], ct))
            .ShouldBe(new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc));
        (await reader.LastUpdatedAsync([SettingsKeys.TickersFocus], ct)).ShouldBeNull();
    }
}
