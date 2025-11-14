// <copyright file="IStrategyManagementService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Interfaces;

namespace TradingBot.Web.Services;

/// <summary>
/// Service for managing trading strategies.
/// </summary>
public interface IStrategyManagementService
{
    /// <summary>
    /// Gets all registered trading strategies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all strategies with their current status.</returns>
    Task<List<IStrategy>> GetAllStrategiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables a specific strategy by name.
    /// </summary>
    /// <param name="strategyName">The name of the strategy to enable.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if strategy was enabled successfully, false if strategy not found.</returns>
    Task<bool> EnableStrategyAsync(string strategyName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables a specific strategy by name.
    /// </summary>
    /// <param name="strategyName">The name of the strategy to disable.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if strategy was disabled successfully, false if strategy not found.</returns>
    Task<bool> DisableStrategyAsync(string strategyName, CancellationToken cancellationToken = default);
}
