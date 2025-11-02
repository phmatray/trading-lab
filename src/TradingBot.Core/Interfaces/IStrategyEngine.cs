// <copyright file="IStrategyEngine.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Engine for managing and executing trading strategies.
/// </summary>
public interface IStrategyEngine
{
    /// <summary>
    /// Gets all registered strategies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all strategies.</returns>
    Task<IReadOnlyList<IStrategy>> GetStrategiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a strategy by name.
    /// </summary>
    /// <param name="name">Strategy name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Strategy or null if not found.</returns>
    Task<IStrategy?> GetStrategyAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a strategy with the engine.
    /// </summary>
    /// <param name="strategy">Strategy to register.</param>
    void RegisterStrategy(IStrategy strategy);

    /// <summary>
    /// Enables a strategy by name.
    /// </summary>
    /// <param name="name">Strategy name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if strategy was enabled, false if not found.</returns>
    Task<bool> EnableStrategyAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables a strategy by name.
    /// </summary>
    /// <param name="name">Strategy name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if strategy was disabled, false if not found.</returns>
    Task<bool> DisableStrategyAsync(string name, CancellationToken cancellationToken = default);
}
