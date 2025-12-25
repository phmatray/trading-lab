using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Ports.Outbound;

/// <summary>
/// Outbound port (repository pattern) for accessing historical price data storage.
/// Abstracts the underlying database implementation (EF Core + SQLite).
/// Supports multiple timeframes (M1, M5, M15, M30, H1, H4, D1, W1, MN1).
/// Replaces IDataRepository from the original architecture.
/// </summary>
public interface IHistoricalDataPort
{
    /// <summary>
    /// Saves historical price data to persistent storage, filtering out duplicates.
    /// Unique constraint is enforced on (Ticker, TimeFrame, DateTime).
    /// </summary>
    /// <param name="ticker">Stock ticker symbol.</param>
    /// <param name="isin">Optional ISIN code for the security.</param>
    /// <param name="timeFrame">Timeframe of the data being saved.</param>
    /// <param name="data">Historical price records to save.</param>
    Task SaveHistoricalDataAsync(string ticker, string? isin, TimeFrame timeFrame, IEnumerable<HistoricalPrice> data);

    /// <summary>
    /// Gets the most recent date for which data exists for a ticker at a specific timeframe.
    /// </summary>
    /// <param name="ticker">Stock ticker symbol.</param>
    /// <param name="timeFrame">Timeframe to query.</param>
    /// <returns>Latest date with data, or null if no data exists.</returns>
    Task<DateTime?> GetLatestDataDateAsync(string ticker, TimeFrame timeFrame);

    /// <summary>
    /// Retrieves all historical data for a ticker at a specific timeframe, ordered by date ascending.
    /// </summary>
    /// <param name="ticker">Stock ticker symbol.</param>
    /// <param name="timeFrame">Timeframe to query.</param>
    /// <returns>List of all historical price records for the specified timeframe.</returns>
    Task<List<HistoricalPrice>> GetHistoricalDataAsync(string ticker, TimeFrame timeFrame);

    /// <summary>
    /// Retrieves historical data for a ticker at a specific timeframe within a date range.
    /// </summary>
    /// <param name="ticker">Stock ticker symbol.</param>
    /// <param name="timeFrame">Timeframe to query.</param>
    /// <param name="start">Start date (inclusive).</param>
    /// <param name="end">End date (inclusive).</param>
    /// <returns>List of historical price records within the date range for the specified timeframe.</returns>
    Task<List<HistoricalPrice>> GetHistoricalDataAsync(string ticker, TimeFrame timeFrame, DateTime start, DateTime end);

    /// <summary>
    /// Gets a summary of available data for a ticker at a specific timeframe.
    /// </summary>
    /// <param name="ticker">Stock ticker symbol.</param>
    /// <param name="timeFrame">Timeframe to query.</param>
    /// <returns>Summary statistics for the ticker's historical data at the specified timeframe.</returns>
    Task<DataSummaryResult> GetDataSummaryAsync(string ticker, TimeFrame timeFrame);

    /// <summary>
    /// Gets all available timeframes for which data exists for a ticker.
    /// </summary>
    /// <param name="ticker">Stock ticker symbol.</param>
    /// <returns>List of timeframes with available data.</returns>
    Task<List<TimeFrame>> GetAvailableTimeFramesAsync(string ticker);
}

/// <summary>
/// Result object containing summary statistics for historical data.
/// </summary>
/// <param name="Ticker">Stock ticker symbol.</param>
/// <param name="ISIN">ISIN code for the security (if available).</param>
/// <param name="TotalRecords">Total number of historical records in storage.</param>
/// <param name="NewRecords">Number of new records added in the last save operation.</param>
/// <param name="OldestDate">Date of the oldest record.</param>
/// <param name="LatestDate">Date of the most recent record.</param>
/// <param name="MinPrice">Lowest price across all records.</param>
/// <param name="MaxPrice">Highest price across all records.</param>
/// <param name="LatestClose">Most recent closing price.</param>
public record DataSummaryResult(
    string Ticker,
    string? ISIN,
    int TotalRecords,
    int NewRecords,
    DateTime? OldestDate,
    DateTime? LatestDate,
    decimal? MinPrice,
    decimal? MaxPrice,
    decimal? LatestClose);
