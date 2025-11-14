// <copyright file="OrderTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Shouldly;
using TradingBot.Core.Enums;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Core.Tests.Models.Trading;

/// <summary>
/// Unit tests for the Order model.
/// </summary>
public sealed class OrderTests
{
    [Fact]
    public void Order_WhenCreatedWithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var orderId = Guid.NewGuid();
        var signalId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var order = new Order
        {
            Id = orderId,
            Symbol = "SPY",
            Type = OrderType.Market,
            Side = OrderSide.Buy,
            Quantity = 10m,
            Status = OrderStatus.Pending,
            CreatedAt = createdAt,
            StrategyName = "TestStrategy",
            SignalId = signalId,
        };

        // Assert
        order.Id.ShouldBe(orderId);
        order.Symbol.ShouldBe("SPY");
        order.Type.ShouldBe(OrderType.Market);
        order.Side.ShouldBe(OrderSide.Buy);
        order.Quantity.ShouldBe(10m);
        order.Status.ShouldBe(OrderStatus.Pending);
        order.CreatedAt.ShouldBe(createdAt);
        order.StrategyName.ShouldBe("TestStrategy");
        order.SignalId.ShouldBe(signalId);
    }

    [Fact]
    public void Order_LimitOrder_ShouldAcceptLimitPrice()
    {
        // Arrange & Act
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Symbol = "AAPL",
            Type = OrderType.Limit,
            Side = OrderSide.Buy,
            Quantity = 5m,
            LimitPrice = 180.50m,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            StrategyName = "LimitStrategy",
        };

        // Assert
        order.LimitPrice.ShouldBe(180.50m);
    }

    [Fact]
    public void Order_StopLossOrder_ShouldAcceptStopPrice()
    {
        // Arrange & Act
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Symbol = "TSLA",
            Type = OrderType.StopLoss,
            Side = OrderSide.Sell,
            Quantity = 8m,
            StopPrice = 250.00m,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            StrategyName = "RiskManagement",
        };

        // Assert
        order.StopPrice.ShouldBe(250.00m);
    }

    [Fact]
    public void Order_WhenSubmitted_ShouldSetSubmittedAt()
    {
        // Arrange
        var submittedAt = DateTime.UtcNow;
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Symbol = "SPY",
            Type = OrderType.Market,
            Side = OrderSide.Buy,
            Quantity = 10m,
            Status = OrderStatus.Submitted,
            CreatedAt = DateTime.UtcNow.AddSeconds(-5),
            StrategyName = "TestStrategy",
            SubmittedAt = submittedAt,
        };

        // Assert
        order.SubmittedAt.ShouldBe(submittedAt);
        order.Status.ShouldBe(OrderStatus.Submitted);
    }

    [Fact]
    public void Order_WhenFilled_ShouldSetFilledQuantityAndAverageFillPrice()
    {
        // Arrange
        var filledAt = DateTime.UtcNow;
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Symbol = "MSFT",
            Type = OrderType.Market,
            Side = OrderSide.Buy,
            Quantity = 20m,
            Status = OrderStatus.Filled,
            CreatedAt = DateTime.UtcNow.AddSeconds(-10),
            SubmittedAt = DateTime.UtcNow.AddSeconds(-8),
            FilledAt = filledAt,
            FilledQuantity = 20m,
            AverageFillPrice = 350.25m,
            Commission = 2.50m,
            StrategyName = "MomentumStrategy",
        };

        // Assert
        order.FilledQuantity.ShouldBe(20m);
        order.AverageFillPrice.ShouldBe(350.25m);
        order.Commission.ShouldBe(2.50m);
        order.FilledAt.ShouldBe(filledAt);
        order.Status.ShouldBe(OrderStatus.Filled);
    }

    [Fact]
    public void Order_PartiallyFilled_ShouldHaveCorrectFilledQuantity()
    {
        // Arrange
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Symbol = "NVDA",
            Type = OrderType.Limit,
            Side = OrderSide.Buy,
            Quantity = 50m,
            LimitPrice = 500.00m,
            Status = OrderStatus.PartiallyFilled,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            SubmittedAt = DateTime.UtcNow.AddMinutes(-4),
            FilledQuantity = 30m,
            AverageFillPrice = 499.50m,
            Commission = 1.50m,
            StrategyName = "MeanReversionStrategy",
        };

        // Assert
        order.FilledQuantity.ShouldBe(30m);
        order.Quantity.ShouldBe(50m);
        order.Status.ShouldBe(OrderStatus.PartiallyFilled);
        order.FilledQuantity.ShouldBeLessThan(order.Quantity);
    }

    [Fact]
    public void Order_Cancelled_ShouldHaveCorrectStatus()
    {
        // Arrange
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Symbol = "AMZN",
            Type = OrderType.Limit,
            Side = OrderSide.Sell,
            Quantity = 15m,
            LimitPrice = 150.00m,
            Status = OrderStatus.Cancelled,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            SubmittedAt = DateTime.UtcNow.AddMinutes(-8),
            StrategyName = "TrendFollowing",
        };

        // Assert
        order.Status.ShouldBe(OrderStatus.Cancelled);
        order.FilledQuantity.ShouldBe(0m);
    }

    [Fact]
    public void Order_Rejected_ShouldHaveCorrectStatus()
    {
        // Arrange
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Symbol = "GOOGL",
            Type = OrderType.Market,
            Side = OrderSide.Buy,
            Quantity = 100m,
            Status = OrderStatus.Rejected,
            CreatedAt = DateTime.UtcNow.AddSeconds(-30),
            SubmittedAt = DateTime.UtcNow.AddSeconds(-28),
            StrategyName = "HighVolume",
        };

        // Assert
        order.Status.ShouldBe(OrderStatus.Rejected);
        order.FilledQuantity.ShouldBe(0m);
    }

    [Fact]
    public void Order_TrailingStop_ShouldAcceptStopPrice()
    {
        // Arrange
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Symbol = "META",
            Type = OrderType.TrailingStop,
            Side = OrderSide.Sell,
            Quantity = 25m,
            StopPrice = 290.00m,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            StrategyName = "ProtectProfit",
        };

        // Assert
        order.Type.ShouldBe(OrderType.TrailingStop);
        order.StopPrice.ShouldBe(290.00m);
    }
}
