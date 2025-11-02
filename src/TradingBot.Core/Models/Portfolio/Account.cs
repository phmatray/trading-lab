// <copyright file="Account.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Models.Portfolio;

/// <summary>
/// Represents a trading account.
/// </summary>
public sealed class Account
{
    /// <summary>
    /// Gets or sets the account identifier.
    /// </summary>
    public required string AccountId { get; set; }

    /// <summary>
    /// Gets or sets the total equity (cash + position values).
    /// </summary>
    public required decimal Equity { get; set; }

    /// <summary>
    /// Gets or sets the available cash balance.
    /// </summary>
    public required decimal Cash { get; set; }

    /// <summary>
    /// Gets or sets the total value of open positions.
    /// </summary>
    public decimal PositionValue { get; set; }

    /// <summary>
    /// Gets or sets the buying power (cash * leverage).
    /// </summary>
    public decimal BuyingPower { get; set; }

    /// <summary>
    /// Gets or sets the current leverage being used.
    /// </summary>
    public decimal Leverage { get; set; }

    /// <summary>
    /// Gets or sets the total unrealized profit/loss from open positions.
    /// </summary>
    public decimal UnrealizedPnL { get; set; }

    /// <summary>
    /// Gets or sets the total realized profit/loss from closed trades.
    /// </summary>
    public decimal RealizedPnL { get; set; }

    /// <summary>
    /// Gets the total profit/loss (realized + unrealized).
    /// </summary>
    public decimal TotalPnL => RealizedPnL + UnrealizedPnL;
}
