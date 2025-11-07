// <copyright file="BacktestResult.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Portfolio;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Web.Models;

/// <summary>
/// Represents the results of a backtest simulation.
/// </summary>
public sealed class BacktestResult
{
    /// <summary>
    /// Gets or sets the unique backtest identifier.
    /// </summary>
    public Guid BacktestId { get; set; }

    /// <summary>
    /// Gets or sets the strategy name used for the backtest.
    /// </summary>
    public string StrategyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the trading symbol.
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the backtest start date.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the backtest end date.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Gets or sets the backtest duration.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the initial capital.
    /// </summary>
    public decimal InitialCapital { get; set; }

    /// <summary>
    /// Gets or sets the final equity.
    /// </summary>
    public decimal FinalEquity { get; set; }

    /// <summary>
    /// Gets or sets the total profit/loss.
    /// </summary>
    public decimal TotalPnL { get; set; }

    /// <summary>
    /// Gets or sets the total return percentage.
    /// </summary>
    public decimal TotalReturn { get; set; }

    /// <summary>
    /// Gets or sets the equity curve data points.
    /// </summary>
    public List<EquityDataPoint> EquityCurve { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of trades executed during the backtest.
    /// </summary>
    public List<Trade> Trades { get; set; } = new();

    /// <summary>
    /// Gets or sets the performance metrics.
    /// </summary>
    public PerformanceMetrics Metrics { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp when the backtest was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
