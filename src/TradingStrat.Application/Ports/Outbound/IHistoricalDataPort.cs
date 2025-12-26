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

    /// <summary>
    /// Gets all unique tickers that have historical data in the database.
    /// </summary>
    /// <returns>List of unique ticker symbols.</returns>
    Task<List<string>> GetAllTickersAsync();

    /// <summary>
    /// Gets summary information for multiple tickers at a specific timeframe in a single operation.
    /// More efficient than calling GetDataSummaryAsync multiple times.
    /// </summary>
    /// <param name="tickers">List of ticker symbols to get summaries for.</param>
    /// <param name="timeFrame">Timeframe to query.</param>
    /// <returns>Dictionary mapping ticker symbols to their data summaries.</returns>
    Task<Dictionary<string, DataSummaryResult>> GetDataSummariesAsync(
        IEnumerable<string> tickers,
        TimeFrame timeFrame);

    /// <summary>
    /// Saves historical price data for multiple tickers in a single bulk operation.
    /// More efficient than calling SaveHistoricalDataAsync multiple times.
    /// </summary>
    /// <param name="tickerDataMap">Dictionary mapping ticker symbols to their ISIN and historical data.</param>
    /// <param name="timeFrame">Timeframe of the data being saved.</param>
    /// <param name="progress">Optional progress reporting for bulk save operation.</param>
    Task BulkSaveHistoricalDataAsync(
        Dictionary<string, (string? isin, IEnumerable<HistoricalPrice> data)> tickerDataMap,
        TimeFrame timeFrame,
        IProgress<BulkSaveProgress>? progress = null);

    /// <summary>
    /// Deletes all historical data for a ticker, optionally limited to a specific timeframe.
    /// </summary>
    /// <param name="ticker">Stock ticker symbol.</param>
    /// <param name="timeFrame">Optional timeframe to delete. If null, deletes all timeframes.</param>
    /// <returns>Number of records deleted.</returns>
    Task<int> DeleteTickerDataAsync(string ticker, TimeFrame? timeFrame = null);

    /// <summary>
    /// Deletes historical data for a ticker within a specific date range and timeframe.
    /// </summary>
    /// <param name="ticker">Stock ticker symbol.</param>
    /// <param name="timeFrame">Timeframe to delete from.</param>
    /// <param name="startDate">Start date (inclusive).</param>
    /// <param name="endDate">End date (inclusive).</param>
    /// <returns>Number of records deleted.</returns>
    Task<int> DeleteDateRangeAsync(
        string ticker,
        TimeFrame timeFrame,
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Gets a lightweight summary of all tickers for a specific timeframe.
    /// Optimized for status page display with minimal data transfer.
    /// </summary>
    /// <param name="timeFrame">Timeframe to query.</param>
    /// <returns>List of ticker summaries with record counts and date ranges.</returns>
    Task<List<TickerSummary>> GetAllTickerSummariesAsync(TimeFrame timeFrame);

    /// <summary>
    /// Gets the last modification timestamp of the database.
    /// Used for cache invalidation to detect when data has changed.
    /// </summary>
    /// <returns>DateTime of last database modification, or null if no data exists.</returns>
    Task<DateTime?> GetDatabaseLastModifiedAsync();
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

/// <summary>
/// Progress reporting object for bulk save operations.
/// </summary>
/// <param name="TotalTickers">Total number of tickers being saved.</param>
/// <param name="CompletedTickers">Number of tickers completed so far.</param>
/// <param name="CurrentTicker">Ticker currently being saved.</param>
/// <param name="TotalRecordsSaved">Total number of records saved across all tickers.</param>
public record BulkSaveProgress(
    int TotalTickers,
    int CompletedTickers,
    string CurrentTicker,
    int TotalRecordsSaved);

/// <summary>
/// Lightweight summary of a ticker's data for status display.
/// </summary>
/// <param name="Ticker">Stock ticker symbol.</param>
/// <param name="ISIN">ISIN code for the security (if available).</param>
/// <param name="RecordCount">Number of historical records.</param>
/// <param name="OldestDate">Date of the oldest record.</param>
/// <param name="LatestDate">Date of the most recent record.</param>
public record TickerSummary(
    string Ticker,
    string? ISIN,
    int RecordCount,
    DateTime? OldestDate,
    DateTime? LatestDate);
