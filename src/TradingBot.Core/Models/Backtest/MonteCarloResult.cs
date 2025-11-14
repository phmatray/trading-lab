// <copyright file="MonteCarloResult.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Models.Backtest;

/// <summary>
/// Monte Carlo simulation result.
/// </summary>
public sealed class MonteCarloResult
{
    /// <summary>
    /// Gets or sets the strategy name.
    /// </summary>
    public required string StrategyName { get; set; }

    /// <summary>
    /// Gets or sets the symbol.
    /// </summary>
    public required string Symbol { get; set; }

    /// <summary>
    /// Gets or sets the number of simulations run.
    /// </summary>
    public required int NumberOfSimulations { get; set; }

    /// <summary>
    /// Gets or sets the original backtest return.
    /// </summary>
    public required decimal OriginalReturn { get; set; }

    /// <summary>
    /// Gets or sets the original backtest drawdown.
    /// </summary>
    public required decimal OriginalDrawdown { get; set; }

    /// <summary>
    /// Gets or sets the individual simulations.
    /// </summary>
    public required List<MonteCarloSimulation> Simulations { get; set; }

    /// <summary>
    /// Gets or sets the aggregated statistics.
    /// </summary>
    public required MonteCarloStatistics Statistics { get; set; }
}
