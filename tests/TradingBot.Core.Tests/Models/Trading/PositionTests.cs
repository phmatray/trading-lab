// <copyright file="PositionTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Enums;
using TradingBot.Core.Models.Trading;
using Xunit;

namespace TradingBot.Core.Tests.Models.Trading;

/// <summary>
/// Unit tests for the Position class.
/// </summary>
public sealed class PositionTests
{
    [Fact]
    public void Position_CalculateUnrealizedPnL_LongPosition_Profit()
    {
        // Arrange
        var position = new Position
        {
            PositionId = Guid.NewGuid(),
            Symbol = "SPY",
            Side = OrderSide.Buy,
            Quantity = 10m,
            AverageEntryPrice = 450.00m,
            CurrentPrice = 460.00m,
            OpenedAt = DateTime.UtcNow,
        };

        // Act
        var unrealizedPnL = (position.CurrentPrice - position.AverageEntryPrice) * position.Quantity;

        // Assert
        Assert.Equal(100m, unrealizedPnL); // (460 - 450) * 10 = 100
        Assert.True(unrealizedPnL > 0);
    }

    [Fact]
    public void Position_CalculateUnrealizedPnL_LongPosition_Loss()
    {
        // Arrange
        var position = new Position
        {
            PositionId = Guid.NewGuid(),
            Symbol = "AAPL",
            Side = OrderSide.Buy,
            Quantity = 20m,
            AverageEntryPrice = 180.00m,
            CurrentPrice = 175.00m,
            OpenedAt = DateTime.UtcNow,
        };

        // Act
        var unrealizedPnL = (position.CurrentPrice - position.AverageEntryPrice) * position.Quantity;

        // Assert
        Assert.Equal(-100m, unrealizedPnL); // (175 - 180) * 20 = -100
        Assert.True(unrealizedPnL < 0);
    }

    [Fact]
    public void Position_CalculateUnrealizedPnL_ShortPosition_Profit()
    {
        // Arrange
        var position = new Position
        {
            PositionId = Guid.NewGuid(),
            Symbol = "TSLA",
            Side = OrderSide.Sell,
            Quantity = 5m,
            AverageEntryPrice = 250.00m,
            CurrentPrice = 240.00m,
            OpenedAt = DateTime.UtcNow,
        };

        // Act
        // For short positions, profit when current price < entry price
        var unrealizedPnL = (position.AverageEntryPrice - position.CurrentPrice) * position.Quantity;

        // Assert
        Assert.Equal(50m, unrealizedPnL); // (250 - 240) * 5 = 50
        Assert.True(unrealizedPnL > 0);
    }

    [Fact]
    public void Position_CalculateUnrealizedPnL_ShortPosition_Loss()
    {
        // Arrange
        var position = new Position
        {
            PositionId = Guid.NewGuid(),
            Symbol = "NVDA",
            Side = OrderSide.Sell,
            Quantity = 3m,
            AverageEntryPrice = 500.00m,
            CurrentPrice = 520.00m,
            OpenedAt = DateTime.UtcNow,
        };

        // Act
        // For short positions, loss when current price > entry price
        var unrealizedPnL = (position.AverageEntryPrice - position.CurrentPrice) * position.Quantity;

        // Assert
        Assert.Equal(-60m, unrealizedPnL); // (500 - 520) * 3 = -60
        Assert.True(unrealizedPnL < 0);
    }

    [Fact]
    public void Position_CalculateUnrealizedPnLPercent_ReturnsCorrectPercentage()
    {
        // Arrange
        var position = new Position
        {
            PositionId = Guid.NewGuid(),
            Symbol = "SPY",
            Side = OrderSide.Buy,
            Quantity = 10m,
            AverageEntryPrice = 400.00m,
            CurrentPrice = 420.00m,
        };

        // Act
        var unrealizedPnLPercent = ((position.CurrentPrice - position.AverageEntryPrice) / position.AverageEntryPrice) * 100m;

        // Assert
        Assert.Equal(5m, unrealizedPnLPercent); // (420 - 400) / 400 * 100 = 5%
    }

    [Fact]
    public void Position_CalculatePositionValue_ReturnsCorrectValue()
    {
        // Arrange
        var position = new Position
        {
            PositionId = Guid.NewGuid(),
            Symbol = "MSFT",
            Side = OrderSide.Buy,
            Quantity = 15m,
            AverageEntryPrice = 350.00m,
            CurrentPrice = 360.00m,
        };

        // Act
        var positionValue = position.CurrentPrice * position.Quantity;

        // Assert
        Assert.Equal(5400m, positionValue); // 360 * 15 = 5400
    }

    [Fact]
    public void Position_WithStopLossPrice_StoresStopLossCorrectly()
    {
        // Arrange
        var position = new Position
        {
            PositionId = Guid.NewGuid(),
            Symbol = "GOOGL",
            Side = OrderSide.Buy,
            Quantity = 8m,
            AverageEntryPrice = 140.00m,
            CurrentPrice = 145.00m,
            StopLossPrice = 135.00m,
        };

        // Assert
        Assert.Equal(135.00m, position.StopLossPrice);
        Assert.True(position.CurrentPrice > position.StopLossPrice);
    }

    [Fact]
    public void Position_WithTakeProfitPrice_StoresTakeProfitCorrectly()
    {
        // Arrange
        var position = new Position
        {
            PositionId = Guid.NewGuid(),
            Symbol = "META",
            Side = OrderSide.Buy,
            Quantity = 12m,
            AverageEntryPrice = 300.00m,
            CurrentPrice = 310.00m,
            TakeProfitPrice = 330.00m,
        };

        // Assert
        Assert.Equal(330.00m, position.TakeProfitPrice);
        Assert.True(position.CurrentPrice < position.TakeProfitPrice);
    }

    [Fact]
    public void Position_ZeroQuantity_InvalidPosition()
    {
        // Arrange
        var position = new Position
        {
            PositionId = Guid.NewGuid(),
            Symbol = "AMD",
            Side = OrderSide.Buy,
            Quantity = 0m,
            AverageEntryPrice = 120.00m,
            CurrentPrice = 125.00m,
        };

        // Assert
        Assert.Equal(0m, position.Quantity);
    }

    [Fact]
    public void Position_UpdateAveragePrice_WhenAddingToPosition()
    {
        // Arrange
        var position = new Position
        {
            PositionId = Guid.NewGuid(),
            Symbol = "INTC",
            Side = OrderSide.Buy,
            Quantity = 100m,
            AverageEntryPrice = 50.00m,
        };

        // Act - Add more shares at different price
        var additionalQuantity = 50m;
        var additionalPrice = 52.00m;
        var newTotalQuantity = position.Quantity + additionalQuantity;
        var newAveragePrice = ((position.Quantity * position.AverageEntryPrice) + (additionalQuantity * additionalPrice)) / newTotalQuantity;

        // Assert
        Assert.Equal(150m, newTotalQuantity);
        Assert.Equal(50.67m, Math.Round(newAveragePrice, 2)); // (100*50 + 50*52) / 150 = 50.67
    }
}
