// <copyright file="StrategyStateDto.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.DTOs;

/// <summary>
/// Data transfer object for real-time strategy state updates via SignalR.
/// Contains current strategy metrics for dashboard display.
/// </summary>
public sealed class StrategyStateDto
{
    /// <summary>
    /// Gets or sets the strategy unique identifier.
    /// </summary>
    public required Guid StrategyId { get; set; }

    /// <summary>
    /// Gets or sets the ETP symbol being traded (e.g., "BTCW").
    /// </summary>
    public required string EtpSymbol { get; set; }

    /// <summary>
    /// Gets or sets the underlying asset symbol (e.g., "COIN").
    /// </summary>
    public required string UnderlyingSymbol { get; set; }

    /// <summary>
    /// Gets or sets the current price of the underlying asset.
    /// </summary>
    public decimal CurrentUnderlyingPrice { get; set; }

    /// <summary>
    /// Gets or sets the current price of the ETP.
    /// </summary>
    public decimal CurrentEtpPrice { get; set; }

    /// <summary>
    /// Gets or sets the current 20-day moving average value.
    /// </summary>
    public decimal? MA20Value { get; set; }

    /// <summary>
    /// Gets or sets the number of consecutive days the underlying price has been below MA20.
    /// </summary>
    public int DaysBelowMA20 { get; set; }

    /// <summary>
    /// Gets or sets the current cash ratio (cash / total equity).
    /// </summary>
    public decimal CurrentCashRatio { get; set; }

    /// <summary>
    /// Gets or sets the total portfolio equity (cash + position value).
    /// </summary>
    public decimal TotalEquity { get; set; }

    /// <summary>
    /// Gets or sets the number of ETP shares currently held.
    /// </summary>
    public decimal EtpSharesHeld { get; set; }

    /// <summary>
    /// Gets or sets the current position value in dollars.
    /// </summary>
    public decimal PositionValue { get; set; }

    /// <summary>
    /// Gets or sets the available cash balance.
    /// </summary>
    public decimal AvailableCash { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last weekly execution.
    /// </summary>
    public DateTime? LastExecutionTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the scheduled date/time for the next weekly execution.
    /// </summary>
    public DateTime NextExecutionTimestamp { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the strategy is currently enabled/active.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether breakout rule is enabled.
    /// </summary>
    public bool BreakoutRuleEnabled { get; set; }

    /// <summary>
    /// Gets or sets the current status message (e.g., "Running", "Waiting", "Error").
    /// </summary>
    public string Status { get; set; } = "Inactive";

    /// <summary>
    /// Gets or sets the timestamp when this state was captured.
    /// </summary>
    public DateTime StateTimestamp { get; set; } = DateTime.UtcNow;
}
