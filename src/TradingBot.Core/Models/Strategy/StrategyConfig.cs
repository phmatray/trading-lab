// <copyright file="StrategyConfig.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Models.Strategy;

/// <summary>
/// Represents configuration for a trading strategy.
/// </summary>
public sealed record StrategyConfig
{
    /// <summary>
    /// Gets the unique strategy name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the strategy type (e.g., "Momentum", "MeanReversion").
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets a value indicating whether the strategy is enabled.
    /// </summary>
    public required bool Enabled { get; init; }

    /// <summary>
    /// Gets the symbols this strategy trades.
    /// </summary>
    public required IReadOnlyList<string> Symbols { get; init; }

    /// <summary>
    /// Gets the timeframe the strategy operates on.
    /// </summary>
    public required string Timeframe { get; init; }

    /// <summary>
    /// Gets the strategy-specific parameters.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Parameters { get; init; }
}
