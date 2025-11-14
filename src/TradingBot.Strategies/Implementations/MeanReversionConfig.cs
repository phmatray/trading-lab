// <copyright file="MeanReversionConfig.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Strategies.Implementations;

/// <summary>
/// Configuration for the mean reversion strategy.
/// </summary>
public sealed class MeanReversionConfig
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
    /// Gets or sets the lookback period for Bollinger Bands.
    /// </summary>
    public int LookbackPeriod { get; set; } = 20;

    /// <summary>
    /// Gets or sets the standard deviation multiplier.
    /// </summary>
    public double StdMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Gets or sets a value indicating whether to exit when price returns to mean.
    /// </summary>
    public bool ExitAtMean { get; set; } = true;
}
