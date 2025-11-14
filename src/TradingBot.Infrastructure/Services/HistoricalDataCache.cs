// Copyright (c) 2025 TradingBot. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.MarketData;
using TradingBot.Infrastructure.Persistence;

namespace TradingBot.Infrastructure.Services;

/// <summary>
/// Implements caching for historical market data to reduce API calls.
/// </summary>
public class HistoricalDataCache : IHistoricalDataCache
{
    /// <summary>
    /// Historical data cached for 1 year.
    /// </summary>
    private static readonly TimeSpan HistoricalDataExpiration = TimeSpan.FromDays(365);

    /// <summary>
    /// Recent data cached for 1 hour.
    /// </summary>
    private static readonly TimeSpan RecentDataExpiration = TimeSpan.FromHours(1);

    /// <summary>
    /// Intraday data cached for 1 minute.
    /// </summary>
    private static readonly TimeSpan IntradayDataExpiration = TimeSpan.FromMinutes(1);

    private readonly TradingBotDbContext _context;
    private readonly ILogger<HistoricalDataCache> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HistoricalDataCache"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    public HistoricalDataCache(
        TradingBotDbContext context,
        ILogger<HistoricalDataCache> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Candle>?> GetAsync(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        string timeframe,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("Symbol cannot be null or empty.", nameof(symbol));
        }

        if (string.IsNullOrWhiteSpace(timeframe))
        {
            throw new ArgumentException("Timeframe cannot be null or empty.", nameof(timeframe));
        }

        if (startDate >= endDate)
        {
            throw new ArgumentException("Start date must be before end date.");
        }

        try
        {
            _logger.LogDebug(
                "Checking cache for {Symbol} from {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd} ({Timeframe})",
                symbol,
                startDate,
                endDate,
                timeframe);

            // Query cached candles
            var cachedCandles = await _context.Candles
                .Where(c => c.Symbol == symbol
                    && c.Timeframe == timeframe
                    && c.Timestamp >= startDate
                    && c.Timestamp <= endDate)
                .OrderBy(c => c.Timestamp)
                .ToListAsync(cancellationToken);

            if (!cachedCandles.Any())
            {
                _logger.LogDebug("Cache miss: No data found for {Symbol}", symbol);
                return null;
            }

            // Check if we have complete data for the requested range
            var expectedCandles = CalculateExpectedCandleCount(startDate, endDate, timeframe);
            var actualCandles = cachedCandles.Count;

            // Allow 5% gap tolerance
            if (actualCandles < expectedCandles * 0.95)
            {
                _logger.LogDebug(
                    "Cache incomplete: {Actual} candles found, expected ~{Expected} for {Symbol}",
                    actualCandles,
                    expectedCandles,
                    symbol);
                return null;
            }

            // Check if cache is expired
            var latestCandle = cachedCandles.Last();
            if (IsCacheExpired(latestCandle.Timestamp, timeframe))
            {
                _logger.LogDebug(
                    "Cache expired: Latest candle at {Timestamp} for {Symbol}",
                    latestCandle.Timestamp,
                    symbol);
                return null;
            }

            _logger.LogInformation(
                "Cache hit: {Count} candles retrieved for {Symbol} ({Timeframe})",
                cachedCandles.Count,
                symbol,
                timeframe);

            return cachedCandles.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving cached data for {Symbol}",
                symbol);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task SetAsync(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        string timeframe,
        IReadOnlyList<Candle> candles,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("Symbol cannot be null or empty.", nameof(symbol));
        }

        if (string.IsNullOrWhiteSpace(timeframe))
        {
            throw new ArgumentException("Timeframe cannot be null or empty.", nameof(timeframe));
        }

        if (candles == null || !candles.Any())
        {
            throw new ArgumentException("Candles cannot be null or empty.", nameof(candles));
        }

        try
        {
            _logger.LogDebug(
                "Caching {Count} candles for {Symbol} ({Timeframe})",
                candles.Count,
                symbol,
                timeframe);

            // Remove existing candles in this range to avoid duplicates
            var existingCandles = await _context.Candles
                .Where(c => c.Symbol == symbol
                    && c.Timeframe == timeframe
                    && c.Timestamp >= startDate
                    && c.Timestamp <= endDate)
                .ToListAsync(cancellationToken);

            if (existingCandles.Any())
            {
                _context.Candles.RemoveRange(existingCandles);
                _logger.LogDebug(
                    "Removed {Count} existing candles from cache",
                    existingCandles.Count);
            }

            // Add new candles
            await _context.Candles.AddRangeAsync(candles, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully cached {Count} candles for {Symbol} ({Timeframe})",
                candles.Count,
                symbol,
                timeframe);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error caching data for {Symbol}",
                symbol);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task InvalidateAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("Symbol cannot be null or empty.", nameof(symbol));
        }

        try
        {
            _logger.LogDebug("Invalidating cache for {Symbol}", symbol);

            var candlesToRemove = await _context.Candles
                .Where(c => c.Symbol == symbol)
                .ToListAsync(cancellationToken);

            if (candlesToRemove.Any())
            {
                _context.Candles.RemoveRange(candlesToRemove);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Invalidated cache: Removed {Count} candles for {Symbol}",
                    candlesToRemove.Count,
                    symbol);
            }
            else
            {
                _logger.LogDebug("No cached data found to invalidate for {Symbol}", symbol);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error invalidating cache for {Symbol}",
                symbol);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Clearing all cached data");

            var allCandles = await _context.Candles
                .ToListAsync(cancellationToken);

            if (allCandles.Any())
            {
                _context.Candles.RemoveRange(allCandles);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Cleared all cached data: Removed {Count} candles",
                    allCandles.Count);
            }
            else
            {
                _logger.LogDebug("No cached data found to clear");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
            throw;
        }
    }

    /// <summary>
    /// Calculates approximate expected candle count for a date range.
    /// </summary>
    private static int CalculateExpectedCandleCount(
        DateTime startDate,
        DateTime endDate,
        string timeframe)
    {
        var duration = endDate - startDate;
        var days = duration.TotalDays;

        return timeframe.ToUpperInvariant() switch
        {
            "1M" or "1MIN" => (int)(days * 24 * 60), // 1 minute candles
            "5M" or "5MIN" => (int)(days * 24 * 12), // 5 minute candles
            "15M" or "15MIN" => (int)(days * 24 * 4), // 15 minute candles
            "1H" or "1HOUR" => (int)(days * 24), // 1 hour candles
            "4H" or "4HOUR" => (int)(days * 6), // 4 hour candles
            "1D" or "1DAY" or "DAILY" => (int)days, // 1 day candles
            "1W" or "1WEEK" or "WEEKLY" => (int)(days / 7), // 1 week candles
            "1MO" or "1MONTH" or "MONTHLY" => (int)(days / 30), // 1 month candles
            _ => (int)days, // Default to daily
        };
    }

    /// <summary>
    /// Determines if cached data has expired based on timeframe and age.
    /// </summary>
    private static bool IsCacheExpired(DateTime latestTimestamp, string timeframe)
    {
        var age = DateTime.UtcNow - latestTimestamp;

        // Historical data (> 1 day old): Cache indefinitely
        if (age > TimeSpan.FromDays(1))
        {
            return age > HistoricalDataExpiration;
        }

        // Recent data (< 1 day old): Cache for 1 hour
        if (age > TimeSpan.FromMinutes(60))
        {
            return age > RecentDataExpiration;
        }

        // Intraday data: Cache for 1 minute
        return age > IntradayDataExpiration;
    }
}
