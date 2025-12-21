using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Query for portfolio performance history.
/// </summary>
/// <param name="PortfolioId">The portfolio ID.</param>
/// <param name="StartDate">Optional start date (defaults to 1 year ago).</param>
/// <param name="EndDate">Optional end date (defaults to today).</param>
public record PortfolioPerformanceQuery(
    int PortfolioId,
    DateTime? StartDate,
    DateTime? EndDate
);

/// <summary>
/// Inbound port (use case interface) for getting portfolio performance analytics.
/// </summary>
public interface IGetPortfolioPerformanceUseCase
{
    /// <summary>
    /// Gets portfolio performance history and metrics.
    /// </summary>
    /// <param name="query">The performance query.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <returns>The portfolio performance history.</returns>
    Task<PortfolioPerformanceHistory> ExecuteAsync(
        PortfolioPerformanceQuery query,
        IProgress<string>? progress = null);
}
