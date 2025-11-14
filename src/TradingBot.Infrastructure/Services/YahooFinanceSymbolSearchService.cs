// <copyright file="YahooFinanceSymbolSearchService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace TradingBot.Infrastructure.Services;

/// <summary>
/// Implements symbol search using Yahoo Finance Autocomplete API with caching.
/// </summary>
public class YahooFinanceSymbolSearchService : ISymbolSearchService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<YahooFinanceSymbolSearchService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="YahooFinanceSymbolSearchService"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client for API requests.</param>
    /// <param name="cache">Memory cache for search results.</param>
    /// <param name="logger">Logger instance.</param>
    public YahooFinanceSymbolSearchService(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<YahooFinanceSymbolSearchService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<List<SymbolSearchResult>> SearchSymbolsAsync(
        string query,
        int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 1)
        {
            return new List<SymbolSearchResult>();
        }

        var cacheKey = $"symbol_search_{query.ToUpperInvariant()}_{maxResults}";
        if (_cache.TryGetValue(cacheKey, out List<SymbolSearchResult>? cachedResults))
        {
            return cachedResults!;
        }

        try
        {
            var url = $"https://query2.finance.yahoo.com/v1/finance/search?q={Uri.EscapeDataString(query)}&quotesCount={maxResults}&newsCount=0";
            var response = await _httpClient.GetFromJsonAsync<YahooSearchResponse>(url, cancellationToken);

            var results = response?.Quotes?
                .Where(q => q.Symbol != null)
                .Select(q => new SymbolSearchResult
                {
                    Symbol = q.Symbol!,
                    Name = q.LongName ?? q.ShortName ?? q.Symbol!,
                    Exchange = q.ExchDisp ?? "Unknown",
                })
                .ToList() ?? new List<SymbolSearchResult>();

            // Cache results for 15 minutes
            _cache.Set(cacheKey, results, TimeSpan.FromMinutes(15));
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching symbols for query: {Query}", query);
            return new List<SymbolSearchResult>();
        }
    }

    /// <summary>
    /// Internal DTO for Yahoo Finance API response.
    /// </summary>
    private sealed class YahooSearchResponse
    {
        /// <summary>
        /// Gets or sets the list of quote results.
        /// </summary>
        [JsonPropertyName("quotes")]
        public List<YahooQuote>? Quotes { get; set; }
    }

    /// <summary>
    /// Internal DTO for a single Yahoo Finance quote result.
    /// </summary>
    private sealed class YahooQuote
    {
        /// <summary>
        /// Gets or sets the trading symbol.
        /// </summary>
        [JsonPropertyName("symbol")]
        public string? Symbol { get; set; }

        /// <summary>
        /// Gets or sets the short name.
        /// </summary>
        [JsonPropertyName("shortname")]
        public string? ShortName { get; set; }

        /// <summary>
        /// Gets or sets the long name.
        /// </summary>
        [JsonPropertyName("longname")]
        public string? LongName { get; set; }

        /// <summary>
        /// Gets or sets the exchange display name.
        /// </summary>
        [JsonPropertyName("exchDisp")]
        public string? ExchDisp { get; set; }
    }
}
