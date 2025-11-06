// <copyright file="OrderType.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Common;

namespace TradingBot.Core.Enums;

/// <summary>
/// Defines the type of order.
/// </summary>
public sealed class OrderType : SmartEnum<OrderType, int>
{
    /// <summary>
    /// Market order - executes immediately at current market price.
    /// </summary>
    public static readonly OrderType Market = new(nameof(Market), 0);

    /// <summary>
    /// Limit order - executes only at specified price or better.
    /// </summary>
    public static readonly OrderType Limit = new(nameof(Limit), 1);

    /// <summary>
    /// Stop-loss order - triggers when price reaches stop level.
    /// </summary>
    public static readonly OrderType StopLoss = new(nameof(StopLoss), 2);

    /// <summary>
    /// Take-profit order - closes position at profit target.
    /// </summary>
    public static readonly OrderType TakeProfit = new(nameof(TakeProfit), 3);

    /// <summary>
    /// Trailing stop - dynamically adjusts stop level.
    /// </summary>
    public static readonly OrderType TrailingStop = new(nameof(TrailingStop), 4);

    private OrderType(string name, int value)
        : base(name, value)
    {
    }
}
