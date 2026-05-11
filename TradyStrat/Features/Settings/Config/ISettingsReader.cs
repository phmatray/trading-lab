namespace TradyStrat.Features.Settings.Config;

/// <summary>Typed facade over ISettingsService for the app's consumers (incl. the Settings-page forms).</summary>
public interface ISettingsReader
{
    Task<AnthropicSettings> AnthropicAsync(CancellationToken ct);
    Task<PolymarketSettings> PolymarketAsync(CancellationToken ct);
    Task<string> FocusTickerAsync(CancellationToken ct);

    /// <summary>MAX(UpdatedAt) over the given keys, or null if none have rows. Used by the forms to show "last updated".</summary>
    Task<DateTime?> LastUpdatedAsync(IEnumerable<string> keys, CancellationToken ct);
}
