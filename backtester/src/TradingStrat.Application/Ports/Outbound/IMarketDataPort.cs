using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Ports.Outbound;

/// <summary>
/// Outbound port for fetching live market data from external API sources.
/// Abstracts the underlying market data provider (Yahoo Finance for daily, Alpha Vantage for intraday).
/// Supports multiple timeframes (M1, M5, M15, M30, H1, H4, D1, W1, MN1).
/// Replaces IYahooFinanceService from the original architecture.
/// </summary>
public interface IMarketDataPort
{
    /// <summary>
    /// Fetches historical price data for a ticker at a specific timeframe from an external market data source.
    /// The adapter implementation determines the appropriate data provider based on timeframe
    /// (e.g., Yahoo Finance for daily, Alpha Vantage for intraday).
    /// </summary>
    /// <param name="ticker">Stock ticker symbol (e.g., "CON3.L", "AAPL").</param>
    /// <param name="timeFrame">Timeframe for the data (M1, M5, M15, M30, H1, H4, D1, W1, MN1).</param>
    /// <param name="startDate">Start date for historical data (inclusive).</param>
    /// <param name="endDate">End date for historical data (inclusive).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Read-only list of historical price records from the external source.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the ticker is not found or API fails.</exception>
    Task<IReadOnlyList<HistoricalPrice>> FetchHistoricalDataAsync(
        string ticker,
        TimeFrame timeFrame,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the most recent price data for a ticker.
    /// Used for live analysis and current position evaluation.
    /// Returns daily data by default (D1 timeframe).
    /// </summary>
    /// <param name="ticker">Stock ticker symbol.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Latest price record, or null if ticker is not found.</returns>
    Task<HistoricalPrice?> FetchLatestPriceAsync(string ticker, CancellationToken cancellationToken = default);
}
