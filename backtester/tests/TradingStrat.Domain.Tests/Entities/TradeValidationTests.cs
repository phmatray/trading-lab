using Shouldly;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Domain.Tests.Entities;

public class TradeValidationTests
{
    #region Price Validation

    [Theory]
    [InlineData(0.01)]
    [InlineData(1)]
    [InlineData(150.50)]
    public void Trade_WithPositivePrice_ShouldBeCreated(decimal price)
    {
        // Arrange & Act
        Trade trade = new()
        {
            Type = TradeType.Buy,
            Quantity = 10,
            Price = price,
            DateTime = DateTime.Now,
            Commission = 1.0m
        };

        // Assert
        trade.Price.ShouldBe(price);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void Trade_WithInvalidPrice_ShouldThrow(decimal price)
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new Trade
        {
            Type = TradeType.Buy,
            Quantity = 10,
            Price = price,
            DateTime = DateTime.Now,
            Commission = 1.0m
        });
    }

    #endregion

    #region Quantity Validation

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(1000)]
    public void Trade_WithPositiveQuantity_ShouldBeCreated(int quantity)
    {
        // Arrange & Act
        Trade trade = new()
        {
            Type = TradeType.Buy,
            Quantity = quantity,
            Price = 150m,
            DateTime = DateTime.Now,
            Commission = 1.0m
        };

        // Assert
        trade.Quantity.ShouldBe(quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Trade_WithInvalidQuantity_ShouldThrow(int quantity)
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new Trade
        {
            Type = TradeType.Buy,
            Quantity = quantity,
            Price = 150m,
            DateTime = DateTime.Now,
            Commission = 1.0m
        });
    }

    #endregion

    #region Commission Validation

    [Theory]
    [InlineData(0)]
    [InlineData(1.0)]
    [InlineData(10.50)]
    public void Trade_WithNonNegativeCommission_ShouldBeCreated(decimal commission)
    {
        // Arrange & Act
        Trade trade = new()
        {
            Type = TradeType.Buy,
            Quantity = 10,
            Price = 150m,
            DateTime = DateTime.Now,
            Commission = commission
        };

        // Assert
        trade.Commission.ShouldBe(commission);
    }

    [Fact]
    public void Trade_WithNegativeCommission_ShouldThrow()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new Trade
        {
            Type = TradeType.Buy,
            Quantity = 10,
            Price = 150m,
            DateTime = DateTime.Now,
            Commission = -1.0m
        });
    }

    #endregion

    #region Type Validation

    [Theory]
    [InlineData(TradeType.Buy)]
    [InlineData(TradeType.Sell)]
    public void Trade_WithValidType_ShouldBeCreated(TradeType type)
    {
        // Arrange & Act
        Trade trade = new()
        {
            Type = type,
            Quantity = 10,
            Price = 150m,
            DateTime = DateTime.Now,
            Commission = 1.0m
        };

        // Assert
        trade.Type.ShouldBe(type);
    }

    #endregion
}
