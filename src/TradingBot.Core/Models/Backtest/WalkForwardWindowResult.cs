// <copyright file="WalkForwardWindowResult.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Models.Backtest;

/// <summary>
/// Result for a single walk-forward window.
/// </summary>
public sealed class WalkForwardWindowResult
{
    /// <summary>
    /// Gets or sets the window number.
    /// </summary>
    public required int WindowNumber { get; set; }

    /// <summary>
    /// Gets or sets the training start date.
    /// </summary>
    public required DateTime TrainingStart { get; set; }

    /// <summary>
    /// Gets or sets the training end date.
    /// </summary>
    public required DateTime TrainingEnd { get; set; }

    /// <summary>
    /// Gets or sets the testing start date.
    /// </summary>
    public required DateTime TestingStart { get; set; }

    /// <summary>
    /// Gets or sets the testing end date.
    /// </summary>
    public required DateTime TestingEnd { get; set; }

    /// <summary>
    /// Gets or sets the best parameters found during optimization.
    /// </summary>
    public required Dictionary<string, decimal> BestParameters { get; set; }

    /// <summary>
    /// Gets or sets the testing period result using the best parameters.
    /// </summary>
    public required BacktestResult TestingResult { get; set; }
}
