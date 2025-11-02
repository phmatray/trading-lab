// <copyright file="OrderStatus.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Enums;

/// <summary>
/// Defines the status of an order.
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Order created but not yet submitted.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Order submitted to broker/exchange.
    /// </summary>
    Submitted = 1,

    /// <summary>
    /// Order partially filled.
    /// </summary>
    PartiallyFilled = 2,

    /// <summary>
    /// Order fully filled.
    /// </summary>
    Filled = 3,

    /// <summary>
    /// Order cancelled by user.
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Order rejected by broker/exchange.
    /// </summary>
    Rejected = 5,

    /// <summary>
    /// Order expired before execution.
    /// </summary>
    Expired = 6,
}
