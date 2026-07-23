// <copyright file="IBacktestingEngine.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Backtest;

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Service for running strategy backtests.
/// </summary>
public interface IBacktestingEngine
{
    /// <summary>
    /// Runs a backtest with the specified configuration.
    /// </summary>
    /// <param name="configuration">Backtest configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Backtest results.</returns>
    Task<BacktestResult> RunBacktestAsync(
        BacktestConfiguration configuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a backtest result by ID.
    /// </summary>
    /// <param name="backtestId">Backtest identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Backtest result if found, null otherwise.</returns>
    Task<BacktestResult?> GetBacktestResultAsync(
        string backtestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all backtest results.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all backtest results.</returns>
    Task<IReadOnlyList<BacktestResult>> GetAllBacktestResultsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent backtest result.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Most recent backtest result if any exist, null otherwise.</returns>
    Task<BacktestResult?> GetLatestBacktestResultAsync(
        CancellationToken cancellationToken = default);
}
