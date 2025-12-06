using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.Ports.Outbound;

/// <summary>
/// Outbound port for fetching live market data from external API sources.
/// Abstracts the underlying market data provider (Yahoo Finance API).
/// Replaces IYahooFinanceService from the original architecture.
/// </summary>
public interface IMarketDataPort
{
    /// <summary>
    /// Fetches historical price data for a ticker from an external market data source.
    /// </summary>
    /// <param name="ticker">Stock ticker symbol (e.g., "CON3.L").</param>
    /// <param name="startDate">Start date for historical data (inclusive).</param>
    /// <param name="endDate">End date for historical data (inclusive).</param>
    /// <returns>Read-only list of historical price records from the external source.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the ticker is not found or API fails.</exception>
    Task<IReadOnlyList<HistoricalPrice>> FetchHistoricalDataAsync(
        string ticker,
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Fetches the most recent price data for a ticker.
    /// Used for live analysis and current position evaluation.
    /// </summary>
    /// <param name="ticker">Stock ticker symbol.</param>
    /// <returns>Latest price record, or null if ticker is not found.</returns>
    Task<HistoricalPrice?> FetchLatestPriceAsync(string ticker);
}
