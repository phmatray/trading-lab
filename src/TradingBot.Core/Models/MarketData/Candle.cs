// <copyright file="Candle.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Models.MarketData;

/// <summary>
/// Represents an OHLCV candlestick for a given timeframe.
/// </summary>
public sealed record Candle
{
    /// <summary>
    /// Gets the trading symbol.
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Gets the candle timestamp in UTC.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the opening price.
    /// </summary>
    public required decimal Open { get; init; }

    /// <summary>
    /// Gets the highest price.
    /// </summary>
    public required decimal High { get; init; }

    /// <summary>
    /// Gets the lowest price.
    /// </summary>
    public required decimal Low { get; init; }

    /// <summary>
    /// Gets the closing price.
    /// </summary>
    public required decimal Close { get; init; }

    /// <summary>
    /// Gets the trading volume.
    /// </summary>
    public required long Volume { get; init; }

    /// <summary>
    /// Gets the timeframe (e.g., 1m, 5m, 1h, 1d).
    /// </summary>
    public required string Timeframe { get; init; }

    /// <summary>
    /// Gets a value indicating whether the candle is bullish (close >= open).
    /// </summary>
    public bool IsBullish => Close >= Open;

    /// <summary>
    /// Gets the candle body size (absolute difference between open and close).
    /// </summary>
    public decimal BodySize => Math.Abs(Close - Open);

    /// <summary>
    /// Gets the candle range (difference between high and low).
    /// </summary>
    public decimal Range => High - Low;

    /// <summary>
    /// Gets the typical price (high + low + close) / 3.
    /// </summary>
    public decimal TypicalPrice => (High + Low + Close) / 3m;
}
