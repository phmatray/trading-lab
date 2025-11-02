// <copyright file="SignalType.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Enums;

/// <summary>
/// Defines the type of trading signal.
/// </summary>
public enum SignalType
{
    /// <summary>
    /// Buy signal - open long position or close short position.
    /// </summary>
    Buy = 0,

    /// <summary>
    /// Sell signal - open short position or close long position.
    /// </summary>
    Sell = 1,

    /// <summary>
    /// Hold signal - maintain current positions, no action required.
    /// </summary>
    Hold = 2,

    /// <summary>
    /// Close signal - close all positions for this symbol.
    /// </summary>
    Close = 3,
}
