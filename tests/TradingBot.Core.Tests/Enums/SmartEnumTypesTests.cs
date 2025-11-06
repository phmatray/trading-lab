// <copyright file="SmartEnumTypesTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Shouldly;
using TradingBot.Core.Enums;

namespace TradingBot.Core.Tests.Enums;

/// <summary>
/// Unit tests for all SmartEnum type implementations.
/// </summary>
public sealed class SmartEnumTypesTests
{
    [Theory]
    [InlineData(0, "Buy")]
    [InlineData(1, "Sell")]
    public void OrderSide_ShouldHaveCorrectValues(int value, string name)
    {
        // Act
        var enumValue = OrderSide.FromValue(value);

        // Assert
        enumValue.Value.ShouldBe(value);
        enumValue.Name.ShouldBe(name);
    }

    [Fact]
    public void OrderSide_GetAll_ShouldReturnAllValues()
    {
        // Act
        var all = OrderSide.GetAll().ToList();

        // Assert
        all.Count.ShouldBe(2);
        all.ShouldContain(OrderSide.Buy);
        all.ShouldContain(OrderSide.Sell);
    }

    [Fact]
    public void OrderSide_FromName_ShouldBeAccessible()
    {
        // Act
        var buy = OrderSide.FromName("Buy");
        var sell = OrderSide.FromName("sell");

        // Assert
        buy.ShouldBe(OrderSide.Buy);
        sell.ShouldBe(OrderSide.Sell);
    }

    [Theory]
    [InlineData(0, "Pending")]
    [InlineData(1, "Submitted")]
    [InlineData(2, "PartiallyFilled")]
    [InlineData(3, "Filled")]
    [InlineData(4, "Cancelled")]
    [InlineData(5, "Rejected")]
    [InlineData(6, "Expired")]
    public void OrderStatus_ShouldHaveCorrectValues(int value, string name)
    {
        // Act
        var enumValue = OrderStatus.FromValue(value);

        // Assert
        enumValue.Value.ShouldBe(value);
        enumValue.Name.ShouldBe(name);
    }

    [Fact]
    public void OrderStatus_GetAll_ShouldReturnAllValues()
    {
        // Act
        var all = OrderStatus.GetAll().ToList();

        // Assert
        all.Count.ShouldBe(7);
        all.ShouldContain(OrderStatus.Pending);
        all.ShouldContain(OrderStatus.Submitted);
        all.ShouldContain(OrderStatus.PartiallyFilled);
        all.ShouldContain(OrderStatus.Filled);
        all.ShouldContain(OrderStatus.Cancelled);
        all.ShouldContain(OrderStatus.Rejected);
        all.ShouldContain(OrderStatus.Expired);
    }

    [Fact]
    public void OrderStatus_StaticValues_ShouldBeAccessible()
    {
        // Assert
        OrderStatus.Pending.Value.ShouldBe(0);
        OrderStatus.Submitted.Value.ShouldBe(1);
        OrderStatus.PartiallyFilled.Value.ShouldBe(2);
        OrderStatus.Filled.Value.ShouldBe(3);
        OrderStatus.Cancelled.Value.ShouldBe(4);
        OrderStatus.Rejected.Value.ShouldBe(5);
        OrderStatus.Expired.Value.ShouldBe(6);
    }

    [Theory]
    [InlineData(0, "Market")]
    [InlineData(1, "Limit")]
    [InlineData(2, "StopLoss")]
    [InlineData(3, "TakeProfit")]
    [InlineData(4, "TrailingStop")]
    public void OrderType_ShouldHaveCorrectValues(int value, string name)
    {
        // Act
        var enumValue = OrderType.FromValue(value);

        // Assert
        enumValue.Value.ShouldBe(value);
        enumValue.Name.ShouldBe(name);
    }

    [Fact]
    public void OrderType_GetAll_ShouldReturnAllValues()
    {
        // Act
        var all = OrderType.GetAll().ToList();

        // Assert
        all.Count.ShouldBe(5);
        all.ShouldContain(OrderType.Market);
        all.ShouldContain(OrderType.Limit);
        all.ShouldContain(OrderType.StopLoss);
        all.ShouldContain(OrderType.TakeProfit);
        all.ShouldContain(OrderType.TrailingStop);
    }

    [Fact]
    public void OrderType_StaticValues_ShouldBeAccessible()
    {
        // Assert
        OrderType.Market.Value.ShouldBe(0);
        OrderType.Limit.Value.ShouldBe(1);
        OrderType.StopLoss.Value.ShouldBe(2);
        OrderType.TakeProfit.Value.ShouldBe(3);
        OrderType.TrailingStop.Value.ShouldBe(4);
    }

    [Theory]
    [InlineData(0, "Buy")]
    [InlineData(1, "Sell")]
    [InlineData(2, "Hold")]
    [InlineData(3, "Close")]
    public void SignalType_ShouldHaveCorrectValues(int value, string name)
    {
        // Act
        var enumValue = SignalType.FromValue(value);

        // Assert
        enumValue.Value.ShouldBe(value);
        enumValue.Name.ShouldBe(name);
    }

    [Fact]
    public void SignalType_GetAll_ShouldReturnAllValues()
    {
        // Act
        var all = SignalType.GetAll().ToList();

        // Assert
        all.Count.ShouldBe(4);
        all.ShouldContain(SignalType.Buy);
        all.ShouldContain(SignalType.Sell);
        all.ShouldContain(SignalType.Hold);
        all.ShouldContain(SignalType.Close);
    }

    [Fact]
    public void SignalType_StaticValues_ShouldBeAccessible()
    {
        // Assert
        SignalType.Buy.Value.ShouldBe(0);
        SignalType.Sell.Value.ShouldBe(1);
        SignalType.Hold.Value.ShouldBe(2);
        SignalType.Close.Value.ShouldBe(3);
    }

    [Fact]
    public void SignalType_Comparison_ShouldWorkCorrectly()
    {
        // Arrange
        var buy = SignalType.Buy;
        var sell = SignalType.Sell;
        var hold = SignalType.Hold;
        var close = SignalType.Close;

        // Act & Assert
        (buy < sell).ShouldBeTrue();
        (hold > sell).ShouldBeTrue();
        (close >= SignalType.FromValue(3)).ShouldBeTrue();
        (buy == SignalType.FromValue(0)).ShouldBeTrue();
    }

    [Fact]
    public void OrderSide_ToString_ShouldReturnName()
    {
        // Act & Assert
        OrderSide.Buy.ToString().ShouldBe("Buy");
        OrderSide.Sell.ToString().ShouldBe("Sell");
    }

    [Fact]
    public void OrderStatus_ToString_ShouldReturnName()
    {
        // Act & Assert
        OrderStatus.Pending.ToString().ShouldBe("Pending");
        OrderStatus.Filled.ToString().ShouldBe("Filled");
    }

    [Fact]
    public void OrderType_ToString_ShouldReturnName()
    {
        // Act & Assert
        OrderType.Market.ToString().ShouldBe("Market");
        OrderType.Limit.ToString().ShouldBe("Limit");
    }

    [Fact]
    public void SignalType_ToString_ShouldReturnName()
    {
        // Act & Assert
        SignalType.Buy.ToString().ShouldBe("Buy");
        SignalType.Hold.ToString().ShouldBe("Hold");
    }

    [Fact]
    public void OrderSide_ImplicitConversion_ShouldWork()
    {
        // Act
        int buyValue = OrderSide.Buy;
        int sellValue = OrderSide.Sell;

        // Assert
        buyValue.ShouldBe(0);
        sellValue.ShouldBe(1);
    }

    [Fact]
    public void OrderStatus_Equality_ShouldWorkCorrectly()
    {
        // Arrange
        var status1 = OrderStatus.Filled;
        var status2 = OrderStatus.FromValue(3);
        var status3 = OrderStatus.Pending;

        // Act & Assert
        (status1 == status2).ShouldBeTrue();
        (status1 != status3).ShouldBeTrue();
        status1.Equals(status2).ShouldBeTrue();
        status1.Equals(status3).ShouldBeFalse();
    }
}
