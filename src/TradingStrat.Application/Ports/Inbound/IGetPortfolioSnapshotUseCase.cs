using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Inbound port (use case interface) for getting portfolio snapshots with current market prices.
/// </summary>
public interface IGetPortfolioSnapshotUseCase
{
    /// <summary>
    /// Gets a current portfolio snapshot with live market prices.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID.</param>
    /// <param name="progress">Optional progress reporter for price fetching.</param>
    /// <returns>The portfolio snapshot.</returns>
    Task<PortfolioSnapshot> ExecuteAsync(
        int portfolioId,
        IProgress<string>? progress = null);
}
