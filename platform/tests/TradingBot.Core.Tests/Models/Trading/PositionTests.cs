// <copyright file="PositionTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Enums;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Core.Tests.Models.Trading;

/// <summary>
/// Unit tests for the Position model.
/// </summary>
public sealed class PositionTests
{
    [Fact]
    public void Position_WhenCreatedWithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var positionId = Guid.NewGuid();
        var openedAt = DateTime.UtcNow;

        var position = new Position
        {
            Id = positionId,
            Symbol = "SPY",
            Side = OrderSide.Buy,
            Quantity = 10m,
            EntryPrice = 450.00m,
            CurrentPrice = 455.00m,
            OpenedAt = openedAt,
            StrategyName = "MomentumStrategy",
        };

        // Assert
        position.Id.ShouldBe(positionId);
        position.Symbol.ShouldBe("SPY");
        position.Side.ShouldBe(OrderSide.Buy);
        position.Quantity.ShouldBe(10m);
        position.EntryPrice.ShouldBe(450.00m);
        position.CurrentPrice.ShouldBe(455.00m);
        position.OpenedAt.ShouldBe(openedAt);
        position.StrategyName.ShouldBe("MomentumStrategy");
    }

    [Fact]
    public void Position_LongPosition_WhenPriceIncreases_ShouldHavePositiveUnrealizedPnL()
    {
        // Arrange
        var position = new Position
        {
            Id = Guid.NewGuid(),
            Symbol = "AAPL",
            Side = OrderSide.Buy, // Long position
            Quantity = 20m,
            EntryPrice = 180.00m,
            CurrentPrice = 185.00m, // Price went up
            OpenedAt = DateTime.UtcNow,
            StrategyName = "TrendFollowing",
        };

        // Act
        var unrealizedPnL = position.UnrealizedPnL;
        var unrealizedPnLPercent = position.UnrealizedPnLPercent;

        // Assert
        unrealizedPnL.ShouldBe(100m); // (185 - 180) * 20 = 100
        unrealizedPnLPercent.ShouldBe(2.7777777777777777777777777778m, tolerance: 0.0001m); // ((185-180)/180) * 100 = 2.78%
    }

    [Fact]
    public void Position_LongPosition_WhenPriceDecreases_ShouldHaveNegativeUnrealizedPnL()
    {
        // Arrange
        var position = new Position
        {
            Id = Guid.NewGuid(),
            Symbol = "TSLA",
            Side = OrderSide.Buy, // Long position
            Quantity = 15m,
            EntryPrice = 250.00m,
            CurrentPrice = 240.00m, // Price went down
            OpenedAt = DateTime.UtcNow,
            StrategyName = "MomentumStrategy",
        };

        // Act
        var unrealizedPnL = position.UnrealizedPnL;
        var unrealizedPnLPercent = position.UnrealizedPnLPercent;

        // Assert
        unrealizedPnL.ShouldBe(-150m); // (240 - 250) * 15 = -150
        unrealizedPnLPercent.ShouldBe(-4m); // ((240-250)/250) * 100 = -4%
    }

    [Fact]
    public void Position_ShortPosition_WhenPriceDecreases_ShouldHavePositiveUnrealizedPnL()
    {
        // Arrange
        var position = new Position
        {
            Id = Guid.NewGuid(),
            Symbol = "NVDA",
            Side = OrderSide.Sell, // Short position
            Quantity = 10m,
            EntryPrice = 500.00m,
            CurrentPrice = 480.00m, // Price went down (good for short)
            OpenedAt = DateTime.UtcNow,
            StrategyName = "ShortSelling",
        };

        // Act
        var unrealizedPnL = position.UnrealizedPnL;
        var unrealizedPnLPercent = position.UnrealizedPnLPercent;

        // Assert
        unrealizedPnL.ShouldBe(200m); // (500 - 480) * 10 = 200
        unrealizedPnLPercent.ShouldBe(4m); // ((500-480)/500) * 100 = 4%
    }

    [Fact]
    public void Position_ShortPosition_WhenPriceIncreases_ShouldHaveNegativeUnrealizedPnL()
    {
        // Arrange
        var position = new Position
        {
            Id = Guid.NewGuid(),
            Symbol = "AMZN",
            Side = OrderSide.Sell, // Short position
            Quantity = 8m,
            EntryPrice = 150.00m,
            CurrentPrice = 160.00m, // Price went up (bad for short)
            OpenedAt = DateTime.UtcNow,
            StrategyName = "ShortSelling",
        };

        // Act
        var unrealizedPnL = position.UnrealizedPnL;
        var unrealizedPnLPercent = position.UnrealizedPnLPercent;

        // Assert
        unrealizedPnL.ShouldBe(-80m); // (150 - 160) * 8 = -80
        unrealizedPnLPercent.ShouldBe(-6.6666666666666666666666666667m, tolerance: 0.0001m); // ((150-160)/150) * 100 = -6.67%
    }

    [Fact]
    public void Position_PositionValue_ShouldBeCalculatedCorrectly()
    {
        // Arrange
        var position = new Position
        {
            Id = Guid.NewGuid(),
            Symbol = "MSFT",
            Side = OrderSide.Buy,
            Quantity = 25m,
            EntryPrice = 350.00m,
            CurrentPrice = 360.00m,
            OpenedAt = DateTime.UtcNow,
            StrategyName = "TechGrowth",
        };

        // Act
        var positionValue = position.PositionValue;

        // Assert
        positionValue.ShouldBe(9000m); // 25 * 360 = 9000
    }

    [Fact]
    public void Position_WithStopLoss_ShouldSetStopLossPrice()
    {
        // Arrange
        var position = new Position
        {
            Id = Guid.NewGuid(),
            Symbol = "META",
            Side = OrderSide.Buy,
            Quantity = 12m,
            EntryPrice = 300.00m,
            CurrentPrice = 310.00m,
            OpenedAt = DateTime.UtcNow,
            StopLoss = 290.00m, // 10 points below entry
            StrategyName = "RiskManaged",
        };

        // Assert
        position.StopLoss.ShouldBe(290.00m);
    }

    [Fact]
    public void Position_WithTakeProfit_ShouldSetTakeProfitPrice()
    {
        // Arrange
        var position = new Position
        {
            Id = Guid.NewGuid(),
            Symbol = "GOOGL",
            Side = OrderSide.Buy,
            Quantity = 18m,
            EntryPrice = 140.00m,
            CurrentPrice = 145.00m,
            OpenedAt = DateTime.UtcNow,
            TakeProfit = 155.00m, // 15 points above entry
            StrategyName = "ProfitTarget",
        };

        // Assert
        position.TakeProfit.ShouldBe(155.00m);
    }

    [Fact]
    public void Position_WithStopLossAndTakeProfit_ShouldSetBothPrices()
    {
        // Arrange
        var position = new Position
        {
            Id = Guid.NewGuid(),
            Symbol = "BRK.B",
            Side = OrderSide.Buy,
            Quantity = 30m,
            EntryPrice = 350.00m,
            CurrentPrice = 355.00m,
            OpenedAt = DateTime.UtcNow,
            StopLoss = 340.00m,
            TakeProfit = 370.00m,
            StrategyName = "BracketOrder",
        };

        // Assert
        position.StopLoss.ShouldBe(340.00m);
        position.TakeProfit.ShouldBe(370.00m);
    }

    [Fact]
    public void Position_WhenPriceEqualsEntry_ShouldHaveZeroPnL()
    {
        // Arrange
        var position = new Position
        {
            Id = Guid.NewGuid(),
            Symbol = "DIS",
            Side = OrderSide.Buy,
            Quantity = 50m,
            EntryPrice = 90.00m,
            CurrentPrice = 90.00m, // Same as entry
            OpenedAt = DateTime.UtcNow,
            StrategyName = "BreakEven",
        };

        // Act
        var unrealizedPnL = position.UnrealizedPnL;
        var unrealizedPnLPercent = position.UnrealizedPnLPercent;

        // Assert
        unrealizedPnL.ShouldBe(0m);
        unrealizedPnLPercent.ShouldBe(0m);
    }
}
