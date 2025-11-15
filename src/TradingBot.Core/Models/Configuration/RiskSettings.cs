// <copyright file="RiskSettings.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Ardalis.SharedKernel;

namespace TradingBot.Core.Models.Configuration;

/// <summary>
/// Represents the risk management settings for the trading account.
/// This is a singleton entity (only one row per database).
/// Consolidated from Models/Risk/RiskSettings.cs and Models/Configuration/RiskSettings.cs.
/// </summary>
public class RiskSettings : EntityBase<Guid>, IAggregateRoot
{
    /// <summary>
    /// Gets or sets the account leverage multiplier.
    /// Example: 1.0 means no leverage, 2.0 means 2x leverage.
    /// </summary>
    public decimal Leverage { get; set; } = 1.0m;

    /// <summary>
    /// Gets or sets the maximum position size as a percentage of account equity.
    /// Example: 10.0 means max 10% of equity per position.
    /// </summary>
    public decimal MaxPositionSizePercent { get; set; } = 10m;

    /// <summary>
    /// Gets or sets the default stop-loss percentage below entry price.
    /// Example: 2.0 means stop-loss at 2% below entry.
    /// </summary>
    public decimal StopLossPercent { get; set; } = 2m;

    /// <summary>
    /// Gets or sets the default take-profit percentage above entry price.
    /// Example: 5.0 means take-profit at 5% above entry.
    /// </summary>
    public decimal TakeProfitPercent { get; set; } = 5m;

    /// <summary>
    /// Gets or sets the maximum number of concurrent open positions.
    /// </summary>
    public int MaxOpenPositions { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum daily loss limit as a percentage of account equity.
    /// Example: 5.0 means trading halts if daily loss exceeds 5% of equity.
    /// </summary>
    public decimal MaxDailyLossPercent { get; set; } = 5m;

    /// <summary>
    /// Gets or sets the maximum daily loss limit (absolute value).
    /// Example: 1000 means trading halts if daily loss exceeds $1000.
    /// </summary>
    public decimal DailyLossLimit { get; set; } = 1000m;

    /// <summary>
    /// Gets or sets the maximum drawdown percentage limit.
    /// Example: 10.0 means trading halts if drawdown exceeds 10% from peak.
    /// </summary>
    public decimal MaxDrawdownPercent { get; set; } = 10.0m;

    /// <summary>
    /// Gets or sets a value indicating whether risk limits are enabled.
    /// </summary>
    public bool RiskLimitsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the timestamp when these settings were last modified.
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when these settings were created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
