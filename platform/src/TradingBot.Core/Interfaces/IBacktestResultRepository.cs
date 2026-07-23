// <copyright file="IBacktestResultRepository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Backtest;

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Repository for managing backtest results persistence.
/// </summary>
public interface IBacktestResultRepository
{
    /// <summary>
    /// Retrieves all backtest results, ordered by creation date descending.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all backtest results.</returns>
    Task<List<BacktestResult>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific backtest result by its unique identifier.
    /// </summary>
    /// <param name="backtestId">The unique identifier of the backtest.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The backtest result, or null if not found.</returns>
    Task<BacktestResult?> GetByIdAsync(string backtestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a new backtest result to the database.
    /// </summary>
    /// <param name="result">The backtest result to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveAsync(BacktestResult result, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a backtest result from the database.
    /// </summary>
    /// <param name="backtestId">The unique identifier of the backtest to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the backtest was deleted, false if not found.</returns>
    Task<bool> DeleteAsync(string backtestId, CancellationToken cancellationToken = default);
}
