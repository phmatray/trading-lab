// <copyright file="IStrategyManagementService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Interfaces;
using TradingBot.Web.Models;

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

    /// <summary>
    /// Retrieves the configurable parameters for a strategy.
    /// </summary>
    /// <param name="strategyName">The name of the strategy.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of parameter descriptors with current values, or empty list if strategy not found.</returns>
    Task<List<StrategyParameterDto>> GetStrategyParametersAsync(string strategyName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the configuration parameters for a strategy.
    /// </summary>
    /// <param name="strategyName">The name of the strategy to configure.</param>
    /// <param name="parameters">Dictionary of parameter names to new values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if configuration was saved successfully, false if strategy not found or validation failed.</returns>
    Task<bool> ConfigureStrategyAsync(string strategyName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets a strategy's configuration to default values.
    /// </summary>
    /// <param name="strategyName">The name of the strategy to reset.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if configuration was reset successfully, false if strategy not found.</returns>
    Task<bool> ResetStrategyToDefaultsAsync(string strategyName, CancellationToken cancellationToken = default);
}
