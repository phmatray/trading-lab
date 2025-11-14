// <copyright file="BacktestResult.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Portfolio;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Core.Models.Backtest;

/// <summary>
/// Results from a backtest run.
/// </summary>
public sealed class BacktestResult
{
    /// <summary>
    /// Gets or sets the backtest identifier.
    /// </summary>
    public required string BacktestId { get; set; }

    /// <summary>
    /// Gets or sets the strategy name.
    /// </summary>
    public required string StrategyName { get; set; }

    /// <summary>
    /// Gets or sets the symbol.
    /// </summary>
    public required string Symbol { get; set; }

    /// <summary>
    /// Gets or sets the start date.
    /// </summary>
    public required DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date.
    /// </summary>
    public required DateTime EndDate { get; set; }

    /// <summary>
    /// Gets or sets the initial capital.
    /// </summary>
    public required decimal InitialCapital { get; set; }

    /// <summary>
    /// Gets or sets the final equity.
    /// </summary>
    public required decimal FinalEquity { get; set; }

    /// <summary>
    /// Gets the total return percentage.
    /// </summary>
    public decimal TotalReturn => InitialCapital > 0
        ? ((FinalEquity - InitialCapital) / InitialCapital) * 100m
        : 0m;

    /// <summary>
    /// Gets the total profit/loss.
    /// </summary>
    public decimal TotalPnL => FinalEquity - InitialCapital;

    /// <summary>
    /// Gets or sets the list of all trades executed.
    /// </summary>
    public required List<Trade> Trades { get; set; }

    /// <summary>
    /// Gets or sets the equity curve (date, equity pairs).
    /// </summary>
    public required List<(DateTime Date, decimal Equity)> EquityCurve { get; set; }

    /// <summary>
    /// Gets or sets the performance metrics.
    /// </summary>
    public required PerformanceMetrics Performance { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the backtest was run.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the duration of the backtest execution.
    /// </summary>
    public TimeSpan Duration { get; set; }
}
