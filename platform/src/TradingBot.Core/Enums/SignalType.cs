// <copyright file="SignalType.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Ardalis.SmartEnum;

namespace TradingBot.Core.Enums;

/// <summary>
/// Defines the type of trading signal.
/// </summary>
public sealed class SignalType : SmartEnum<SignalType>
{
    /// <summary>
    /// Buy signal - open long position or close short position.
    /// </summary>
    public static readonly SignalType Buy = new(nameof(Buy), 0);

    /// <summary>
    /// Sell signal - open short position or close long position.
    /// </summary>
    public static readonly SignalType Sell = new(nameof(Sell), 1);

    /// <summary>
    /// Hold signal - maintain current positions, no action required.
    /// </summary>
    public static readonly SignalType Hold = new(nameof(Hold), 2);

    /// <summary>
    /// Close signal - close all positions for this symbol.
    /// </summary>
    public static readonly SignalType Close = new(nameof(Close), 3);

    private SignalType(string name, int value)
        : base(name, value)
    {
    }
}
