// <copyright file="IStrategyManagementService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Web.Services;

using TradingBot.Core.Interfaces;
using TradingBot.Web.Models;

/// <summary>
/// Service for managing trading strategies including enable/disable and configuration.
/// </summary>
public interface IStrategyManagementService
{
    /// <summary>
    /// Retrieves all registered trading strategies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all strategies with current enabled status and parameters.</returns>
    Task<List<IStrategy>> GetAllStrategiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables a strategy by name.
    /// </summary>
    /// <param name="strategyName">The name of the strategy to enable.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if strategy was enabled successfully, false if strategy not found.</returns>
    /// <remarks>
    /// Enabling a strategy allows it to generate signals and execute trades.
    /// </remarks>
    Task<bool> EnableStrategyAsync(string strategyName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables a strategy by name.
    /// </summary>
    /// <param name="strategyName">The name of the strategy to disable.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if strategy was disabled successfully, false if strategy not found.</returns>
    /// <remarks>
    /// Disabling a strategy stops it from generating new signals, but does not close existing positions.
    /// </remarks>
    Task<bool> DisableStrategyAsync(string strategyName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the configurable parameters for a strategy.
    /// </summary>
    /// <param name="strategyName">The name of the strategy.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of parameter descriptors with current values, or empty list if strategy not found.</returns>
    /// <remarks>
    /// Parameters include display name, description, type, current value, min/max bounds.
    /// Example: FastPeriod (int, current=12, min=1, max=50, description="Fast moving average period")
    /// </remarks>
    Task<List<StrategyParameterDto>> GetStrategyParametersAsync(string strategyName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the configuration parameters for a strategy.
    /// </summary>
    /// <param name="strategyName">The name of the strategy to configure.</param>
    /// <param name="parameters">Dictionary of parameter names to new values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if configuration was saved successfully, false if strategy not found or validation failed.</returns>
    /// <remarks>
    /// This method:
    /// 1. Validates parameter names and types against strategy schema
    /// 2. Validates values against min/max bounds
    /// 3. Saves configuration to StrategyConfiguration table (upsert by strategy name)
    /// 4. Applies new parameters to the in-memory strategy instance
    /// 5. Publishes SignalR event OnStrategyConfigurationChanged
    /// If strategy is currently enabled, the new parameters take effect immediately for future signals.
    /// </remarks>
    Task<bool> ConfigureStrategyAsync(string strategyName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets a strategy's configuration to default values.
    /// </summary>
    /// <param name="strategyName">The name of the strategy to reset.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if configuration was reset successfully, false if strategy not found.</returns>
    /// <remarks>
    /// Deletes the StrategyConfiguration record and reloads strategy with default parameter values.
    /// </remarks>
    Task<bool> ResetStrategyToDefaultsAsync(string strategyName, CancellationToken cancellationToken = default);
}
