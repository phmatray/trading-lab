// <copyright file="IndicatorLibraryTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.MarketData;
using TradingBot.Strategies.Indicators;

namespace TradingBot.Strategies.Tests.Indicators;

/// <summary>
/// Unit tests for the IndicatorLibrary.
/// </summary>
public sealed class IndicatorLibraryTests
{
    #region Test Data Helpers

    private static List<Candle> CreateTestCandles(int count, decimal startPrice = 100m)
    {
        var candles = new List<Candle>();
        var baseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        for (int i = 0; i < count; i++)
        {
            var price = startPrice + i;
            candles.Add(new Candle
            {
                Symbol = "TEST",
                Timestamp = baseDate.AddDays(i),
                Open = price - 0.5m,
                High = price + 1m,
                Low = price - 1m,
                Close = price,
                Volume = 1000000,
                Timeframe = "1d",
            });
        }

        return candles;
    }

    private static List<Candle> CreateVolatileCandles()
    {
        var prices = new[] { 100m, 102m, 98m, 103m, 99m, 105m, 97m, 106m, 96m, 108m, 95m, 110m, 94m, 112m, 93m };
        var candles = new List<Candle>();
        var baseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        for (int i = 0; i < prices.Length; i++)
        {
            candles.Add(new Candle
            {
                Symbol = "VOLATILE",
                Timestamp = baseDate.AddDays(i),
                Open = prices[i] - 1m,
                High = prices[i] + 2m,
                Low = prices[i] - 2m,
                Close = prices[i],
                Volume = 1000000,
                Timeframe = "1d",
            });
        }

        return candles;
    }

    #endregion

    #region SMA Tests

    [Fact]
    public void CalculateSMA_WithValidData_ShouldReturnCorrectAverage()
    {
        // Arrange
        var candles = CreateTestCandles(10); // Prices: 100, 101, 102, 103, 104, 105, 106, 107, 108, 109
        var period = 5;

        // Act
        var sma = IndicatorLibrary.CalculateSMA(candles, period);

        // Assert - SMA of last 5 prices: (105+106+107+108+109)/5 = 107
        sma.ShouldBe(107m);
    }

    [Fact]
    public void CalculateSMA_WithNullCandles_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => IndicatorLibrary.CalculateSMA(null!, 5))
            .Message.ShouldContain("Candles cannot be null or empty");
    }

    [Fact]
    public void CalculateSMA_WithEmptyCandles_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => IndicatorLibrary.CalculateSMA(new List<Candle>(), 5))
            .Message.ShouldContain("Candles cannot be null or empty");
    }

    [Fact]
    public void CalculateSMA_WithInvalidPeriod_ShouldThrowArgumentException()
    {
        // Arrange
        var candles = CreateTestCandles(10);

        // Act & Assert
        Should.Throw<ArgumentException>(() => IndicatorLibrary.CalculateSMA(candles, 0))
            .Message.ShouldContain("Period must be greater than zero");
    }

    [Fact]
    public void CalculateSMA_WithInsufficientData_ShouldThrowArgumentException()
    {
        // Arrange
        var candles = CreateTestCandles(5);
        var period = 10;

        // Act & Assert
        Should.Throw<ArgumentException>(() => IndicatorLibrary.CalculateSMA(candles, period))
            .Message.ShouldContain("Insufficient data");
    }

    #endregion

    #region EMA Tests

    [Fact]
    public void CalculateEMA_WithValidData_ShouldReturnValue()
    {
        // Arrange
        var candles = CreateTestCandles(30);
        var period = 10;

        // Act
        var ema = IndicatorLibrary.CalculateEMA(candles, period);

        // Assert
        ema.ShouldBeGreaterThan(0);
        // EMA should be weighted towards recent prices
        ema.ShouldBeGreaterThan(IndicatorLibrary.CalculateSMA(candles.Take(period).ToList(), period));
    }

    [Fact]
    public void CalculateEMA_WithNullCandles_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => IndicatorLibrary.CalculateEMA(null!, 10))
            .Message.ShouldContain("Candles cannot be null or empty");
    }

    [Fact]
    public void CalculateEMA_WithInsufficientData_ShouldThrowArgumentException()
    {
        // Arrange
        var candles = CreateTestCandles(5);

        // Act & Assert
        Should.Throw<ArgumentException>(() => IndicatorLibrary.CalculateEMA(candles, 10))
            .Message.ShouldContain("Insufficient data");
    }

    #endregion

    #region RSI Tests

    [Fact]
    public void CalculateRSI_WithAllGains_ShouldReturn100()
    {
        // Arrange - All prices increasing
        var candles = CreateTestCandles(20); // Steadily increasing prices

        // Act
        var rsi = IndicatorLibrary.CalculateRSI(candles, 14);

        // Assert
        rsi.ShouldBe(100m);
    }

    [Fact]
    public void CalculateRSI_WithValidData_ShouldReturnValueBetween0And100()
    {
        // Arrange
        var candles = CreateVolatileCandles();

        // Act
        var rsi = IndicatorLibrary.CalculateRSI(candles, 14);

        // Assert
        rsi.ShouldBeGreaterThanOrEqualTo(0m);
        rsi.ShouldBeLessThanOrEqualTo(100m);
    }

    [Fact]
    public void CalculateRSI_WithNullCandles_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => IndicatorLibrary.CalculateRSI(null!, 14))
            .Message.ShouldContain("Candles cannot be null or empty");
    }

    [Fact]
    public void CalculateRSI_WithInsufficientData_ShouldThrowArgumentException()
    {
        // Arrange
        var candles = CreateTestCandles(10);

        // Act & Assert
        Should.Throw<ArgumentException>(() => IndicatorLibrary.CalculateRSI(candles, 14))
            .Message.ShouldContain("Insufficient data");
    }

    #endregion

    #region MACD Tests

    [Fact]
    public void CalculateMACD_WithValidData_ShouldReturnAllComponents()
    {
        // Arrange
        var candles = CreateTestCandles(50);

        // Act
        var (macd, signal, histogram) = IndicatorLibrary.CalculateMACD(candles, 12, 26, 9);

        // Assert
        macd.ShouldNotBe(0m);
        signal.ShouldNotBe(0m);
        histogram.ShouldBe(macd - signal);
    }

    [Fact]
    public void CalculateMACD_WithNullCandles_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => IndicatorLibrary.CalculateMACD(null!, 12, 26, 9))
            .Message.ShouldContain("Candles cannot be null or empty");
    }

    [Fact]
    public void CalculateMACD_WithInvalidPeriods_ShouldThrowArgumentException()
    {
        // Arrange
        var candles = CreateTestCandles(50);

        // Act & Assert - Fast period >= slow period
        Should.Throw<ArgumentException>(() => IndicatorLibrary.CalculateMACD(candles, 26, 12, 9))
            .Message.ShouldContain("Fast period must be less than slow period");
    }

    [Fact]
    public void CalculateMACD_WithInsufficientData_ShouldThrowArgumentException()
    {
        // Arrange
        var candles = CreateTestCandles(20);

        // Act & Assert
        Should.Throw<ArgumentException>(() => IndicatorLibrary.CalculateMACD(candles, 12, 26, 9))
            .Message.ShouldContain("Insufficient data");
    }

    #endregion

    #region Bollinger Bands Tests

    [Fact]
    public void CalculateBollingerBands_WithValidData_ShouldReturnAllBands()
    {
        // Arrange
        var candles = CreateTestCandles(30);

        // Act
        var (upper, middle, lower) = IndicatorLibrary.CalculateBollingerBands(candles, 20, 2.0);

        // Assert
        upper.ShouldBeGreaterThan(middle);
        middle.ShouldBeGreaterThan(lower);
        // Middle band should equal SMA
        middle.ShouldBe(IndicatorLibrary.CalculateSMA(candles, 20));
    }

    [Fact]
    public void CalculateBollingerBands_WithVolatileData_ShouldHaveWideBands()
    {
        // Arrange
        var candles = CreateVolatileCandles();

        // Act
        var (upper, _, lower) = IndicatorLibrary.CalculateBollingerBands(candles, 10, 2.0);

        // Assert
        var bandwidth = upper - lower;
        bandwidth.ShouldBeGreaterThan(0m);
    }

    [Fact]
    public void CalculateBollingerBands_WithNullCandles_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => IndicatorLibrary.CalculateBollingerBands(null!, 20, 2.0))
            .Message.ShouldContain("Candles cannot be null or empty");
    }

    [Fact]
    public void CalculateBollingerBands_WithInvalidStdDevMultiplier_ShouldThrowArgumentException()
    {
        // Arrange
        var candles = CreateTestCandles(30);

        // Act & Assert
        Should.Throw<ArgumentException>(() => IndicatorLibrary.CalculateBollingerBands(candles, 20, -1.0))
            .Message.ShouldContain("Standard deviation multiplier must be greater than zero");
    }

    #endregion

    #region ATR Tests

    [Fact]
    public void CalculateATR_WithValidData_ShouldReturnPositiveValue()
    {
        // Arrange
        var candles = CreateVolatileCandles();

        // Act
        var atr = IndicatorLibrary.CalculateATR(candles, 14);

        // Assert
        atr.ShouldBeGreaterThan(0m);
    }

    [Fact]
    public void CalculateATR_WithHighVolatility_ShouldReturnHigherValue()
    {
        // Arrange
        var lowVolatilityCandles = CreateTestCandles(20); // Steady increase
        var highVolatilityCandles = CreateVolatileCandles(); // Choppy price action

        // Act
        var lowAtr = IndicatorLibrary.CalculateATR(lowVolatilityCandles, 14);
        var highAtr = IndicatorLibrary.CalculateATR(highVolatilityCandles, 14);

        // Assert
        highAtr.ShouldBeGreaterThan(lowAtr);
    }

    [Fact]
    public void CalculateATR_WithNullCandles_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => IndicatorLibrary.CalculateATR(null!, 14))
            .Message.ShouldContain("Candles cannot be null or empty");
    }

    [Fact]
    public void CalculateATR_WithInsufficientData_ShouldThrowArgumentException()
    {
        // Arrange
        var candles = CreateTestCandles(10);

        // Act & Assert
        Should.Throw<ArgumentException>(() => IndicatorLibrary.CalculateATR(candles, 14))
            .Message.ShouldContain("Insufficient data");
    }

    #endregion
}
