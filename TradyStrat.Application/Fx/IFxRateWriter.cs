using TradyStrat.Domain;

namespace TradyStrat.Application.Fx;

/// <summary>
/// Write-side port for FxRate persistence. Implemented only by the EF
/// adapter; consumed only by hosted services (DailyFxCache).
/// Phase 5 scaffolding — the existing hosted-service write paths still use
/// AppDbContext directly until a future consolidation phase.
/// </summary>
public interface IFxRateWriter
{
    Task AppendAsync(IReadOnlyList<FxRate> rates, CancellationToken ct);
}
