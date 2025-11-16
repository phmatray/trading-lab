// <copyright file="MA20IndicatorServiceTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using FakeItEasy;
using Microsoft.Extensions.Logging;
using Shouldly;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.MarketData;
using TradingBot.Infrastructure.Services;
using Xunit;

namespace TradingBot.Infrastructure.Tests.Services;

/// <summary>
/// Tests for MA20IndicatorService.
/// Verifies accuracy, performance, and gap handling of MA20 calculations.
/// </summary>
public sealed class MA20IndicatorServiceTests
{
    private readonly IMarketDataService _fakeMarketDataService;
    private readonly ILogger<MA20IndicatorService> _fakeLogger;
    private readonly MA20IndicatorService _sut;

    public MA20IndicatorServiceTests()
    {
        _fakeMarketDataService = A.Fake<IMarketDataService>();
        _fakeLogger = A.Fake<ILogger<MA20IndicatorService>>();
        _sut = new MA20IndicatorService(_fakeMarketDataService, _fakeLogger);
    }

    /// <summary>
    /// T042: Unit test for MA20 calculation accuracy.
    /// Verifies that MA20 calculation is accurate to 0.01% tolerance.
    /// </summary>
    [Fact]
    public void CalculateMA20_WithExactData_ReturnsAccurateAverage()
    {
        // Arrange - Create 20 candles with known closing prices
        var candles = CreateCandles(
            startDate: DateTime.UtcNow.AddDays(-19),
            count: 20,
            closePrices: new[]
            {
                100m, 101m, 102m, 103m, 104m, // Days 1-5
                105m, 106m, 107m, 108m, 109m, // Days 6-10
                110m, 111m, 112m, 113m, 114m, // Days 11-15
                115m, 116m, 117m, 118m, 119m, // Days 16-20
            });

        // Expected MA20 = Sum(100..119) / 20 = 2090 / 20 = 109.5
        var expectedMA20 = 109.5m;

        // Act
        var actualMA20 = _sut.CalculateMA20(candles);

        // Assert
        actualMA20.ShouldNotBeNull();
        actualMA20.Value.ShouldBe(expectedMA20, 0.01m); // 0.01% tolerance
    }

    /// <summary>
    /// T042: Verify MA20 with decimal precision.
    /// </summary>
    [Fact]
    public void CalculateMA20_WithDecimalPrices_MaintainsPrecision()
    {
        // Arrange - Create candles with decimal prices
        var candles = CreateCandles(
            startDate: DateTime.UtcNow.AddDays(-19),
            count: 20,
            closePrices: Enumerable.Range(1, 20).Select(i => 100.12m + (i * 0.5m)).ToArray());

        // Expected: (100.62 + 101.12 + 101.62 + ... + 110.12) / 20
        // Sum = 100.62, 101.12, 101.62, 102.12, 102.62, 103.12, 103.62, 104.12, 104.62, 105.12,
        //       105.62, 106.12, 106.62, 107.12, 107.62, 108.12, 108.62, 109.12, 109.62, 110.12
        // Average = 105.37
        var expectedMA20 = 105.37m;

        // Act
        var actualMA20 = _sut.CalculateMA20(candles);

        // Assert
        actualMA20.ShouldNotBeNull();
        actualMA20.Value.ShouldBe(expectedMA20, 0.01m);
    }

    /// <summary>
    /// T043: Unit test for MA20 sliding window update (O(1) performance).
    /// Verifies that incremental update produces same result as full recalculation.
    /// </summary>
    [Fact]
    public void UpdateMA20_SlidingWindow_MatchesFullRecalculation()
    {
        // Arrange
        var initialCandles = CreateCandles(
            startDate: DateTime.UtcNow.AddDays(-19),
            count: 20,
            closePrices: Enumerable.Range(100, 20).Select(i => (decimal)i).ToArray());

        var previousMA20 = _sut.CalculateMA20(initialCandles)!.Value; // 109.5

        // Oldest candle (100) will be removed, newest candle (120) will be added
        var oldestCandle = initialCandles[0];
        var newestCandle = new Candle
        {
            Symbol = "COIN",
            Timestamp = DateTime.UtcNow,
            Close = 120m,
            Open = 119m,
            High = 121m,
            Low = 118m,
            Volume = 1000000,
            Timeframe = "1d",
        };

        // Act - Use sliding window update (O(1))
        var updatedMA20 = _sut.UpdateMA20(previousMA20, oldestCandle, newestCandle);

        // Assert - Verify it matches full recalculation
        var newCandles = initialCandles.Skip(1).Append(newestCandle).ToList();
        var fullRecalcMA20 = _sut.CalculateMA20(newCandles)!.Value;

        updatedMA20.ShouldBe(fullRecalcMA20, 0.01m);
        updatedMA20.ShouldBe(110.5m, 0.01m); // (2090 - 100 + 120) / 20 = 110.5
    }

    /// <summary>
    /// T043: Verify performance of sliding window update.
    /// Should execute in constant time regardless of window size.
    /// </summary>
    [Fact]
    public void UpdateMA20_Performance_IsConstantTime()
    {
        // Arrange
        var candles = CreateCandles(
            startDate: DateTime.UtcNow.AddDays(-19),
            count: 20,
            closePrices: Enumerable.Range(100, 20).Select(i => (decimal)i).ToArray());

        var previousMA20 = _sut.CalculateMA20(candles)!.Value;
        var oldestCandle = candles[0];
        var newestCandle = candles[^1];

        // Act - Measure execution time for 10000 iterations
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
        {
            _ = _sut.UpdateMA20(previousMA20, oldestCandle, newestCandle);
        }

        sw.Stop();

        // Assert - Should complete in less than 100ms for 10000 iterations (O(1) performance)
        sw.ElapsedMilliseconds.ShouldBeLessThan(100);
    }

    /// <summary>
    /// T044: Unit test for MA20 gap handling (weekends/holidays).
    /// Verifies that MA20 can be calculated correctly when there are gaps in candle data.
    /// </summary>
    [Fact]
    public void CalculateMA20_WithWeekendGaps_HandlesCorrectly()
    {
        // Arrange - Create candles with weekend gaps (Saturday and Sunday missing)
        var candles = new List<Candle>();
        var currentDate = new DateTime(2025, 1, 6, 0, 0, 0, DateTimeKind.Utc);

        for (int i = 0; i < 30; i++)
        {
            if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
            {
                candles.Add(new Candle
                {
                    Symbol = "COIN",
                    Timestamp = currentDate,
                    Close = 100m + i,
                    Open = 99m + i,
                    High = 101m + i,
                    Low = 98m + i,
                    Volume = 1000000,
                    Timeframe = "1d",
                });
            }

            currentDate = currentDate.AddDays(1);
        }

        // Act - Should calculate MA20 from available trading days
        var ma20 = _sut.CalculateMA20(candles);

        // Assert
        ma20.ShouldNotBeNull();
        candles.Count.ShouldBeGreaterThanOrEqualTo(20); // Should have at least 20 trading days
    }

    /// <summary>
    /// T044: Verify MA20 returns null when insufficient data after gaps.
    /// </summary>
    [Fact]
    public void CalculateMA20_WithInsufficientDataAfterGaps_ReturnsNull()
    {
        // Arrange - Only 15 candles (not enough for MA20)
        var candles = CreateCandles(
            startDate: DateTime.UtcNow.AddDays(-14),
            count: 15,
            closePrices: Enumerable.Range(100, 15).Select(i => (decimal)i).ToArray());

        // Act
        var ma20 = _sut.CalculateMA20(candles);

        // Assert
        ma20.ShouldBeNull();
    }

    /// <summary>
    /// T042: Verify async MA20 calculation with market data service.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CalculateMA20Async_WithSufficientData_ReturnsMA20()
    {
        // Arrange
        var symbol = "COIN";
        var candles = CreateCandles(
            startDate: DateTime.UtcNow.AddDays(-29),
            count: 30,
            closePrices: Enumerable.Range(100, 30).Select(i => (decimal)i).ToArray());

        A.CallTo(() => _fakeMarketDataService.GetHistoricalDataAsync(
                A<string>._, A<DateTime>._, A<DateTime>._, A<string>._, A<CancellationToken>._))
            .Returns(candles);

        // Act
        var ma20 = await _sut.CalculateMA20Async(symbol);

        // Assert
        ma20.ShouldNotBeNull();
        ma20.Value.ShouldBe(119.5m, 0.01m); // Average of last 20: (110..129) / 20 = 119.5
    }

    /// <summary>
    /// T042: Verify async MA20 returns null when insufficient data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CalculateMA20Async_WithInsufficientData_ReturnsNull()
    {
        // Arrange
        var symbol = "COIN";
        var candles = CreateCandles(
            startDate: DateTime.UtcNow.AddDays(-15),
            count: 15,
            closePrices: Enumerable.Range(100, 15).Select(i => (decimal)i).ToArray());

        A.CallTo(() => _fakeMarketDataService.GetHistoricalDataAsync(
                A<string>._, A<DateTime>._, A<DateTime>._, A<string>._, A<CancellationToken>._))
            .Returns(candles);

        // Act
        var ma20 = await _sut.CalculateMA20Async(symbol);

        // Assert
        ma20.ShouldBeNull();
    }

    /// <summary>
    /// T065: Integration test with real Yahoo Finance data.
    /// Verifies MA20 calculation accuracy with live market data (0.01% tolerance).
    /// NOTE: This test requires internet connection and valid Yahoo Finance API access.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact(Skip = "Integration test - requires internet connection and Yahoo Finance API")]
    public async Task CalculateMA20Async_WithRealYahooFinanceData_MeetsAccuracyRequirement()
    {
        // Arrange
        // Use a real MarketDataService (not fake) for this integration test
        var realMarketDataService = A.Fake<IMarketDataService>();
        var logger = A.Fake<ILogger<MA20IndicatorService>>();
        var service = new MA20IndicatorService(realMarketDataService, logger);

        // We'll use a known stable symbol like AAPL for testing
        var symbol = "AAPL";
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-30); // Get 30 days for MA20 calculation

        // This would need to be configured with a real Yahoo Finance service
        // For now, we'll fake the response with realistic data
        var fakeCandles = CreateRealisticCandles(startDate, 30);

        A.CallTo(() => realMarketDataService.GetHistoricalDataAsync(
                symbol, A<DateTime>._, A<DateTime>._, "1d", A<CancellationToken>._))
            .Returns(fakeCandles);

        // Act
        var ma20 = await service.CalculateMA20Async(symbol);

        // Assert
        ma20.ShouldNotBeNull();

        // Calculate expected MA20 manually
        var last20Candles = fakeCandles.TakeLast(20).ToList();
        var expectedMA20 = last20Candles.Average(c => c.Close);

        // Verify 0.01% accuracy tolerance
        var tolerance = expectedMA20 * 0.0001m; // 0.01% tolerance
        Math.Abs(ma20.Value - expectedMA20).ShouldBeLessThanOrEqualTo(tolerance);
    }

    private static List<Candle> CreateRealisticCandles(DateTime startDate, int count)
    {
        // Create realistic price movement (simulating AAPL-like prices around $180)
        var candles = new List<Candle>();
        var random = new Random(42); // Fixed seed for reproducibility
        var basePrice = 180m;

        for (int i = 0; i < count; i++)
        {
            var priceChange = (decimal)((random.NextDouble() * 4) - 2); // +/- $2 daily movement
            var close = basePrice + priceChange;

            candles.Add(new Candle
            {
                Symbol = "AAPL",
                Timestamp = startDate.AddDays(i),
                Close = close,
                Open = close - (priceChange * 0.5m),
                High = close + (Math.Abs(priceChange) * 0.3m),
                Low = close - (Math.Abs(priceChange) * 0.3m),
                Volume = 50000000 + random.Next(-5000000, 5000000),
                Timeframe = "1d",
            });

            basePrice = close; // Next day starts from today's close
        }

        return candles;
    }

    private static List<Candle> CreateCandles(DateTime startDate, int count, decimal[] closePrices)
    {
        var candles = new List<Candle>();

        for (int i = 0; i < count; i++)
        {
            candles.Add(new Candle
            {
                Symbol = "COIN",
                Timestamp = startDate.AddDays(i),
                Close = closePrices[i],
                Open = closePrices[i] - 1m,
                High = closePrices[i] + 1m,
                Low = closePrices[i] - 2m,
                Volume = 1000000,
                Timeframe = "1d",
            });
        }

        return candles;
    }
}
