// <copyright file="RiskParameters.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Models.Risk;

/// <summary>
/// Represents risk management parameters.
/// </summary>
public sealed record RiskParameters
{
    /// <summary>
    /// Gets the maximum leverage allowed.
    /// </summary>
    public required decimal MaxLeverage { get; init; }

    /// <summary>
    /// Gets the maximum position size as percentage of portfolio.
    /// </summary>
    public required decimal MaxPositionSizePercent { get; init; }

    /// <summary>
    /// Gets the default stop-loss percentage.
    /// </summary>
    public required decimal DefaultStopLossPercent { get; init; }

    /// <summary>
    /// Gets the default take-profit percentage.
    /// </summary>
    public required decimal DefaultTakeProfitPercent { get; init; }

    /// <summary>
    /// Gets the maximum daily loss percentage.
    /// </summary>
    public required decimal MaxDailyLossPercent { get; init; }

    /// <summary>
    /// Gets the maximum drawdown percentage before auto-shutdown.
    /// </summary>
    public required decimal MaxDrawdownPercent { get; init; }
}
