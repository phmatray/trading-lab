// <copyright file="SymbolInfo.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Models.MarketData;

/// <summary>
/// Represents information about a tradable symbol.
/// </summary>
public sealed record SymbolInfo
{
    /// <summary>
    /// Gets the trading symbol (e.g., "AAPL", "SPY").
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Gets the full company or asset name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the exchange where the symbol is traded.
    /// </summary>
    public required string Exchange { get; init; }

    /// <summary>
    /// Gets the asset type (e.g., "Stock", "ETF", "Crypto", "Forex").
    /// </summary>
    public required string AssetType { get; init; }

    /// <summary>
    /// Gets the currency in which the symbol is denominated.
    /// </summary>
    public required string Currency { get; init; }

    /// <summary>
    /// Gets the minimum price increment (tick size).
    /// </summary>
    public decimal TickSize { get; init; }

    /// <summary>
    /// Gets the minimum quantity increment (lot size).
    /// </summary>
    public decimal LotSize { get; init; }

    /// <summary>
    /// Gets a value indicating whether the symbol is currently tradable.
    /// </summary>
    public bool IsTradable { get; init; }
}
