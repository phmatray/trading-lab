// <copyright file="HistoricalDataCacheTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using TradingBot.Core.Models.MarketData;
using TradingBot.Infrastructure.Persistence;
using TradingBot.Infrastructure.Services;

namespace TradingBot.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for HistoricalDataCache.
/// </summary>
public class HistoricalDataCacheTests : IDisposable
{
    private readonly TradingBotDbContext _context;
    private readonly ILogger<HistoricalDataCache> _logger;
    private readonly HistoricalDataCache _cache;

    public HistoricalDataCacheTests()
    {
        var options = new DbContextOptionsBuilder<TradingBotDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TradingBotDbContext(options);
        _logger = new Mock<ILogger<HistoricalDataCache>>().Object;
        _cache = new HistoricalDataCache(_context, _logger);
    }

    [Fact]
    public async Task GetAsync_WhenCacheEmpty_ShouldReturnNull()
    {
        // Act
        var result = await _cache.GetAsync(
            "SPY",
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow,
            "1D");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task SetAsync_ShouldCacheCandles()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-10);
        var endDate = DateTime.UtcNow;
        var candles = CreateSampleCandles("SPY", startDate, endDate, "1D", 10);

        // Act
        await _cache.SetAsync("SPY", startDate, endDate, "1D", candles);

        // Assert
        var cachedData = await _context.Candles
            .Where(c => c.Symbol == "SPY")
            .ToListAsync();

        cachedData.ShouldNotBeEmpty();
        cachedData.Count.ShouldBe(10);
    }

    [Fact]
    public async Task GetAsync_AfterSetAsync_ShouldReturnCachedCandles()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-10);
        var endDate = DateTime.UtcNow;
        var candles = CreateSampleCandles("SPY", startDate, endDate, "1D", 10);
        await _cache.SetAsync("SPY", startDate, endDate, "1D", candles);

        // Act
        var result = await _cache.GetAsync("SPY", startDate, endDate, "1D");

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(10);
        result.First().Symbol.ShouldBe("SPY");
    }

    [Fact]
    public async Task SetAsync_WithEmptyCandles_ShouldThrowArgumentException()
    {
        // Arrange
        var emptyCandles = new List<Candle>().AsReadOnly();

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _cache.SetAsync(
                "SPY",
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow,
                "1D",
                emptyCandles));
    }

    [Fact]
    public async Task SetAsync_WithNullSymbol_ShouldThrowArgumentException()
    {
        // Arrange
        var candles = CreateSampleCandles("SPY", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, "1D", 1);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _cache.SetAsync(
                null!,
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow,
                "1D",
                candles));
    }

    [Fact]
    public async Task SetAsync_WithNullTimeframe_ShouldThrowArgumentException()
    {
        // Arrange
        var candles = CreateSampleCandles("SPY", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, "1D", 1);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _cache.SetAsync(
                "SPY",
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow,
                null!,
                candles));
    }

    [Fact]
    public async Task GetAsync_WithInvalidDateRange_ShouldThrowArgumentException()
    {
        // Arrange
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(1); // Start after end

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _cache.GetAsync("SPY", startDate, endDate, "1D"));
    }

    [Fact]
    public async Task SetAsync_ShouldReplaceExistingCandles()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-10);
        var endDate = DateTime.UtcNow;
        var candles1 = CreateSampleCandles("SPY", startDate, endDate, "1D", 10);
        var candles2 = CreateSampleCandles("SPY", startDate, endDate, "1D", 8);

        await _cache.SetAsync("SPY", startDate, endDate, "1D", candles1);

        // Act
        await _cache.SetAsync("SPY", startDate, endDate, "1D", candles2);

        // Assert
        var cachedData = await _context.Candles
            .Where(c => c.Symbol == "SPY")
            .ToListAsync();

        cachedData.Count.ShouldBe(8); // Should have new count, not old + new
    }

    [Fact]
    public async Task InvalidateAsync_ShouldRemoveAllCandlesForSymbol()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-10);
        var endDate = DateTime.UtcNow;
        var spyCandles = CreateSampleCandles("SPY", startDate, endDate, "1D", 10);
        var aaplCandles = CreateSampleCandles("AAPL", startDate, endDate, "1D", 10);

        await _cache.SetAsync("SPY", startDate, endDate, "1D", spyCandles);
        await _cache.SetAsync("AAPL", startDate, endDate, "1D", aaplCandles);

        // Act
        await _cache.InvalidateAsync("SPY");

        // Assert
        var spyData = await _context.Candles
            .Where(c => c.Symbol == "SPY")
            .ToListAsync();
        var aaplData = await _context.Candles
            .Where(c => c.Symbol == "AAPL")
            .ToListAsync();

        spyData.ShouldBeEmpty();
        aaplData.ShouldNotBeEmpty();
        aaplData.Count.ShouldBe(10);
    }

    [Fact]
    public async Task InvalidateAsync_WithNullSymbol_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _cache.InvalidateAsync(null!));
    }

    [Fact]
    public async Task ClearAsync_ShouldRemoveAllCachedData()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-10);
        var endDate = DateTime.UtcNow;
        var spyCandles = CreateSampleCandles("SPY", startDate, endDate, "1D", 10);
        var aaplCandles = CreateSampleCandles("AAPL", startDate, endDate, "1D", 10);

        await _cache.SetAsync("SPY", startDate, endDate, "1D", spyCandles);
        await _cache.SetAsync("AAPL", startDate, endDate, "1D", aaplCandles);

        // Act
        await _cache.ClearAsync();

        // Assert
        var allData = await _context.Candles.ToListAsync();
        allData.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAsync_WithIncompleteData_ShouldReturnNull()
    {
        // Arrange - Create only 5 candles for a 30-day range (should expect ~30)
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;
        var incompleteCandles = CreateSampleCandles("SPY", startDate, endDate, "1D", 5);

        await _cache.SetAsync("SPY", startDate, endDate, "1D", incompleteCandles);

        // Act
        var result = await _cache.GetAsync("SPY", startDate, endDate, "1D");

        // Assert
        result.ShouldBeNull(); // Incomplete data should not be returned
    }

    [Fact]
    public async Task GetAsync_WithCompleteData_ShouldReturnCandles()
    {
        // Arrange - Create complete data (allow 5% tolerance)
        var startDate = DateTime.UtcNow.AddDays(-10);
        var endDate = DateTime.UtcNow;
        var completeCandles = CreateSampleCandles("SPY", startDate, endDate, "1D", 10);

        await _cache.SetAsync("SPY", startDate, endDate, "1D", completeCandles);

        // Act
        var result = await _cache.GetAsync("SPY", startDate, endDate, "1D");

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(10);
    }

    [Fact]
    public async Task GetAsync_WithDifferentTimeframes_ShouldReturnCorrectData()
    {
        // Arrange - Use historical data (> 1 day old) to avoid cache expiration
        var startDate = DateTime.UtcNow.AddDays(-10);
        var endDate = DateTime.UtcNow.AddDays(-5);  // End 5 days ago (historical)
        var dailyCandles = CreateSampleCandles("SPY", startDate, endDate, "1D", 5);
        var hourlyCandles = CreateSampleCandles("SPY", startDate, endDate, "1H", 120); // 5 days * 24 hours = 120

        await _cache.SetAsync("SPY", startDate, endDate, "1D", dailyCandles);
        await _cache.SetAsync("SPY", startDate, endDate, "1H", hourlyCandles);

        // Act
        var dailyResult = await _cache.GetAsync("SPY", startDate, endDate, "1D");
        var hourlyResult = await _cache.GetAsync("SPY", startDate, endDate, "1H");

        // Assert
        dailyResult.ShouldNotBeNull();
        dailyResult.Count.ShouldBe(5);
        hourlyResult.ShouldNotBeNull();
        hourlyResult.Count.ShouldBe(120);
    }

    [Fact]
    public async Task InvalidateAsync_WhenNoCachedData_ShouldNotThrow()
    {
        // Act & Assert
        await Should.NotThrowAsync(async () =>
            await _cache.InvalidateAsync("NONEXISTENT"));
    }

    [Fact]
    public async Task ClearAsync_WhenNoCachedData_ShouldNotThrow()
    {
        // Act & Assert
        await Should.NotThrowAsync(async () =>
            await _cache.ClearAsync());
    }

    [Fact]
    public async Task GetAsync_WithMultipleSymbols_ShouldReturnCorrectSymbolData()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-10);
        var endDate = DateTime.UtcNow;
        var spyCandles = CreateSampleCandles("SPY", startDate, endDate, "1D", 10);
        var aaplCandles = CreateSampleCandles("AAPL", startDate, endDate, "1D", 10);

        await _cache.SetAsync("SPY", startDate, endDate, "1D", spyCandles);
        await _cache.SetAsync("AAPL", startDate, endDate, "1D", aaplCandles);

        // Act
        var spyResult = await _cache.GetAsync("SPY", startDate, endDate, "1D");
        var aaplResult = await _cache.GetAsync("AAPL", startDate, endDate, "1D");

        // Assert
        spyResult.ShouldNotBeNull();
        spyResult.ShouldAllBe(c => c.Symbol == "SPY");

        aaplResult.ShouldNotBeNull();
        aaplResult.ShouldAllBe(c => c.Symbol == "AAPL");
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    private static IReadOnlyList<Candle> CreateSampleCandles(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        string timeframe,
        int count)
    {
        var candles = new List<Candle>();
        var interval = (endDate - startDate).TotalHours / count;

        for (int i = 0; i < count; i++)
        {
            candles.Add(new Candle
            {
                Symbol = symbol,
                Timestamp = startDate.AddHours(interval * i),
                Open = 100m + i,
                High = 101m + i,
                Low = 99m + i,
                Close = 100.5m + i,
                Volume = 1000000 + (i * 10000),
                Timeframe = timeframe,
            });
        }

        return candles.AsReadOnly();
    }
}
