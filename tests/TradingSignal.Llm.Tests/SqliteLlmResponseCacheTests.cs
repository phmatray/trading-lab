using Shouldly;
using TradingSignal.Core;
using TradingSignal.Llm.Caching;

namespace TradingSignal.Llm.Tests;

public sealed class SqliteLlmResponseCacheTests : IAsyncLifetime, IDisposable
{
    private readonly string _dbPath;

    public SqliteLlmResponseCacheTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"tsig-llm-cache-{Guid.NewGuid():N}.db");
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { /* best effort */ }
        }
    }

    [Fact]
    public async Task RoundTrips_Signal()
    {
        await using SqliteLlmResponseCache cache = new(_dbPath);

        string key = "abc123";
        RawSignal signal = new(TradeAction.Buy, 0.73, "test reason");

        (await cache.TryGetAsync(key, CancellationToken.None)).ShouldBeNull();
        await cache.SetAsync(key, signal, CancellationToken.None);
        RawSignal? loaded = await cache.TryGetAsync(key, CancellationToken.None);

        loaded.ShouldNotBeNull();
        loaded.ShouldBe(signal);
    }

    [Fact]
    public async Task Overwrites_On_Set()
    {
        await using SqliteLlmResponseCache cache = new(_dbPath);

        await cache.SetAsync("k", new RawSignal(TradeAction.Buy, 0.5, "v1"), CancellationToken.None);
        await cache.SetAsync("k", new RawSignal(TradeAction.Sell, 0.9, "v2"), CancellationToken.None);

        RawSignal? loaded = await cache.TryGetAsync("k", CancellationToken.None);

        loaded.ShouldNotBeNull();
        loaded.Action.ShouldBe(TradeAction.Sell);
        loaded.Reason.ShouldBe("v2");
    }
}
