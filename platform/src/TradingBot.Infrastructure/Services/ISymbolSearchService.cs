// <copyright file="ISymbolSearchService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Infrastructure.Services;

/// <summary>
/// Defines a service for searching trading symbols.
/// </summary>
public interface ISymbolSearchService
{
    /// <summary>
    /// Searches for trading symbols matching the specified query.
    /// </summary>
    /// <param name="query">The search query (e.g., "AAPL", "Tesla").</param>
    /// <param name="maxResults">Maximum number of results to return (default: 10).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of matching symbol search results.</returns>
    Task<List<SymbolSearchResult>> SearchSymbolsAsync(
        string query,
        int maxResults = 10,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a single symbol search result.
/// </summary>
public class SymbolSearchResult
{
    /// <summary>
    /// Gets or sets the trading symbol (e.g., "AAPL").
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full company or security name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exchange where the symbol is traded (e.g., "NASDAQ").
    /// </summary>
    public string Exchange { get; set; } = string.Empty;
}
