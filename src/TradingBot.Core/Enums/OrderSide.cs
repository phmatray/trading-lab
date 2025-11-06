// <copyright file="OrderSide.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Common;

namespace TradingBot.Core.Enums;

/// <summary>
/// Defines the side of the order.
/// </summary>
public sealed class OrderSide : SmartEnum<OrderSide, int>
{
    /// <summary>
    /// Buy order - opens long position or closes short position.
    /// </summary>
    public static readonly OrderSide Buy = new(nameof(Buy), 0);

    /// <summary>
    /// Sell order - closes long position or opens short position.
    /// </summary>
    public static readonly OrderSide Sell = new(nameof(Sell), 1);

    private OrderSide(string name, int value)
        : base(name, value)
    {
    }
}
