using Shouldly;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Domain.Tests.Entities;

public class PositionValidationTests
{
    #region Ticker Validation

    [Fact]
    public void Position_WithValidTicker_ShouldBeCreated()
    {
        // Arrange & Act
        Position position = new()
        {
            Ticker = "AAPL",
            Quantity = 10,
            EntryPrice = 150m,
            EntryDate = DateTime.Today
        };

        // Assert
        position.Ticker.ShouldBe("AAPL");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Position_WithInvalidTicker_ShouldThrow(string? ticker)
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new Position
        {
            Ticker = ticker!,
            Quantity = 10,
            EntryPrice = 150m,
            EntryDate = DateTime.Today
        });
    }

    #endregion

    #region Quantity Validation

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(1000)]
    public void Position_WithPositiveQuantity_ShouldBeCreated(int quantity)
    {
        // Arrange & Act
        Position position = new()
        {
            Ticker = "AAPL",
            Quantity = quantity,
            EntryPrice = 150m,
            EntryDate = DateTime.Today
        };

        // Assert
        position.Quantity.ShouldBe(quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Position_WithInvalidQuantity_ShouldThrow(int quantity)
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new Position
        {
            Ticker = "AAPL",
            Quantity = quantity,
            EntryPrice = 150m,
            EntryDate = DateTime.Today
        });
    }

    #endregion

    #region EntryPrice Validation

    [Theory]
    [InlineData(0.01)]
    [InlineData(1)]
    [InlineData(150.50)]
    [InlineData(10000)]
    public void Position_WithPositiveEntryPrice_ShouldBeCreated(decimal entryPrice)
    {
        // Arrange & Act
        Position position = new()
        {
            Ticker = "AAPL",
            Quantity = 10,
            EntryPrice = entryPrice,
            EntryDate = DateTime.Today
        };

        // Assert
        position.EntryPrice.ShouldBe(entryPrice);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void Position_WithInvalidEntryPrice_ShouldThrow(decimal entryPrice)
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new Position
        {
            Ticker = "AAPL",
            Quantity = 10,
            EntryPrice = entryPrice,
            EntryDate = DateTime.Today
        });
    }

    #endregion

    #region EntryDate Validation

    [Fact]
    public void Position_WithFutureEntryDate_ShouldThrow()
    {
        // Arrange
        DateTime futureDate = DateTime.Today.AddDays(1);

        // Act & Assert
        Should.Throw<ArgumentException>(() => new Position
        {
            Ticker = "AAPL",
            Quantity = 10,
            EntryPrice = 150m,
            EntryDate = futureDate
        });
    }

    [Fact]
    public void Position_WithTodayEntryDate_ShouldBeCreated()
    {
        // Arrange & Act
        Position position = new()
        {
            Ticker = "AAPL",
            Quantity = 10,
            EntryPrice = 150m,
            EntryDate = DateTime.Today
        };

        // Assert
        position.EntryDate.ShouldBe(DateTime.Today);
    }

    [Fact]
    public void Position_WithPastEntryDate_ShouldBeCreated()
    {
        // Arrange
        DateTime pastDate = DateTime.Today.AddDays(-30);

        // Act
        Position position = new()
        {
            Ticker = "AAPL",
            Quantity = 10,
            EntryPrice = 150m,
            EntryDate = pastDate
        };

        // Assert
        position.EntryDate.ShouldBe(pastDate);
    }

    #endregion
}
