// <copyright file="MomentumConfig.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Strategies.Implementations;

/// <summary>
/// Configuration for the momentum strategy.
/// </summary>
public sealed class MomentumConfig
{
    /// <summary>
    /// Gets or sets the strategy name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the strategy is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the symbols to trade.
    /// </summary>
    public required List<string> Symbols { get; set; }

    /// <summary>
    /// Gets or sets the timeframe.
    /// </summary>
    public required string Timeframe { get; set; }

    /// <summary>
    /// Gets or sets the RSI period.
    /// </summary>
    public int RsiPeriod { get; set; } = 14;

    /// <summary>
    /// Gets or sets the RSI oversold threshold.
    /// </summary>
    public decimal RsiOversold { get; set; } = 30m;

    /// <summary>
    /// Gets or sets the RSI overbought threshold.
    /// </summary>
    public decimal RsiOverbought { get; set; } = 70m;

    /// <summary>
    /// Gets or sets the MACD fast period.
    /// </summary>
    public int MacdFast { get; set; } = 12;

    /// <summary>
    /// Gets or sets the MACD slow period.
    /// </summary>
    public int MacdSlow { get; set; } = 26;

    /// <summary>
    /// Gets or sets the MACD signal period.
    /// </summary>
    public int MacdSignal { get; set; } = 9;

    /// <summary>
    /// Gets or sets the SMA period.
    /// </summary>
    public int SmaPeriod { get; set; } = 50;
}
