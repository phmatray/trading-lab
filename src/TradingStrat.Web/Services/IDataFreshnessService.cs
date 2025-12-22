namespace TradingStrat.Web.Services;

/// <summary>
/// Service for checking data freshness and notifying users when data is stale.
/// </summary>
public interface IDataFreshnessService
{
    /// <summary>
    /// Checks if data for the specified ticker is stale and sends a notification if needed.
    /// </summary>
    /// <param name="ticker">The ticker symbol to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CheckAndNotifyAsync(string ticker, CancellationToken cancellationToken = default);
}
