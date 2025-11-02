// <copyright file="OrderSide.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Enums;

/// <summary>
/// Defines the side of the order.
/// </summary>
public enum OrderSide
{
    /// <summary>
    /// Buy order - opens long position or closes short position.
    /// </summary>
    Buy = 0,

    /// <summary>
    /// Sell order - closes long position or opens short position.
    /// </summary>
    Sell = 1,
}
