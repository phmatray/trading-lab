// <copyright file="CandleTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Shouldly;
using TradingBot.Core.Models.MarketData;

namespace TradingBot.Core.Tests.Models.MarketData;

/// <summary>
/// Unit tests for the Candle model.
/// </summary>
public sealed class CandleTests
{
    [Fact]
    public void Candle_WhenCreatedWithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var timestamp = DateTime.UtcNow;
        var candle = new Candle
        {
            Symbol = "SPY",
            Timestamp = timestamp,
            Open = 450.00m,
            High = 455.00m,
            Low = 448.00m,
            Close = 453.00m,
            Volume = 1000000,
            Timeframe = "1h",
        };

        // Assert
        candle.Symbol.ShouldBe("SPY");
        candle.Timestamp.ShouldBe(timestamp);
        candle.Open.ShouldBe(450.00m);
        candle.High.ShouldBe(455.00m);
        candle.Low.ShouldBe(448.00m);
        candle.Close.ShouldBe(453.00m);
        candle.Volume.ShouldBe(1000000);
        candle.Timeframe.ShouldBe("1h");
    }

    [Fact]
    public void Candle_BullishCandle_ShouldHaveIsBullishTrue()
    {
        // Arrange & Act
        var candle = new Candle
        {
            Symbol = "AAPL",
            Timestamp = DateTime.UtcNow,
            Open = 180.00m,
            High = 185.00m,
            Low = 179.00m,
            Close = 184.00m, // Close > Open
            Volume = 500000,
            Timeframe = "5m",
        };

        // Assert
        candle.IsBullish.ShouldBeTrue();
    }

    [Fact]
    public void Candle_BearishCandle_ShouldHaveIsBullishFalse()
    {
        // Arrange & Act
        var candle = new Candle
        {
            Symbol = "TSLA",
            Timestamp = DateTime.UtcNow,
            Open = 250.00m,
            High = 252.00m,
            Low = 245.00m,
            Close = 246.00m, // Close < Open
            Volume = 750000,
            Timeframe = "15m",
        };

        // Assert
        candle.IsBullish.ShouldBeFalse();
    }

    [Fact]
    public void Candle_DojiCandle_ShouldHaveIsBullishTrue()
    {
        // Arrange & Act - Doji has Close == Open
        var candle = new Candle
        {
            Symbol = "NVDA",
            Timestamp = DateTime.UtcNow,
            Open = 500.00m,
            High = 505.00m,
            Low = 495.00m,
            Close = 500.00m, // Close == Open
            Volume = 300000,
            Timeframe = "1m",
        };

        // Assert
        candle.IsBullish.ShouldBeTrue(); // Close >= Open
    }

    [Fact]
    public void Candle_BodySize_ShouldBeCalculatedCorrectly()
    {
        // Arrange & Act
        var bullishCandle = new Candle
        {
            Symbol = "MSFT",
            Timestamp = DateTime.UtcNow,
            Open = 350.00m,
            High = 360.00m,
            Low = 348.00m,
            Close = 358.00m,
            Volume = 200000,
            Timeframe = "30m",
        };

        var bearishCandle = new Candle
        {
            Symbol = "GOOGL",
            Timestamp = DateTime.UtcNow,
            Open = 140.00m,
            High = 142.00m,
            Low = 135.00m,
            Close = 136.00m,
            Volume = 150000,
            Timeframe = "1h",
        };

        // Assert
        bullishCandle.BodySize.ShouldBe(8.00m); // |358 - 350| = 8
        bearishCandle.BodySize.ShouldBe(4.00m); // |136 - 140| = 4
    }

    [Fact]
    public void Candle_Range_ShouldBeCalculatedCorrectly()
    {
        // Arrange & Act
        var candle = new Candle
        {
            Symbol = "AMZN",
            Timestamp = DateTime.UtcNow,
            Open = 150.00m,
            High = 160.00m,
            Low = 145.00m,
            Close = 155.00m,
            Volume = 400000,
            Timeframe = "4h",
        };

        // Assert
        candle.Range.ShouldBe(15.00m); // 160 - 145 = 15
    }

    [Fact]
    public void Candle_TypicalPrice_ShouldBeCalculatedCorrectly()
    {
        // Arrange & Act
        var candle = new Candle
        {
            Symbol = "META",
            Timestamp = DateTime.UtcNow,
            Open = 300.00m,
            High = 315.00m,
            Low = 295.00m,
            Close = 310.00m,
            Volume = 600000,
            Timeframe = "1d",
        };

        // Assert
        candle.TypicalPrice.ShouldBe(306.666666666666666666666666667m, tolerance: 0.001m); // (315 + 295 + 310) / 3
    }

    [Fact]
    public void Candle_WithDifferentTimeframes_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var oneMinute = new Candle
        {
            Symbol = "BTC",
            Timestamp = DateTime.UtcNow,
            Open = 45000m,
            High = 45100m,
            Low = 44900m,
            Close = 45050m,
            Volume = 100,
            Timeframe = "1m",
        };

        var oneDay = new Candle
        {
            Symbol = "ETH",
            Timestamp = DateTime.UtcNow,
            Open = 3000m,
            High = 3100m,
            Low = 2950m,
            Close = 3080m,
            Volume = 5000,
            Timeframe = "1d",
        };

        // Assert
        oneMinute.Timeframe.ShouldBe("1m");
        oneDay.Timeframe.ShouldBe("1d");
    }

    [Fact]
    public void Candle_AsRecord_ShouldSupportValueEquality()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var candle1 = new Candle
        {
            Symbol = "SPY",
            Timestamp = timestamp,
            Open = 450.00m,
            High = 455.00m,
            Low = 448.00m,
            Close = 453.00m,
            Volume = 1000000,
            Timeframe = "1h",
        };

        var candle2 = new Candle
        {
            Symbol = "SPY",
            Timestamp = timestamp,
            Open = 450.00m,
            High = 455.00m,
            Low = 448.00m,
            Close = 453.00m,
            Volume = 1000000,
            Timeframe = "1h",
        };

        // Act & Assert - Records support value equality
        candle1.ShouldNotBeSameAs(candle2);
        candle1.Equals(candle2).ShouldBeTrue();
    }

    [Fact]
    public void Candle_WithHighVolume_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var candle = new Candle
        {
            Symbol = "SPY",
            Timestamp = DateTime.UtcNow,
            Open = 450.00m,
            High = 455.00m,
            Low = 448.00m,
            Close = 453.00m,
            Volume = 999_999_999,
            Timeframe = "1d",
        };

        // Assert
        candle.Volume.ShouldBe(999_999_999);
    }

    [Fact]
    public void Candle_DojiWithZeroBodySize_ShouldCalculateCorrectly()
    {
        // Arrange & Act
        var doji = new Candle
        {
            Symbol = "DIS",
            Timestamp = DateTime.UtcNow,
            Open = 90.00m,
            High = 92.00m,
            Low = 88.00m,
            Close = 90.00m, // Same as open
            Volume = 250000,
            Timeframe = "1h",
        };

        // Assert
        doji.BodySize.ShouldBe(0m);
        doji.IsBullish.ShouldBeTrue();
        doji.Range.ShouldBe(4.00m);
    }

    [Fact]
    public void Candle_WithSmallRange_ShouldCalculateCorrectly()
    {
        // Arrange & Act
        var candle = new Candle
        {
            Symbol = "COIN",
            Timestamp = DateTime.UtcNow,
            Open = 100.00m,
            High = 100.10m,
            Low = 99.90m,
            Close = 100.05m,
            Volume = 50000,
            Timeframe = "1m",
        };

        // Assert
        candle.Range.ShouldBe(0.20m);
        candle.BodySize.ShouldBe(0.05m);
    }
}
