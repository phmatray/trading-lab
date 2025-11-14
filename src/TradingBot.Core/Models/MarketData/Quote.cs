// <copyright file="Quote.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Models.MarketData;

/// <summary>
/// Represents a real-time market quote for a symbol.
/// </summary>
public sealed record Quote
{
    /// <summary>
    /// Gets the trading symbol (e.g., "AAPL", "SPY").
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Gets the timestamp of the quote in UTC.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the current price.
    /// </summary>
    public required decimal Price { get; init; }

    /// <summary>
    /// Gets the bid price.
    /// </summary>
    public required decimal Bid { get; init; }

    /// <summary>
    /// Gets the ask price.
    /// </summary>
    public required decimal Ask { get; init; }

    /// <summary>
    /// Gets the trading volume.
    /// </summary>
    public required long Volume { get; init; }

    /// <summary>
    /// Gets the absolute price change.
    /// </summary>
    public required decimal Change { get; init; }

    /// <summary>
    /// Gets the percentage price change.
    /// </summary>
    public required decimal ChangePercent { get; init; }

    /// <summary>
    /// Gets the bid-ask spread.
    /// </summary>
    public decimal Spread => Ask - Bid;

    /// <summary>
    /// Gets the midpoint price between bid and ask.
    /// </summary>
    public decimal MidPrice => (Bid + Ask) / 2m;
}
