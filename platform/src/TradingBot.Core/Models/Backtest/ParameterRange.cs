// <copyright file="ParameterRange.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Models.Backtest;

/// <summary>
/// Represents a parameter range for optimization.
/// </summary>
public sealed class ParameterRange
{
    /// <summary>
    /// Gets or sets the minimum value.
    /// </summary>
    public required decimal Min { get; set; }

    /// <summary>
    /// Gets or sets the maximum value.
    /// </summary>
    public required decimal Max { get; set; }

    /// <summary>
    /// Gets or sets the step size.
    /// </summary>
    public required decimal Step { get; set; }
}
