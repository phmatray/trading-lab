// <copyright file="IStrategy.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.MarketData;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Defines a trading strategy that generates signals from market data.
/// </summary>
public interface IStrategy
{
    /// <summary>
    /// Gets the unique strategy name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the strategy type (e.g., "Momentum", "MeanReversion").
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Gets the symbols this strategy trades.
    /// </summary>
    IReadOnlyList<string> Symbols { get; }

    /// <summary>
    /// Gets the timeframe the strategy operates on.
    /// </summary>
    string Timeframe { get; }

    /// <summary>
    /// Gets a value indicating whether the strategy is currently enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Initializes the strategy with configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a trading signal for the given symbol and data.
    /// </summary>
    /// <param name="symbol">The trading symbol.</param>
    /// <param name="data">Historical candle data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Trading signal or null if no signal.</returns>
    Task<Signal?> GenerateSignalAsync(
        string symbol,
        IReadOnlyList<Candle> data,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates strategy parameters.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if parameters are valid.</returns>
    Task<bool> ValidateParametersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables the strategy.
    /// </summary>
    void Enable();

    /// <summary>
    /// Disables the strategy.
    /// </summary>
    void Disable();
}
