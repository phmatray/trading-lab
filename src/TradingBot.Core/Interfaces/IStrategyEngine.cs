// <copyright file="IStrategyEngine.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Trading;

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Engine for managing and executing trading strategies.
/// </summary>
public interface IStrategyEngine
{
    /// <summary>
    /// Event raised when a strategy generates a trading signal.
    /// </summary>
    event EventHandler<Signal>? SignalGenerated;

    /// <summary>
    /// Gets a value indicating whether the engine is currently running.
    /// </summary>
    bool IsRunning { get; }

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

    /// <summary>
    /// Starts the strategy engine background execution loop.
    /// </summary>
    /// <param name="interval">Execution interval (how often to run strategies).</param>
    /// <param name="cancellationToken">Cancellation token to stop the engine.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartAsync(TimeSpan interval, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the strategy engine background execution loop.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopAsync();
}
