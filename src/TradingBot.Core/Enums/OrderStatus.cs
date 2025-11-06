// <copyright file="OrderStatus.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Common;

namespace TradingBot.Core.Enums;

/// <summary>
/// Defines the status of an order.
/// </summary>
public sealed class OrderStatus : SmartEnum<OrderStatus, int>
{
    /// <summary>
    /// Order created but not yet submitted.
    /// </summary>
    public static readonly OrderStatus Pending = new(nameof(Pending), 0);

    /// <summary>
    /// Order submitted to broker/exchange.
    /// </summary>
    public static readonly OrderStatus Submitted = new(nameof(Submitted), 1);

    /// <summary>
    /// Order partially filled.
    /// </summary>
    public static readonly OrderStatus PartiallyFilled = new(nameof(PartiallyFilled), 2);

    /// <summary>
    /// Order fully filled.
    /// </summary>
    public static readonly OrderStatus Filled = new(nameof(Filled), 3);

    /// <summary>
    /// Order cancelled by user.
    /// </summary>
    public static readonly OrderStatus Cancelled = new(nameof(Cancelled), 4);

    /// <summary>
    /// Order rejected by broker/exchange.
    /// </summary>
    public static readonly OrderStatus Rejected = new(nameof(Rejected), 5);

    /// <summary>
    /// Order expired before execution.
    /// </summary>
    public static readonly OrderStatus Expired = new(nameof(Expired), 6);

    private OrderStatus(string name, int value)
        : base(name, value)
    {
    }
}
