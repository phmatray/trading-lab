using TradyStrat.Domain.Settings.Polymarket;

namespace TradyStrat.Application.Settings;

public interface IPolymarketSettingsRepository
{
    Task<PolymarketSettings> GetAsync(CancellationToken ct);
    Task SaveAsync(PolymarketSettings settings, CancellationToken ct);
}
