// <copyright file="BacktestResult.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Portfolio;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Core.Models.Backtest;

/// <summary>
/// Represents the results of a completed backtest execution.
/// </summary>
public class BacktestResult
{
    /// <summary>
    /// Gets or sets the unique identifier for this backtest.
    /// Format: "bt_{strategy}_{symbol}_{timestamp}" for readability.
    /// </summary>
    public string BacktestId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the strategy that was backtested.
    /// </summary>
    public string StrategyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the symbol that was backtested.
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the start date of the backtest period.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date of the backtest period.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Gets or sets the initial capital at the start of the backtest.
    /// </summary>
    public decimal InitialCapital { get; set; }

    /// <summary>
    /// Gets or sets the final equity at the end of the backtest.
    /// </summary>
    public decimal FinalEquity { get; set; }

    /// <summary>
    /// Gets the total return as a percentage.
    /// Calculated as: ((FinalEquity - InitialCapital) / InitialCapital) * 100.
    /// </summary>
    public decimal TotalReturn => InitialCapital > 0
        ? ((FinalEquity - InitialCapital) / InitialCapital) * 100m
        : 0m;

    /// <summary>
    /// Gets the total profit and loss.
    /// </summary>
    public decimal TotalPnL => FinalEquity - InitialCapital;

    /// <summary>
    /// Gets or sets the Sharpe ratio (risk-adjusted return).
    /// Higher is better (>1.0 is good, >2.0 is excellent).
    /// </summary>
    public decimal SharpeRatio { get; set; }

    /// <summary>
    /// Gets or sets the maximum drawdown as a percentage.
    /// Represents the largest peak-to-trough decline.
    /// </summary>
    public decimal MaxDrawdown { get; set; }

    /// <summary>
    /// Gets or sets the win rate as a percentage (0-100).
    /// Calculated as: (winning trades / total trades) * 100.
    /// </summary>
    public decimal WinRate { get; set; }

    /// <summary>
    /// Gets or sets the profit factor.
    /// Calculated as: gross profits / gross losses.
    /// Values >1.0 indicate profitability.
    /// </summary>
    public decimal ProfitFactor { get; set; }

    /// <summary>
    /// Gets or sets the total number of trades executed.
    /// </summary>
    public int TotalTrades { get; set; }

    /// <summary>
    /// Gets or sets the list of all trades executed (for in-memory use).
    /// </summary>
    public List<Trade> Trades { get; set; } = new();

    /// <summary>
    /// Gets or sets the equity curve data points (for in-memory use).
    /// </summary>
    public List<(DateTime Date, decimal Equity)> EquityCurve { get; set; } = new();

    /// <summary>
    /// Gets or sets the performance metrics (for in-memory use).
    /// </summary>
    public PerformanceMetrics Performance { get; set; } = null!;

    /// <summary>
    /// Gets or sets the JSON-serialized list of all trades (for database persistence).
    /// Each trade includes: symbol, side, entry/exit prices, profit and loss, timestamps.
    /// </summary>
    public string TradesJson { get; set; } = "[]";

    /// <summary>
    /// Gets or sets the JSON-serialized equity curve data points (for database persistence).
    /// Format: [{"Date": "2024-01-01", "Equity": 100000}, ...].
    /// </summary>
    public string EquityCurveJson { get; set; } = "[]";

    /// <summary>
    /// Gets or sets the timestamp when this backtest was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the duration of the backtest execution.
    /// </summary>
    public TimeSpan Duration { get; set; }
}
