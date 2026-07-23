using TradingStrat.Domain.Common;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Inbound port (use case interface) for getting portfolio metrics displayed in the top bar.
/// Calculates portfolio value, today's return, and win rate efficiently.
/// </summary>
public interface IGetPortfolioTopBarMetricsUseCase
{
    /// <summary>
    /// Gets top bar metrics for the specified portfolio.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID.</param>
    /// <param name="progress">Optional progress reporter for data fetching.</param>
    /// <returns>Result containing the top bar metrics, or errors if operation failed.</returns>
    Task<Result<TopBarMetrics>> ExecuteAsync(
        int portfolioId,
        IProgress<string>? progress = null);
}
