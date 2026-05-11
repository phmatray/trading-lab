using Shouldly;
using TradyStrat.Common.Domain;
using TradyStrat.Tests.Fx;             // TestRepo<T>
using TradyStrat.Tests.Specifications; // InMemoryDb
using Xunit;

namespace TradyStrat.Tests.Settings.Config;

public class SettingEntryRoundtripTests
{
    [Fact]
    public async Task Persists_and_reads_back_by_key()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        var repo = new TestRepo<SettingEntry>(db);

        await repo.AddAsync(
            new SettingEntry { Key = "anthropic.model", Value = "claude-opus-4-7", UpdatedAt = new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc) },
            ct);

        var got = await repo.GetByIdAsync("anthropic.model", ct);

        got.ShouldNotBeNull();
        got!.Value.ShouldBe("claude-opus-4-7");
    }
}
