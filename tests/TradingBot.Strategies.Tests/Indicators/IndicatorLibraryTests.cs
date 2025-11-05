// <copyright file="IndicatorLibraryTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.MarketData;
using TradingBot.Strategies.Indicators;
using Xunit;

namespace TradingBot.Strategies.Tests.Indicators;

/// <summary>
/// Unit tests for the IndicatorLibrary.
/// </summary>
public sealed class IndicatorLibraryTests
{
    [Fact]
    public void CalculateSMA_WithValidData_ReturnsCorrectAverage()
    {
        // Arrange
        var candles = new List<Candle>
        {
            CreateCandle(100m, 105m, 95m, 100m),
            CreateCandle(101m, 106m, 96m, 102m),
            CreateCandle(103m, 108m, 98m, 105m),
            CreateCandle(104m, 109m, 99m, 107m),
            CreateCandle(106m, 111m, 101m, 110m),
        };

        // Act
        var sma = IndicatorLibrary.CalculateSMA(candles, 5);

        // Assert
        // SMA = (100 + 102 + 105 + 107 + 110) / 5 = 104.8
        Assert.Equal(104.8m, sma);
    }

    [Fact]
    public void CalculateSMA_WithInsufficientData_ThrowsException()
    {
        // Arrange
        var candles = new List<Candle>
        {
            CreateCandle(100m, 105m, 95m, 100m),
            CreateCandle(101m, 106m, 96m, 102m),
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => IndicatorLibrary.CalculateSMA(candles, 5));
    }

    [Fact]
    public void CalculateEMA_WithValidData_ReturnsCorrectValue()
    {
        // Arrange
        var candles = new List<Candle>
        {
            CreateCandle(100m, 105m, 95m, 100m),
            CreateCandle(101m, 106m, 96m, 102m),
            CreateCandle(103m, 108m, 98m, 105m),
            CreateCandle(104m, 109m, 99m, 107m),
            CreateCandle(106m, 111m, 101m, 110m),
            CreateCandle(108m, 113m, 103m, 112m),
        };

        // Act
        var ema = IndicatorLibrary.CalculateEMA(candles, 5);

        // Assert
        Assert.True(ema > 0);
        Assert.True(ema >= 100m && ema <= 115m);
    }

    [Fact]
    public void CalculateRSI_WithUptrend_ReturnsHighValue()
    {
        // Arrange - Create uptrending data
        var candles = new List<Candle>();
        for (int i = 0; i < 20; i++)
        {
            var close = 100m + (i * 2m); // Steadily increasing
            candles.Add(CreateCandle(close - 2m, close + 1m, close - 3m, close));
        }

        // Act
        var rsi = IndicatorLibrary.CalculateRSI(candles, 14);

        // Assert
        Assert.True(rsi > 50m, $"RSI should be > 50 for uptrend, got {rsi}");
        Assert.True(rsi <= 100m, $"RSI should be <= 100, got {rsi}");
    }

    [Fact]
    public void CalculateRSI_WithDowntrend_ReturnsLowValue()
    {
        // Arrange - Create downtrending data
        var candles = new List<Candle>();
        for (int i = 0; i < 20; i++)
        {
            var close = 140m - (i * 2m); // Steadily decreasing
            candles.Add(CreateCandle(close + 2m, close + 3m, close - 1m, close));
        }

        // Act
        var rsi = IndicatorLibrary.CalculateRSI(candles, 14);

        // Assert
        Assert.True(rsi < 50m, $"RSI should be < 50 for downtrend, got {rsi}");
        Assert.True(rsi >= 0m, $"RSI should be >= 0, got {rsi}");
    }

    [Fact]
    public void CalculateMACD_WithValidData_ReturnsValues()
    {
        // Arrange
        var candles = new List<Candle>();
        for (int i = 0; i < 30; i++)
        {
            var close = 100m + (i * 0.5m);
            candles.Add(CreateCandle(close - 1m, close + 1m, close - 2m, close));
        }

        // Act
        var (macd, signal, histogram) = IndicatorLibrary.CalculateMACD(candles, 12, 26, 9);

        // Assert
        Assert.NotEqual(0m, macd);
        Assert.NotEqual(0m, signal);
        Assert.Equal(macd - signal, histogram);
    }

    [Fact]
    public void CalculateBollingerBands_WithValidData_ReturnsCorrectBands()
    {
        // Arrange
        var candles = new List<Candle>();
        for (int i = 0; i < 25; i++)
        {
            var close = 100m + (decimal)(Math.Sin(i * 0.3) * 5);
            candles.Add(CreateCandle(close - 1m, close + 1m, close - 2m, close));
        }

        // Act
        var (upper, middle, lower) = IndicatorLibrary.CalculateBollingerBands(candles, 20, 2.0);

        // Assert
        Assert.True(upper > middle, "Upper band should be above middle");
        Assert.True(middle > lower, "Middle band should be above lower");
        Assert.True(upper - middle > 0, "Bands should have width");
    }

    [Fact]
    public void CalculateATR_WithValidData_ReturnsPositiveValue()
    {
        // Arrange
        var candles = new List<Candle>();
        for (int i = 0; i < 20; i++)
        {
            var close = 100m + (i * 1m);
            candles.Add(CreateCandle(close - 2m, close + 3m, close - 4m, close));
        }

        // Act
        var atr = IndicatorLibrary.CalculateATR(candles, 14);

        // Assert
        Assert.True(atr > 0m, "ATR should be positive");
        Assert.True(atr < 20m, "ATR should be reasonable for test data");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CalculateSMA_WithInvalidPeriod_ThrowsException(int period)
    {
        // Arrange
        var candles = CreateSampleCandles(10);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => IndicatorLibrary.CalculateSMA(candles, period));
    }

    [Fact]
    public void CalculateSMA_WithNullCandles_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => IndicatorLibrary.CalculateSMA(null!, 5));
    }

    [Fact]
    public void CalculateSMA_WithEmptyCandles_ThrowsException()
    {
        // Arrange
        var candles = new List<Candle>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => IndicatorLibrary.CalculateSMA(candles, 5));
    }

    private static Candle CreateCandle(decimal open, decimal high, decimal low, decimal close)
    {
        return new Candle
        {
            Symbol = "TEST",
            Timestamp = DateTime.UtcNow,
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Volume = 1000000m,
        };
    }

    private static List<Candle> CreateSampleCandles(int count)
    {
        var candles = new List<Candle>();
        for (int i = 0; i < count; i++)
        {
            var close = 100m + i;
            candles.Add(CreateCandle(close - 1m, close + 1m, close - 2m, close));
        }

        return candles;
    }
}
