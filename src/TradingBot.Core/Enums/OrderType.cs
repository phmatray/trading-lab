// <copyright file="OrderType.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Enums;

/// <summary>
/// Defines the type of order.
/// </summary>
public enum OrderType
{
    /// <summary>
    /// Market order - executes immediately at current market price.
    /// </summary>
    Market = 0,

    /// <summary>
    /// Limit order - executes only at specified price or better.
    /// </summary>
    Limit = 1,

    /// <summary>
    /// Stop-loss order - triggers when price reaches stop level.
    /// </summary>
    StopLoss = 2,

    /// <summary>
    /// Take-profit order - closes position at profit target.
    /// </summary>
    TakeProfit = 3,

    /// <summary>
    /// Trailing stop - dynamically adjusts stop level.
    /// </summary>
    TrailingStop = 4,
}
