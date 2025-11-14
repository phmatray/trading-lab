// <copyright file="IStrategyConfigurationRepository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Configuration;

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Repository interface for managing strategy configuration persistence.
/// </summary>
public interface IStrategyConfigurationRepository
{
    /// <summary>
    /// Retrieves the configuration for a specific strategy by name.
    /// </summary>
    /// <param name="strategyName">The name of the strategy.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The strategy configuration if found, otherwise null.</returns>
    Task<StrategyConfiguration?> GetByStrategyNameAsync(string strategyName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new strategy configuration or updates an existing one.
    /// </summary>
    /// <param name="configuration">The strategy configuration to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved strategy configuration.</returns>
    /// <remarks>
    /// This method performs an upsert operation based on strategy name.
    /// If a configuration already exists for the strategy name, it will be updated.
    /// Otherwise, a new configuration will be created.
    /// </remarks>
    Task<StrategyConfiguration> UpsertAsync(StrategyConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the configuration for a specific strategy by name.
    /// </summary>
    /// <param name="strategyName">The name of the strategy.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the configuration was deleted, false if not found.</returns>
    Task<bool> DeleteAsync(string strategyName, CancellationToken cancellationToken = default);
}
