// <copyright file="IBacktestService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Web.Services;

using TradingBot.Core.Models.Backtest;
using TradingBot.Web.Models;

/// <summary>
/// Service for running backtests and retrieving backtest results.
/// </summary>
public interface IBacktestService
{
    /// <summary>
    /// Retrieves all saved backtest results, ordered by creation date descending.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of backtest results.</returns>
    Task<List<BacktestResult>> GetBacktestResultsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific backtest result by ID.
    /// </summary>
    /// <param name="backtestId">The unique identifier of the backtest.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The backtest result, or null if not found.</returns>
    Task<BacktestResult?> GetBacktestByIdAsync(string backtestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a backtest asynchronously and saves the results.
    /// </summary>
    /// <param name="request">The backtest configuration request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated backtest ID (format: "bt_{strategy}_{symbol}_{timestamp}").</returns>
    /// <remarks>
    /// This method queues the backtest execution as a background task and returns immediately.
    /// Progress and completion are communicated via SignalR events:
    /// - OnBacktestProgress(backtestId, progressPercent, statusMessage)
    /// - OnBacktestCompleted(backtestId, result)
    /// - OnBacktestFailed(backtestId, errorMessage)
    ///
    /// Backtest execution:
    /// 1. Validates request (strategy exists, symbol valid, date range valid, etc.)
    /// 2. Fetches historical market data for symbol and date range
    /// 3. Creates isolated portfolio manager and strategy instance
    /// 4. Replays historical data, generating signals and executing orders
    /// 5. Calculates performance metrics (total return, Sharpe ratio, max drawdown, win rate, profit factor)
    /// 6. Generates equity curve data points
    /// 7. Saves BacktestResult to database
    /// 8. Publishes OnBacktestCompleted event
    ///
    /// Typical execution time: 10-30 seconds for 1 year of daily data.
    /// </remarks>
    Task<string> RunBacktestAsync(BacktestRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a running backtest.
    /// </summary>
    /// <param name="backtestId">The ID of the backtest to cancel.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if backtest was cancelled successfully, false if backtest not found or already completed.</returns>
    /// <remarks>
    /// Sends cancellation signal to the background worker. Partial results are not saved.
    /// </remarks>
    Task<bool> CancelBacktestAsync(string backtestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a backtest result.
    /// </summary>
    /// <param name="backtestId">The ID of the backtest to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if backtest was deleted successfully, false if not found.</returns>
    Task<bool> DeleteBacktestAsync(string backtestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a backtest's trade list to CSV format.
    /// </summary>
    /// <param name="backtestId">The ID of the backtest whose trades to export.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>CSV-formatted string with trade data, or empty string if backtest not found.</returns>
    Task<string> ExportBacktestTradesToCsvAsync(string backtestId, CancellationToken cancellationToken = default);
}
