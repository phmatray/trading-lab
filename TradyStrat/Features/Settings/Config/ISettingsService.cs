namespace TradyStrat.Features.Settings.Config;

/// <summary>Raw key/value read/write over the Settings table. Scoped (rides AppDbContext).</summary>
public interface ISettingsService
{
    /// <summary>Raw stored string. Throws InvalidOperationException if the key has no row (= bug; startup seeds all keys).</summary>
    Task<string> GetRawAsync(string key, CancellationToken ct);

    /// <summary>
    /// Parsed via the key's SettingDescriptor and cast to T. T MUST be the descriptor's parsed type
    /// exactly (e.g. int for "anthropic.maxTokens", decimal for "polymarket.minVolumeUsd", string[] for
    /// "polymarket.searchQueries") — this is an unchecked unbox, so e.g. GetAsync&lt;long&gt; on an int-typed
    /// setting throws InvalidCastException. ISettingsReader always uses the right type. Also throws
    /// InvalidOperationException if the key has no row (delegates to <see cref="GetRawAsync"/>).
    /// </summary>
    Task<T> GetAsync<T>(string key, CancellationToken ct);

    /// <summary>Upserts the row and stamps UpdatedAt.</summary>
    Task SetAsync(string key, string rawValue, CancellationToken ct);

    /// <summary>MAX(UpdatedAt) over the given keys, or null if none of them have rows.</summary>
    Task<DateTime?> LastUpdatedAsync(IEnumerable<string> keys, CancellationToken ct);
}
