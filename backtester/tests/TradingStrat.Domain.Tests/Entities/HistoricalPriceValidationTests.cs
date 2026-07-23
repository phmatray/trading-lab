using Shouldly;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Tests.Entities;

public class HistoricalPriceValidationTests
{
    #region OHLCV Relationships

    [Fact]
    public void HistoricalPrice_WithValidOHLCV_ShouldBeCreated()
    {
        // Arrange & Act
        HistoricalPrice price = new()
        {
            Ticker = "AAPL",
            DateTime = DateTime.Today,
            Open = 100m,
            High = 110m,
            Low = 95m,
            Close = 105m,
            Volume = 1000000
        };

        // Assert
        price.High.ShouldBe(110m);
        price.Low.ShouldBe(95m);
    }

    [Fact]
    public void HistoricalPrice_WithCloseAboveHigh_ShouldThrow()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new HistoricalPrice
        {
            Ticker = "AAPL",
            DateTime = DateTime.Today,
            Open = 100m,
            High = 105m,
            Low = 95m,
            Close = 110m, // Above high
            Volume = 1000000
        });
    }

    [Fact]
    public void HistoricalPrice_WithCloseBelowLow_ShouldThrow()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new HistoricalPrice
        {
            Ticker = "AAPL",
            DateTime = DateTime.Today,
            Open = 100m,
            High = 110m,
            Low = 95m,
            Close = 90m, // Below low
            Volume = 1000000
        });
    }

    [Fact]
    public void HistoricalPrice_WithHighBelowLow_ShouldThrow()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new HistoricalPrice
        {
            Ticker = "AAPL",
            DateTime = DateTime.Today,
            Open = 100m,
            High = 95m, // Below low
            Low = 100m,
            Close = 98m,
            Volume = 1000000
        });
    }

    #endregion

    #region Volume Validation

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1000000)]
    public void HistoricalPrice_WithNonNegativeVolume_ShouldBeCreated(long volume)
    {
        // Arrange & Act
        HistoricalPrice price = new()
        {
            Ticker = "AAPL",
            DateTime = DateTime.Today,
            Open = 100m,
            High = 110m,
            Low = 95m,
            Close = 105m,
            Volume = volume
        };

        // Assert
        price.Volume.ShouldBe(volume);
    }

    [Fact]
    public void HistoricalPrice_WithNegativeVolume_ShouldThrow()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new HistoricalPrice
        {
            Ticker = "AAPL",
            DateTime = DateTime.Today,
            Open = 100m,
            High = 110m,
            Low = 95m,
            Close = 105m,
            Volume = -1
        });
    }

    #endregion

    #region Ticker Validation

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void HistoricalPrice_WithInvalidTicker_ShouldThrow(string? ticker)
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new HistoricalPrice
        {
            Ticker = ticker!,
            DateTime = DateTime.Today,
            Open = 100m,
            High = 110m,
            Low = 95m,
            Close = 105m,
            Volume = 1000000
        });
    }

    #endregion

    #region TimeFrame Validation

    [Fact]
    public void HistoricalPrice_WithValidTimeFrame_ShouldBeCreated()
    {
        // Arrange & Act
        HistoricalPrice price = new()
        {
            Ticker = "AAPL",
            DateTime = DateTime.Today,
            Open = 100m,
            High = 110m,
            Low = 95m,
            Close = 105m,
            Volume = 1000000,
            TimeFrame = TimeFrameUnit.D1
        };

        // Assert
        price.TimeFrame.ShouldBe(TimeFrameUnit.D1);
    }

    #endregion
}
