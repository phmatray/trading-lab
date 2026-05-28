using Microsoft.Data.Sqlite;
using TradingSignal.Core;

namespace TradingSignal.Llm.Caching;

public sealed class SqliteLlmResponseCache : ILlmResponseCache, IAsyncDisposable
{
    private readonly string _connString;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _initialized;

    public SqliteLlmResponseCache(string dbPath)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(dbPath));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        _connString = new SqliteConnectionStringBuilder { DataSource = dbPath, Cache = SqliteCacheMode.Shared }.ToString();
    }

    private async ValueTask EnsureInitializedAsync(CancellationToken ct)
    {
        if (_initialized) return;
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_initialized) return;
            await using var conn = new SqliteConnection(_connString);
            await conn.OpenAsync(ct).ConfigureAwait(false);

            await using (var create = conn.CreateCommand())
            {
                create.CommandText = """
                    CREATE TABLE IF NOT EXISTS llm_cache (
                        key TEXT PRIMARY KEY,
                        action INTEGER NOT NULL,
                        confidence REAL NOT NULL,
                        reason TEXT NOT NULL,
                        reasoning TEXT NULL,
                        created_at TEXT NOT NULL
                    );
                    """;
                await create.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            // Idempotent migration for pre-existing DBs.
            await using (var alter = conn.CreateCommand())
            {
                alter.CommandText = "ALTER TABLE llm_cache ADD COLUMN reasoning TEXT NULL";
                try { await alter.ExecuteNonQueryAsync(ct).ConfigureAwait(false); }
                catch (SqliteException) { /* column already exists — expected on fresh DBs */ }
            }

            _initialized = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<RawSignal?> TryGetAsync(string key, CancellationToken ct)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);

        await using var conn = new SqliteConnection(_connString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT action, confidence, reason, reasoning FROM llm_cache WHERE key = $key";
        cmd.Parameters.AddWithValue("$key", key);

        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false)) return null;

        var action = (TradeAction)reader.GetInt32(0);
        var confidence = reader.GetDouble(1);
        var reason = reader.GetString(2);
        string? reasoning = await reader.IsDBNullAsync(3, ct).ConfigureAwait(false) ? null : reader.GetString(3);
        return new RawSignal(action, confidence, reason, reasoning);
    }

    public async Task SetAsync(string key, RawSignal signal, CancellationToken ct)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);

        await using var conn = new SqliteConnection(_connString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO llm_cache (key, action, confidence, reason, reasoning, created_at)
            VALUES ($key, $action, $confidence, $reason, $reasoning, $createdAt);
            """;
        cmd.Parameters.AddWithValue("$key", key);
        cmd.Parameters.AddWithValue("$action", (int)signal.Action);
        cmd.Parameters.AddWithValue("$confidence", signal.Confidence);
        cmd.Parameters.AddWithValue("$reason", signal.Reason);
        cmd.Parameters.AddWithValue("$reasoning", (object?)signal.Reasoning ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$createdAt", DateTime.UtcNow.ToString("o"));
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync()
    {
        _gate.Dispose();
        SqliteConnection.ClearAllPools();
        return ValueTask.CompletedTask;
    }
}
