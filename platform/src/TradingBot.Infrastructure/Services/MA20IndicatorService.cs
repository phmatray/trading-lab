// <copyright file="MA20IndicatorService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.MarketData;

namespace TradingBot.Infrastructure.Services;

/// <summary>
/// Service for calculating the 20-day moving average (MA20) indicator using a sliding window algorithm.
/// </summary>
public sealed class MA20IndicatorService : IMA20IndicatorService
{
    private const int MA20Period = 20;
    private readonly IMarketDataService _marketDataService;
    private readonly ILogger<MA20IndicatorService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MA20IndicatorService"/> class.
    /// </summary>
    /// <param name="marketDataService">The market data service.</param>
    /// <param name="logger">The logger.</param>
    public MA20IndicatorService(
        IMarketDataService marketDataService,
        ILogger<MA20IndicatorService> logger)
    {
        _marketDataService = marketDataService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<decimal?> CalculateMA20Async(string symbol, CancellationToken cancellationToken = default)
    {
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-30); // Fetch 30 days to ensure we have at least 20

        var candles = await _marketDataService.GetHistoricalDataAsync(
            symbol,
            startDate,
            endDate,
            "1d", // Daily candles for MA20 calculation
            cancellationToken);

        if (candles == null || candles.Count < MA20Period)
        {
            _logger.LogWarning(
                "Insufficient candle data for MA20 calculation. Symbol: {Symbol}, Required: {Required}, Available: {Available}",
                symbol,
                MA20Period,
                candles?.Count ?? 0);

            return null;
        }

        // Take the last 20 candles and calculate MA20
        var last20Candles = candles
            .OrderBy(c => c.Timestamp)
            .TakeLast(MA20Period)
            .ToList();

        return CalculateMA20(last20Candles);
    }

    /// <inheritdoc/>
    public decimal? CalculateMA20(IReadOnlyList<Candle> candles)
    {
        if (candles == null || candles.Count < MA20Period)
        {
            return null;
        }

        // Calculate simple moving average of closing prices
        var sum = candles.TakeLast(MA20Period).Sum(c => c.Close);
        return sum / MA20Period;
    }

    /// <inheritdoc/>
    public decimal UpdateMA20(decimal previousMA20, Candle oldestCandle, Candle newestCandle)
    {
        // Sliding window optimization: O(1) time complexity
        // New MA20 = Previous MA20 - (Oldest Close / 20) + (Newest Close / 20)
        // This is equivalent to: (Sum - Oldest + Newest) / 20
        var adjustment = (newestCandle.Close - oldestCandle.Close) / MA20Period;
        return previousMA20 + adjustment;
    }
}
