// <copyright file="IBacktestService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Backtest;

namespace TradingBot.Web.Services;

/// <summary>
/// Service for retrieving backtest results.
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
}
