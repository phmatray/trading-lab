// <copyright file="RiskSettings.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Models.Risk;

/// <summary>
/// Trading risk management settings.
/// </summary>
public sealed class RiskSettings
{
    /// <summary>
    /// Gets or sets the account leverage multiplier.
    /// </summary>
    public decimal Leverage { get; set; } = 1.0m;

    /// <summary>
    /// Gets or sets the default stop-loss percentage.
    /// </summary>
    public decimal StopLossPercent { get; set; } = 2.0m;

    /// <summary>
    /// Gets or sets the default take-profit percentage.
    /// </summary>
    public decimal TakeProfitPercent { get; set; } = 5.0m;

    /// <summary>
    /// Gets or sets the maximum daily loss limit.
    /// </summary>
    public decimal DailyLossLimit { get; set; } = 1000m;

    /// <summary>
    /// Gets or sets the maximum drawdown percentage limit.
    /// </summary>
    public decimal MaxDrawdownPercent { get; set; } = 10.0m;

    /// <summary>
    /// Gets or sets the maximum position size as percentage of equity.
    /// </summary>
    public decimal MaxPositionSizePercent { get; set; } = 10.0m;

    /// <summary>
    /// Gets or sets a value indicating whether risk limits are enabled.
    /// </summary>
    public bool RiskLimitsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the timestamp when settings were last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
