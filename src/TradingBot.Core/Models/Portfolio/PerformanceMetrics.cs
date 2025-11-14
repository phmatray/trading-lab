// <copyright file="PerformanceMetrics.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Models.Portfolio;

/// <summary>
/// Represents performance metrics for a trading strategy or portfolio.
/// </summary>
public sealed record PerformanceMetrics
{
    /// <summary>
    /// Gets the total return percentage.
    /// </summary>
    public required decimal TotalReturn { get; init; }

    /// <summary>
    /// Gets the annualized return percentage.
    /// </summary>
    public required decimal AnnualizedReturn { get; init; }

    /// <summary>
    /// Gets the Sharpe ratio.
    /// </summary>
    public required decimal SharpeRatio { get; init; }

    /// <summary>
    /// Gets the Sortino ratio.
    /// </summary>
    public required decimal SortinoRatio { get; init; }

    /// <summary>
    /// Gets the Calmar ratio.
    /// </summary>
    public required decimal CalmarRatio { get; init; }

    /// <summary>
    /// Gets the maximum drawdown percentage.
    /// </summary>
    public required decimal MaxDrawdown { get; init; }

    /// <summary>
    /// Gets the total number of trades.
    /// </summary>
    public required int TotalTrades { get; init; }

    /// <summary>
    /// Gets the number of winning trades.
    /// </summary>
    public required int WinningTrades { get; init; }

    /// <summary>
    /// Gets the number of losing trades.
    /// </summary>
    public required int LosingTrades { get; init; }

    /// <summary>
    /// Gets the win rate percentage.
    /// </summary>
    public decimal WinRate => TotalTrades > 0 ? (decimal)WinningTrades / TotalTrades * 100m : 0m;

    /// <summary>
    /// Gets the average winning trade amount.
    /// </summary>
    public decimal AverageWin { get; init; }

    /// <summary>
    /// Gets the average losing trade amount.
    /// </summary>
    public decimal AverageLoss { get; init; }

    /// <summary>
    /// Gets the profit factor (total wins / total losses).
    /// </summary>
    public decimal ProfitFactor { get; init; }
}
