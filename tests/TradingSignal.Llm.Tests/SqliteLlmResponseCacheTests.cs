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

    [Fact]
    public async Task Round_Trips_RawSignal_With_Reasoning()
    {
        string dbPath = TempDbPath();
        try
        {
            await using SqliteLlmResponseCache sut = new(dbPath);
            RawSignal signal = new(TradeAction.Buy, 0.7, "short reason", "the full thinking trace text");

            await sut.SetAsync("k", signal, CancellationToken.None);
            RawSignal? got = await sut.TryGetAsync("k", CancellationToken.None);

            got.ShouldNotBeNull();
            got!.Reasoning.ShouldBe("the full thinking trace text");
        }
        finally { Cleanup(dbPath); }
    }

    [Fact]
    public async Task Migrates_PreExisting_Db_Without_Reasoning_Column()
    {
        string dbPath = TempDbPath();
        try
        {
            // Create the DB with the OLD schema (no reasoning column).
            await using (Microsoft.Data.Sqlite.SqliteConnection conn = new($"Data Source={dbPath}"))
            {
                await conn.OpenAsync(TestContext.Current.CancellationToken);
                await using Microsoft.Data.Sqlite.SqliteCommand cmd = conn.CreateCommand();
                cmd.CommandText = """
                    CREATE TABLE llm_cache (
                        key TEXT PRIMARY KEY,
                        action INTEGER NOT NULL,
                        confidence REAL NOT NULL,
                        reason TEXT NOT NULL,
                        created_at TEXT NOT NULL
                    );
                    """;
                await cmd.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
            }
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

            // Open with new code — ALTER migration should add the column.
            await using SqliteLlmResponseCache sut = new(dbPath);
            await sut.SetAsync("k", new RawSignal(TradeAction.Sell, 0.3, "r", "trace"), CancellationToken.None);
            RawSignal? got = await sut.TryGetAsync("k", CancellationToken.None);

            got.ShouldNotBeNull();
            got!.Reasoning.ShouldBe("trace");
        }
        finally { Cleanup(dbPath); }
    }

    private static string TempDbPath()
        => Path.Combine(Path.GetTempPath(), $"tsig-llmcache-{Guid.NewGuid():N}.db");

    private static void Cleanup(string dbPath)
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(dbPath)) File.Delete(dbPath);
    }
}
