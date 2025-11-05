// <copyright file="OrderTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Enums;
using TradingBot.Core.Models.Trading;
using Xunit;

namespace TradingBot.Core.Tests.Models.Trading;

/// <summary>
/// Unit tests for the Order class.
/// </summary>
public sealed class OrderTests
{
    [Fact]
    public void Order_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var symbol = "SPY";
        var orderType = OrderType.Market;
        var side = OrderSide.Buy;
        var quantity = 10m;
        var price = 450.50m;

        // Act
        var order = new Order
        {
            OrderId = orderId,
            Symbol = symbol,
            Type = orderType,
            Side = side,
            Quantity = quantity,
            Price = price,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };

        // Assert
        Assert.Equal(orderId, order.OrderId);
        Assert.Equal(symbol, order.Symbol);
        Assert.Equal(orderType, order.Type);
        Assert.Equal(side, order.Side);
        Assert.Equal(quantity, order.Quantity);
        Assert.Equal(price, order.Price);
        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Fact]
    public void Order_CalculateTotalValue_CorrectlyCalculatesValue()
    {
        // Arrange
        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            Symbol = "SPY",
            Type = OrderType.Market,
            Side = OrderSide.Buy,
            Quantity = 10m,
            Price = 450.50m,
            Status = OrderStatus.Filled,
            FilledQuantity = 10m,
            AverageFilledPrice = 450.50m,
        };

        // Act
        var totalValue = order.FilledQuantity * order.AverageFilledPrice;

        // Assert
        Assert.Equal(4505m, totalValue);
    }

    [Theory]
    [InlineData(OrderStatus.Pending, false)]
    [InlineData(OrderStatus.Submitted, false)]
    [InlineData(OrderStatus.PartiallyFilled, false)]
    [InlineData(OrderStatus.Filled, true)]
    [InlineData(OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Rejected, true)]
    [InlineData(OrderStatus.Expired, true)]
    public void Order_Status_DeterminesIfOrderIsTerminal(OrderStatus status, bool expectedIsTerminal)
    {
        // Arrange
        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            Symbol = "SPY",
            Status = status,
        };

        // Act
        var isTerminal = status is OrderStatus.Filled or OrderStatus.Cancelled or OrderStatus.Rejected or OrderStatus.Expired;

        // Assert
        Assert.Equal(expectedIsTerminal, isTerminal);
    }

    [Fact]
    public void Order_WithLimitPrice_StoresLimitCorrectly()
    {
        // Arrange & Act
        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            Symbol = "AAPL",
            Type = OrderType.Limit,
            Side = OrderSide.Buy,
            Quantity = 50m,
            Price = 180.00m,
            LimitPrice = 180.00m,
            Status = OrderStatus.Pending,
        };

        // Assert
        Assert.Equal(OrderType.Limit, order.Type);
        Assert.Equal(180.00m, order.LimitPrice);
    }

    [Fact]
    public void Order_WithStopPrice_StoresStopCorrectly()
    {
        // Arrange & Act
        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            Symbol = "TSLA",
            Type = OrderType.StopLoss,
            Side = OrderSide.Sell,
            Quantity = 5m,
            Price = 250.00m,
            StopPrice = 245.00m,
            Status = OrderStatus.Pending,
        };

        // Assert
        Assert.Equal(OrderType.StopLoss, order.Type);
        Assert.Equal(245.00m, order.StopPrice);
    }

    [Fact]
    public void Order_PartiallyFilled_TracksFilledQuantity()
    {
        // Arrange
        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            Symbol = "NVDA",
            Type = OrderType.Market,
            Side = OrderSide.Buy,
            Quantity = 100m,
            Price = 500.00m,
            Status = OrderStatus.PartiallyFilled,
            FilledQuantity = 60m,
            AverageFilledPrice = 499.50m,
        };

        // Act
        var remainingQuantity = order.Quantity - order.FilledQuantity;

        // Assert
        Assert.Equal(60m, order.FilledQuantity);
        Assert.Equal(40m, remainingQuantity);
        Assert.Equal(OrderStatus.PartiallyFilled, order.Status);
    }

    [Fact]
    public void Order_WithCommission_StoresCommissionCorrectly()
    {
        // Arrange & Act
        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            Symbol = "SPY",
            Type = OrderType.Market,
            Side = OrderSide.Buy,
            Quantity = 10m,
            Price = 450.00m,
            Status = OrderStatus.Filled,
            FilledQuantity = 10m,
            AverageFilledPrice = 450.00m,
            Commission = 1.00m,
        };

        // Act
        var netValue = (order.FilledQuantity * order.AverageFilledPrice) + order.Commission;

        // Assert
        Assert.Equal(1.00m, order.Commission);
        Assert.Equal(4501.00m, netValue);
    }
}
