// <copyright file="StrategyStateDto.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Web.Models;

/// <summary>
/// Data transfer object for real-time strategy state updates via SignalR.
/// </summary>
public sealed class StrategyStateDto
{
    /// <summary>
    /// Gets or sets the strategy identifier.
    /// </summary>
    public required Guid StrategyId { get; set; }

    /// <summary>
    /// Gets or sets the strategy name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the strategy is enabled.
    /// </summary>
    public required bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the ETP symbol.
    /// </summary>
    public required string EtpSymbol { get; set; }

    /// <summary>
    /// Gets or sets the underlying asset symbol.
    /// </summary>
    public required string UnderlyingSymbol { get; set; }

    /// <summary>
    /// Gets or sets the current underlying price.
    /// </summary>
    public decimal? CurrentUnderlyingPrice { get; set; }

    /// <summary>
    /// Gets or sets the current ETP price.
    /// </summary>
    public decimal? CurrentEtpPrice { get; set; }

    /// <summary>
    /// Gets or sets the current MA20 value.
    /// </summary>
    public decimal? CurrentMA20 { get; set; }

    /// <summary>
    /// Gets or sets the consecutive days below MA20.
    /// </summary>
    public int DaysBelowMA20 { get; set; }

    /// <summary>
    /// Gets or sets the current cash ratio (0-1).
    /// </summary>
    public decimal? CurrentCashRatio { get; set; }

    /// <summary>
    /// Gets or sets the minimum cash ratio threshold.
    /// </summary>
    public required decimal MinCashRatio { get; set; }

    /// <summary>
    /// Gets or sets the maximum cash ratio threshold.
    /// </summary>
    public required decimal MaxCashRatio { get; set; }

    /// <summary>
    /// Gets or sets the last execution timestamp (UTC).
    /// </summary>
    public DateTime? LastExecutionTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the last daily update timestamp (UTC).
    /// </summary>
    public DateTime? LastDailyUpdateTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the next scheduled execution date (UTC).
    /// </summary>
    public DateTime? NextScheduledExecution { get; set; }

    /// <summary>
    /// Gets or sets the execution day of week (0=Sunday, 6=Saturday).
    /// </summary>
    public required int ExecutionDayOfWeek { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the strategy is currently above MA20.
    /// </summary>
    public bool IsBullish { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the sell condition is met (2+ days below MA20).
    /// </summary>
    public bool IsSellConditionMet { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the buy condition is met.
    /// </summary>
    public bool IsBuyConditionMet { get; set; }
}
