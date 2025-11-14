// <copyright file="TradeTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Shouldly;
using TradingBot.Core.Enums;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Core.Tests.Models.Trading;

/// <summary>
/// Unit tests for the Trade model.
/// </summary>
public sealed class TradeTests
{
    [Fact]
    public void Trade_WhenCreatedWithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var tradeId = Guid.NewGuid();
        var entryTime = DateTime.UtcNow.AddHours(-2);
        var exitTime = DateTime.UtcNow;

        var trade = new Trade
        {
            Id = tradeId,
            Symbol = "SPY",
            Side = OrderSide.Buy,
            Quantity = 10m,
            EntryPrice = 450.00m,
            ExitPrice = 455.00m,
            EntryTime = entryTime,
            ExitTime = exitTime,
            Commission = 2.50m,
            StrategyName = "MomentumStrategy",
        };

        // Assert
        trade.Id.ShouldBe(tradeId);
        trade.Symbol.ShouldBe("SPY");
        trade.Side.ShouldBe(OrderSide.Buy);
        trade.Quantity.ShouldBe(10m);
        trade.EntryPrice.ShouldBe(450.00m);
        trade.ExitPrice.ShouldBe(455.00m);
        trade.EntryTime.ShouldBe(entryTime);
        trade.ExitTime.ShouldBe(exitTime);
        trade.Commission.ShouldBe(2.50m);
        trade.StrategyName.ShouldBe("MomentumStrategy");
    }

    [Fact]
    public void Trade_LongTrade_WhenProfitable_ShouldHavePositiveRealizedPnL()
    {
        // Arrange
        var trade = new Trade
        {
            Id = Guid.NewGuid(),
            Symbol = "AAPL",
            Side = OrderSide.Buy, // Long trade
            Quantity = 20m,
            EntryPrice = 180.00m,
            ExitPrice = 185.00m, // Higher exit
            EntryTime = DateTime.UtcNow.AddDays(-1),
            ExitTime = DateTime.UtcNow,
            Commission = 5.00m,
            StrategyName = "TrendFollowing",
        };

        // Act
        var realizedPnL = trade.RealizedPnL;
        var realizedPnLPercent = trade.RealizedPnLPercent;
        var isWinner = trade.IsWinner;

        // Assert
        realizedPnL.ShouldBe(95m); // (185 - 180) * 20 - 5 = 100 - 5 = 95
        realizedPnLPercent.ShouldBe(2.7777777777777777777777777778m, tolerance: 0.0001m); // ((185-180)/180) * 100
        isWinner.ShouldBeTrue();
    }

    [Fact]
    public void Trade_LongTrade_WhenUnprofitable_ShouldHaveNegativeRealizedPnL()
    {
        // Arrange
        var trade = new Trade
        {
            Id = Guid.NewGuid(),
            Symbol = "TSLA",
            Side = OrderSide.Buy, // Long trade
            Quantity = 15m,
            EntryPrice = 250.00m,
            ExitPrice = 240.00m, // Lower exit (loss)
            EntryTime = DateTime.UtcNow.AddDays(-3),
            ExitTime = DateTime.UtcNow,
            Commission = 3.00m,
            StrategyName = "MomentumStrategy",
        };

        // Act
        var realizedPnL = trade.RealizedPnL;
        var realizedPnLPercent = trade.RealizedPnLPercent;
        var isWinner = trade.IsWinner;

        // Assert
        realizedPnL.ShouldBe(-153m); // (240 - 250) * 15 - 3 = -150 - 3 = -153
        realizedPnLPercent.ShouldBe(-4m); // ((240-250)/250) * 100 = -4%
        isWinner.ShouldBeFalse();
    }

    [Fact]
    public void Trade_ShortTrade_WhenProfitable_ShouldHavePositiveRealizedPnL()
    {
        // Arrange
        var trade = new Trade
        {
            Id = Guid.NewGuid(),
            Symbol = "NVDA",
            Side = OrderSide.Sell, // Short trade
            Quantity = 10m,
            EntryPrice = 500.00m,
            ExitPrice = 480.00m, // Lower exit (good for short)
            EntryTime = DateTime.UtcNow.AddDays(-2),
            ExitTime = DateTime.UtcNow,
            Commission = 4.00m,
            StrategyName = "ShortSelling",
        };

        // Act
        var realizedPnL = trade.RealizedPnL;
        var realizedPnLPercent = trade.RealizedPnLPercent;
        var isWinner = trade.IsWinner;

        // Assert
        realizedPnL.ShouldBe(196m); // (500 - 480) * 10 - 4 = 200 - 4 = 196
        realizedPnLPercent.ShouldBe(4m); // ((500-480)/500) * 100 = 4%
        isWinner.ShouldBeTrue();
    }

    [Fact]
    public void Trade_ShortTrade_WhenUnprofitable_ShouldHaveNegativeRealizedPnL()
    {
        // Arrange
        var trade = new Trade
        {
            Id = Guid.NewGuid(),
            Symbol = "AMZN",
            Side = OrderSide.Sell, // Short trade
            Quantity = 8m,
            EntryPrice = 150.00m,
            ExitPrice = 160.00m, // Higher exit (bad for short)
            EntryTime = DateTime.UtcNow.AddDays(-1),
            ExitTime = DateTime.UtcNow,
            Commission = 2.00m,
            StrategyName = "ShortSelling",
        };

        // Act
        var realizedPnL = trade.RealizedPnL;
        var realizedPnLPercent = trade.RealizedPnLPercent;
        var isWinner = trade.IsWinner;

        // Assert
        realizedPnL.ShouldBe(-82m); // (150 - 160) * 8 - 2 = -80 - 2 = -82
        realizedPnLPercent.ShouldBe(-6.6666666666666666666666666667m, tolerance: 0.0001m); // ((150-160)/150) * 100
        isWinner.ShouldBeFalse();
    }

    [Fact]
    public void Trade_Duration_ShouldBeCalculatedCorrectly()
    {
        // Arrange
        var entryTime = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var exitTime = new DateTime(2024, 1, 1, 14, 30, 0, DateTimeKind.Utc);

        var trade = new Trade
        {
            Id = Guid.NewGuid(),
            Symbol = "MSFT",
            Side = OrderSide.Buy,
            Quantity = 25m,
            EntryPrice = 350.00m,
            ExitPrice = 360.00m,
            EntryTime = entryTime,
            ExitTime = exitTime,
            Commission = 1.00m,
            StrategyName = "DayTrading",
        };

        // Act
        var duration = trade.Duration;

        // Assert
        duration.ShouldBe(TimeSpan.FromHours(4.5));
        duration.TotalHours.ShouldBe(4.5);
    }

    [Fact]
    public void Trade_WithZeroCommission_ShouldCalculatePnLWithoutCommission()
    {
        // Arrange
        var trade = new Trade
        {
            Id = Guid.NewGuid(),
            Symbol = "META",
            Side = OrderSide.Buy,
            Quantity = 12m,
            EntryPrice = 300.00m,
            ExitPrice = 310.00m,
            EntryTime = DateTime.UtcNow.AddHours(-5),
            ExitTime = DateTime.UtcNow,
            Commission = 0m, // No commission
            StrategyName = "CommissionFree",
        };

        // Act
        var realizedPnL = trade.RealizedPnL;

        // Assert
        realizedPnL.ShouldBe(120m); // (310 - 300) * 12 = 120
    }

    [Fact]
    public void Trade_WhenBreakEven_ShouldHaveZeroPnL()
    {
        // Arrange
        var trade = new Trade
        {
            Id = Guid.NewGuid(),
            Symbol = "GOOGL",
            Side = OrderSide.Buy,
            Quantity = 18m,
            EntryPrice = 140.00m,
            ExitPrice = 140.00m, // Same price
            EntryTime = DateTime.UtcNow.AddHours(-2),
            ExitTime = DateTime.UtcNow,
            Commission = 0m,
            StrategyName = "BreakEven",
        };

        // Act
        var realizedPnL = trade.RealizedPnL;
        var realizedPnLPercent = trade.RealizedPnLPercent;
        var isWinner = trade.IsWinner;

        // Assert
        realizedPnL.ShouldBe(0m);
        realizedPnLPercent.ShouldBe(0m);
        isWinner.ShouldBeFalse(); // Not profitable
    }

    [Fact]
    public void Trade_WithHighCommission_CanTurnProfitToLoss()
    {
        // Arrange
        var trade = new Trade
        {
            Id = Guid.NewGuid(),
            Symbol = "DIS",
            Side = OrderSide.Buy,
            Quantity = 10m,
            EntryPrice = 90.00m,
            ExitPrice = 92.00m, // Small profit
            EntryTime = DateTime.UtcNow.AddMinutes(-30),
            ExitTime = DateTime.UtcNow,
            Commission = 25.00m, // High commission
            StrategyName = "HighCostTrading",
        };

        // Act
        var realizedPnL = trade.RealizedPnL;
        var isWinner = trade.IsWinner;

        // Assert
        realizedPnL.ShouldBe(-5m); // (92 - 90) * 10 - 25 = 20 - 25 = -5
        isWinner.ShouldBeFalse(); // Commission turned it into a loss
    }

    [Fact]
    public void Trade_RealizedPnLPercent_ShouldNotIncludeCommission()
    {
        // Arrange
        var trade = new Trade
        {
            Id = Guid.NewGuid(),
            Symbol = "BRK.B",
            Side = OrderSide.Buy,
            Quantity = 30m,
            EntryPrice = 350.00m,
            ExitPrice = 370.00m,
            EntryTime = DateTime.UtcNow.AddDays(-7),
            ExitTime = DateTime.UtcNow,
            Commission = 10.00m,
            StrategyName = "ValueInvesting",
        };

        // Act
        var realizedPnLPercent = trade.RealizedPnLPercent;

        // Assert
        // Percentage should not include commission: ((370-350)/350) * 100
        realizedPnLPercent.ShouldBe(5.7142857142857142857142857143m, tolerance: 0.0001m);
    }

    [Fact]
    public void Trade_ShortDuration_ShouldBeMeasuredInMinutes()
    {
        // Arrange
        var entryTime = DateTime.UtcNow;
        var exitTime = entryTime.AddMinutes(15);

        var trade = new Trade
        {
            Id = Guid.NewGuid(),
            Symbol = "COIN",
            Side = OrderSide.Buy,
            Quantity = 5m,
            EntryPrice = 100.00m,
            ExitPrice = 102.00m,
            EntryTime = entryTime,
            ExitTime = exitTime,
            Commission = 0.50m,
            StrategyName = "ScalpTrading",
        };

        // Act
        var duration = trade.Duration;

        // Assert
        duration.TotalMinutes.ShouldBe(15);
        duration.ShouldBe(TimeSpan.FromMinutes(15));
    }
}
