// <copyright file="Theme.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Ardalis.SmartEnum;

namespace TradingBot.Core.ValueObjects;

/// <summary>
/// Represents the UI theme options.
/// </summary>
public sealed class Theme : SmartEnum<Theme>
{
    /// <summary>
    /// Light theme.
    /// </summary>
    public static readonly Theme Light = new(nameof(Light), 0);

    /// <summary>
    /// Dark theme.
    /// </summary>
    public static readonly Theme Dark = new(nameof(Dark), 1);

    private Theme(string name, int value)
        : base(name, value)
    {
    }
}
