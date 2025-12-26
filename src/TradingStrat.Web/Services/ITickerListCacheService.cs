using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Web.Services;

/// <summary>
/// Service for caching ticker lists and summaries to reduce database queries.
/// Used by UI components that display dropdown lists or autocomplete.
/// </summary>
public interface ITickerListCacheService
{
    /// <summary>
    /// Gets list of all unique tickers from cache or database.
    /// </summary>
    /// <param name="timeFrame">Timeframe to get tickers for.</param>
    /// <returns>List of ticker symbols.</returns>
    Task<List<string>> GetTickersAsync(TimeFrame timeFrame);

    /// <summary>
    /// Gets ticker summaries (ticker + record count) from cache or database.
    /// </summary>
    /// <param name="timeFrame">Timeframe to get summaries for.</param>
    /// <returns>List of ticker summaries with metadata.</returns>
    Task<List<TickerSummary>> GetTickerSummariesAsync(TimeFrame timeFrame);

    /// <summary>
    /// Invalidates the ticker cache for a specific timeframe.
    /// Call this after adding or deleting ticker data.
    /// </summary>
    /// <param name="timeFrame">Timeframe to invalidate, or null for all timeframes.</param>
    void InvalidateCache(TimeFrame? timeFrame = null);

    /// <summary>
    /// Gets the count of tickers for a specific timeframe.
    /// Uses cached data if available.
    /// </summary>
    /// <param name="timeFrame">Timeframe to count tickers for.</param>
    /// <returns>Number of tickers with data for the timeframe.</returns>
    Task<int> GetTickerCountAsync(TimeFrame timeFrame);
}
