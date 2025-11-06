// <copyright file="WalkForwardResult.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Models.Backtest;

/// <summary>
/// Result of walk-forward optimization.
/// </summary>
public sealed class WalkForwardResult
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
    /// Gets or sets the start date.
    /// </summary>
    public required DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date.
    /// </summary>
    public required DateTime EndDate { get; set; }

    /// <summary>
    /// Gets or sets the number of windows.
    /// </summary>
    public required int NumberOfWindows { get; set; }

    /// <summary>
    /// Gets or sets the window results.
    /// </summary>
    public required List<WalkForwardWindowResult> WindowResults { get; set; }

    /// <summary>
    /// Gets or sets the average return across all windows.
    /// </summary>
    public required decimal AverageReturn { get; set; }

    /// <summary>
    /// Gets or sets the median return across all windows.
    /// </summary>
    public required decimal MedianReturn { get; set; }

    /// <summary>
    /// Gets or sets the number of winning windows.
    /// </summary>
    public required int WinningWindows { get; set; }

    /// <summary>
    /// Gets or sets the total number of windows.
    /// </summary>
    public required int TotalWindows { get; set; }
}
