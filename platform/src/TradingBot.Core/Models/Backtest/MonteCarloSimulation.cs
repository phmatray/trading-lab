// <copyright file="MonteCarloSimulation.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Models.Backtest;

/// <summary>
/// Individual Monte Carlo simulation.
/// </summary>
public sealed class MonteCarloSimulation
{
    /// <summary>
    /// Gets or sets the simulation number.
    /// </summary>
    public required int SimulationNumber { get; set; }

    /// <summary>
    /// Gets or sets the final equity.
    /// </summary>
    public required decimal FinalEquity { get; set; }

    /// <summary>
    /// Gets or sets the total return.
    /// </summary>
    public required decimal TotalReturn { get; set; }

    /// <summary>
    /// Gets or sets the maximum drawdown.
    /// </summary>
    public required decimal MaxDrawdown { get; set; }

    /// <summary>
    /// Gets or sets the Sharpe ratio.
    /// </summary>
    public required decimal SharpeRatio { get; set; }

    /// <summary>
    /// Gets or sets the profit factor.
    /// </summary>
    public required decimal ProfitFactor { get; set; }
}
