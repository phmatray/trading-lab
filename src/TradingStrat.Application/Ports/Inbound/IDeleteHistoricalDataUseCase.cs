using TradingStrat.Domain.Common;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Use case for deleting historical data.
/// Supports deletion by ticker (all timeframes or specific timeframe) or by date range.
/// </summary>
public interface IDeleteHistoricalDataUseCase
{
    /// <summary>
    /// Deletes all historical data for a ticker, optionally limited to a specific timeframe.
    /// </summary>
    /// <param name="ticker">Stock ticker symbol.</param>
    /// <param name="timeFrame">Optional timeframe to delete. If null, deletes all timeframes.</param>
    /// <returns>Result containing the number of records deleted.</returns>
    Task<Result<DeleteDataResult>> DeleteTickerAsync(string ticker, TimeFrame? timeFrame = null);

    /// <summary>
    /// Deletes historical data for a ticker within a specific date range and timeframe.
    /// </summary>
    /// <param name="ticker">Stock ticker symbol.</param>
    /// <param name="timeFrame">Timeframe to delete from.</param>
    /// <param name="startDate">Start date (inclusive).</param>
    /// <param name="endDate">End date (inclusive).</param>
    /// <returns>Result containing the number of records deleted.</returns>
    Task<Result<DeleteDataResult>> DeleteDateRangeAsync(
        string ticker,
        TimeFrame timeFrame,
        DateTime startDate,
        DateTime endDate);
}

/// <summary>
/// Result of a delete operation.
/// </summary>
/// <param name="RecordsDeleted">Number of records deleted.</param>
/// <param name="Message">Human-readable message describing the operation result.</param>
public record DeleteDataResult(
    int RecordsDeleted,
    string Message);
