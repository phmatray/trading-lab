using Shouldly;
using TradyStrat.Application.Settings.Config;
using TradyStrat.Domain;
using TradyStrat.Domain.Settings.Anthropic;
using TradyStrat.Infrastructure.Settings;
using TradyStrat.TestKit.Specifications;
using TradyStrat.TestKit.Time;
using Xunit;

namespace TradyStrat.Infrastructure.Tests.Settings;

public class EfAnthropicSettingsRepositoryTests
{
    [Fact]
    public async Task RoundTrips_through_VOs()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        Seed(db, SettingsKeys.AnthropicModel, "claude-opus-4-7");
        Seed(db, SettingsKeys.AnthropicMaxTokens, "1500");
        Seed(db, SettingsKeys.AnthropicThinkingBudget, "8192");
        Seed(db, SettingsKeys.AnthropicMaxParallelSuggestions, "3");
        await db.SaveChangesAsync(ct);

        var repo = new EfAnthropicSettingsRepository(db, new FakeClock(DateTime.UtcNow));
        var loaded = await repo.GetAsync(ct);

        loaded.Model.Value.ShouldBe("claude-opus-4-7");
        loaded.MaxTokens.Value.ShouldBe(1500);

        var updated = loaded with { MaxTokens = MaxTokens.Of(2000) };
        await repo.SaveAsync(updated, ct);

        var reloaded = await new EfAnthropicSettingsRepository(db, new FakeClock(DateTime.UtcNow)).GetAsync(ct);
        reloaded.MaxTokens.Value.ShouldBe(2000);
    }

    private static void Seed(Infrastructure.Data.AppDbContext db, string key, string value)
        => db.Add(new SettingEntry { Key = key, Value = value, UpdatedAt = DateTime.UtcNow });
}
