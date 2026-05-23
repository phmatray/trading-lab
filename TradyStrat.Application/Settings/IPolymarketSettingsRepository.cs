using TradyStrat.Domain.Settings.Polymarket;

namespace TradyStrat.Application.Settings;

public interface IPolymarketSettingsRepository
{
    Task<PolymarketSettings> GetAsync(CancellationToken ct);
    Task SaveAsync(PolymarketSettings settings, CancellationToken ct);

    /// <summary>MAX(UpdatedAt) over the Polymarket setting rows, or null if none exist. Used by the Settings form to show "last saved at".</summary>
    Task<DateTime?> LastUpdatedAsync(CancellationToken ct);
}
