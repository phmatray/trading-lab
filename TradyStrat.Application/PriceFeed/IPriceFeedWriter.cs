using TradyStrat.Domain;

namespace TradyStrat.Application.PriceFeed;

/// <summary>
/// Write-side port for PriceBar persistence. Implemented only by the EF
/// adapter; consumed only by hosted services (DailyPriceCache).
/// Phase 5 scaffolding — the existing hosted-service write paths still use
/// AppDbContext directly until a future consolidation phase.
/// </summary>
public interface IPriceFeedWriter
{
    Task AppendAsync(IReadOnlyList<PriceBar> bars, CancellationToken ct);
}
