using Shouldly;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Tests.ValueObjects;

public class TradeSignalValidationTests
{
    #region Hold Signal Validation

    [Fact]
    public void TradeSignal_HoldWithZeroQuantity_ShouldBeCreated()
    {
        // Arrange & Act
        TradeSignal signal = new(SignalType.Hold, 100m, 0, "Market neutral");

        // Assert
        signal.Type.ShouldBe(SignalType.Hold);
        signal.Quantity.ShouldBe(0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void TradeSignal_HoldWithPositiveQuantity_ShouldThrow(int quantity)
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new TradeSignal(
            SignalType.Hold,
            100m,
            quantity,
            "Market neutral"
        ));
    }

    #endregion

    #region Buy Signal Validation

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void TradeSignal_BuyWithPositiveQuantity_ShouldBeCreated(int quantity)
    {
        // Arrange & Act
        TradeSignal signal = new(SignalType.Buy, 100m, quantity, "Bullish signal");

        // Assert
        signal.Type.ShouldBe(SignalType.Buy);
        signal.Quantity.ShouldBe(quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void TradeSignal_BuyWithInvalidQuantity_ShouldThrow(int quantity)
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new TradeSignal(
            SignalType.Buy,
            100m,
            quantity,
            "Bullish signal"
        ));
    }

    #endregion

    #region Sell Signal Validation

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void TradeSignal_SellWithPositiveQuantity_ShouldBeCreated(int quantity)
    {
        // Arrange & Act
        TradeSignal signal = new(SignalType.Sell, 100m, quantity, "Bearish signal");

        // Assert
        signal.Type.ShouldBe(SignalType.Sell);
        signal.Quantity.ShouldBe(quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void TradeSignal_SellWithInvalidQuantity_ShouldThrow(int quantity)
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new TradeSignal(
            SignalType.Sell,
            100m,
            quantity,
            "Bearish signal"
        ));
    }

    #endregion

    #region Immutability Tests

    [Fact]
    public void TradeSignal_IsImmutable()
    {
        // Arrange
        TradeSignal signal1 = new(SignalType.Buy, 100m, 10, "Test");
        TradeSignal signal2 = new(SignalType.Buy, 100m, 10, "Test");

        // Act & Assert - Records are immutable and support value equality
        signal1.ShouldBe(signal2);
        (signal1 == signal2).ShouldBeTrue();
    }

    #endregion

    #region Price Validation

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void TradeSignal_BuyWithInvalidPrice_ShouldThrow(decimal price)
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new TradeSignal(
            SignalType.Buy,
            price,
            10,
            "Test"
        ));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void TradeSignal_SellWithInvalidPrice_ShouldThrow(decimal price)
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new TradeSignal(
            SignalType.Sell,
            price,
            10,
            "Test"
        ));
    }

    [Fact]
    public void TradeSignal_HoldWithZeroPrice_ShouldNotThrow()
    {
        // Arrange & Act
        TradeSignal signal = new TradeSignal(
            SignalType.Hold,
            0m,  // Price is irrelevant for Hold signals
            0,
            "Holding position"
        );

        // Assert
        signal.Price.ShouldBe(0m);
        signal.Type.ShouldBe(SignalType.Hold);
    }

    #endregion

    #region Reason Validation

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TradeSignal_WithInvalidReason_ShouldThrow(string? reason)
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new TradeSignal(
            SignalType.Buy,
            100m,
            10,
            reason!
        ));
    }

    #endregion
}
