// <copyright file="IBacktestService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Backtest;
using TradingBot.Web.Models;

namespace TradingBot.Web.Services;

/// <summary>
/// Service for running backtests and managing backtest results.
/// </summary>
public interface IBacktestService
{
    /// <summary>
    /// Gets all backtest results ordered by creation date descending.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of backtest results with summary information.</returns>
    Task<List<BacktestResult>> GetBacktestResultsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed backtest result by ID.
    /// </summary>
    /// <param name="backtestId">The unique identifier of the backtest.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Detailed backtest result or null if not found.</returns>
    Task<BacktestResult?> GetBacktestByIdAsync(string backtestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a backtest asynchronously with the specified parameters.
    /// </summary>
    /// <param name="request">The backtest configuration request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated backtest ID.</returns>
    Task<string> RunBacktestAsync(BacktestRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a running backtest.
    /// </summary>
    /// <param name="backtestId">The ID of the backtest to cancel.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if backtest was cancelled successfully, false if not found.</returns>
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
