// <copyright file="TransactionCostSimulatorTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Enums;
using TradingBot.Core.Models.Backtest;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Analytics.Tests;

/// <summary>
/// Unit tests for TransactionCostSimulator.
/// </summary>
public sealed class TransactionCostSimulatorTests
{
    [Fact]
    public void CalculateCommission_WithPerTradeOnly_ShouldReturnPerTradeFee()
    {
        // Arrange
        var costModel = new TransactionCostModel
        {
            Enabled = true,
            CommissionPerTrade = 5m,
            CommissionPerShare = 0m,
        };
        var simulator = new TransactionCostSimulator(costModel);
        var order = CreateOrder(OrderSide.Buy, 100);

        // Act
        var commission = simulator.CalculateCommission(order);

        // Assert
        commission.ShouldBe(5m);
    }

    [Fact]
    public void CalculateCommission_WithPerShareOnly_ShouldReturnPerShareFee()
    {
        // Arrange
        var costModel = new TransactionCostModel
        {
            Enabled = true,
            CommissionPerTrade = 0m,
            CommissionPerShare = 0.01m,
        };
        var simulator = new TransactionCostSimulator(costModel);
        var order = CreateOrder(OrderSide.Buy, 100);

        // Act
        var commission = simulator.CalculateCommission(order);

        // Assert
        commission.ShouldBe(1m); // 100 shares * $0.01 = $1
    }

    [Fact]
    public void CalculateCommission_WithBothFees_ShouldReturnSum()
    {
        // Arrange
        var costModel = new TransactionCostModel
        {
            Enabled = true,
            CommissionPerTrade = 2m,
            CommissionPerShare = 0.005m,
        };
        var simulator = new TransactionCostSimulator(costModel);
        var order = CreateOrder(OrderSide.Buy, 200);

        // Act
        var commission = simulator.CalculateCommission(order);

        // Assert
        commission.ShouldBe(3m); // $2 + (200 * $0.005) = $3
    }

    [Fact]
    public void CalculateCommission_WhenDisabled_ShouldReturnZero()
    {
        // Arrange
        var costModel = new TransactionCostModel
        {
            Enabled = false,
            CommissionPerTrade = 10m,
            CommissionPerShare = 0.05m,
        };
        var simulator = new TransactionCostSimulator(costModel);
        var order = CreateOrder(OrderSide.Buy, 100);

        // Act
        var commission = simulator.CalculateCommission(order);

        // Assert
        commission.ShouldBe(0m);
    }

    [Fact]
    public void CalculateSlippage_ForBuyOrder_ShouldReturnPositiveSlippage()
    {
        // Arrange
        var costModel = new TransactionCostModel
        {
            Enabled = true,
            SlippagePercent = 0.1m,
        };
        var simulator = new TransactionCostSimulator(costModel);
        var order = CreateOrder(OrderSide.Buy, 10);
        var executionPrice = 100m;

        // Act
        var slippage = simulator.CalculateSlippage(order, executionPrice);

        // Assert
        // Buy slippage increases cost: 100 * 10 * 0.001 = 1
        slippage.ShouldBe(1m);
    }

    [Fact]
    public void CalculateSlippage_ForSellOrder_ShouldReturnNegativeSlippage()
    {
        // Arrange
        var costModel = new TransactionCostModel
        {
            Enabled = true,
            SlippagePercent = 0.1m,
        };
        var simulator = new TransactionCostSimulator(costModel);
        var order = CreateOrder(OrderSide.Sell, 10);
        var executionPrice = 100m;

        // Act
        var slippage = simulator.CalculateSlippage(order, executionPrice);

        // Assert
        // Sell slippage reduces proceeds: -(100 * 10 * 0.001) = -1
        slippage.ShouldBe(-1m);
    }

    [Fact]
    public void CalculateSlippage_WhenDisabled_ShouldReturnZero()
    {
        // Arrange
        var costModel = new TransactionCostModel
        {
            Enabled = false,
            SlippagePercent = 1m,
        };
        var simulator = new TransactionCostSimulator(costModel);
        var order = CreateOrder(OrderSide.Buy, 100);

        // Act
        var slippage = simulator.CalculateSlippage(order, 100m);

        // Assert
        slippage.ShouldBe(0m);
    }

    [Fact]
    public void CalculateSpread_ShouldReturnHalfSpreadCost()
    {
        // Arrange
        var costModel = new TransactionCostModel
        {
            Enabled = true,
            SpreadPercent = 0.1m,
        };
        var simulator = new TransactionCostSimulator(costModel);
        var order = CreateOrder(OrderSide.Buy, 10);
        var midPrice = 100m;

        // Act
        var spreadCost = simulator.CalculateSpread(order, midPrice);

        // Assert
        // Spread cost: (100 * 10 * 0.001) / 2 = 0.5
        spreadCost.ShouldBe(0.5m);
    }

    [Fact]
    public void CalculateSpread_WhenDisabled_ShouldReturnZero()
    {
        // Arrange
        var costModel = new TransactionCostModel
        {
            Enabled = false,
            SpreadPercent = 0.5m,
        };
        var simulator = new TransactionCostSimulator(costModel);
        var order = CreateOrder(OrderSide.Buy, 100);

        // Act
        var spreadCost = simulator.CalculateSpread(order, 100m);

        // Assert
        spreadCost.ShouldBe(0m);
    }

    [Fact]
    public void CalculateTotalCost_ShouldSumAllCosts()
    {
        // Arrange
        var costModel = new TransactionCostModel
        {
            Enabled = true,
            CommissionPerTrade = 1m,
            CommissionPerShare = 0.01m,
            SlippagePercent = 0.1m,
            SpreadPercent = 0.05m,
        };
        var simulator = new TransactionCostSimulator(costModel);
        var order = CreateOrder(OrderSide.Buy, 100);
        var fillPrice = 50m;

        // Act
        var totalCost = simulator.CalculateTotalCost(order, fillPrice);

        // Assert
        // Commission: 1 + (100 * 0.01) = 2
        // Slippage: |50 * 100 * 0.001| = 5
        // Spread: (50 * 100 * 0.0005) / 2 = 1.25
        // Total: 2 + 5 + 1.25 = 8.25
        totalCost.ShouldBe(8.25m);
    }

    [Fact]
    public void CalculateTotalCost_WhenDisabled_ShouldReturnZero()
    {
        // Arrange
        var costModel = new TransactionCostModel
        {
            Enabled = false,
            CommissionPerTrade = 10m,
            SlippagePercent = 1m,
            SpreadPercent = 0.5m,
        };
        var simulator = new TransactionCostSimulator(costModel);
        var order = CreateOrder(OrderSide.Buy, 100);

        // Act
        var totalCost = simulator.CalculateTotalCost(order, 100m);

        // Assert
        totalCost.ShouldBe(0m);
    }

    [Fact]
    public void ApplySlippage_ForBuyOrder_ShouldIncreasePrice()
    {
        // Arrange
        var costModel = new TransactionCostModel
        {
            Enabled = true,
            SlippagePercent = 0.1m,
        };
        var simulator = new TransactionCostSimulator(costModel);
        var order = CreateOrder(OrderSide.Buy, 100);
        var basePrice = 100m;

        // Act
        var priceWithSlippage = simulator.ApplySlippage(order, basePrice);

        // Assert
        priceWithSlippage.ShouldBe(100.1m); // 100 * 1.001 = 100.1
    }

    [Fact]
    public void ApplySlippage_ForSellOrder_ShouldDecreasePrice()
    {
        // Arrange
        var costModel = new TransactionCostModel
        {
            Enabled = true,
            SlippagePercent = 0.1m,
        };
        var simulator = new TransactionCostSimulator(costModel);
        var order = CreateOrder(OrderSide.Sell, 100);
        var basePrice = 100m;

        // Act
        var priceWithSlippage = simulator.ApplySlippage(order, basePrice);

        // Assert
        priceWithSlippage.ShouldBe(99.9m); // 100 * 0.999 = 99.9
    }

    [Fact]
    public void ApplySlippage_WhenDisabled_ShouldReturnBasePrice()
    {
        // Arrange
        var costModel = new TransactionCostModel
        {
            Enabled = false,
            SlippagePercent = 5m,
        };
        var simulator = new TransactionCostSimulator(costModel);
        var order = CreateOrder(OrderSide.Buy, 100);

        // Act
        var priceWithSlippage = simulator.ApplySlippage(order, 100m);

        // Assert
        priceWithSlippage.ShouldBe(100m);
    }

    [Fact]
    public void GetCostBreakdown_ShouldReturnAllComponents()
    {
        // Arrange
        var costModel = new TransactionCostModel
        {
            Enabled = true,
            CommissionPerTrade = 2m,
            CommissionPerShare = 0.01m,
            SlippagePercent = 0.1m,
            SpreadPercent = 0.05m,
        };
        var simulator = new TransactionCostSimulator(costModel);
        var order = CreateOrder(OrderSide.Buy, 100);
        var fillPrice = 100m;

        // Act
        var breakdown = simulator.GetCostBreakdown(order, fillPrice);

        // Assert
        breakdown.ShouldNotBeNull();
        breakdown.Commission.ShouldBe(3m); // 2 + (100 * 0.01)
        breakdown.Slippage.ShouldBe(10m); // |100 * 100 * 0.001|
        breakdown.Spread.ShouldBe(2.5m); // (100 * 100 * 0.0005) / 2
        breakdown.TotalCost.ShouldBe(15.5m); // 3 + 10 + 2.5
    }

    [Fact]
    public void Constructor_WithNullCostModel_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new TransactionCostSimulator(null!));
    }

    [Fact]
    public void CalculateTotalCost_WithLargeOrder_ShouldCalculateCorrectly()
    {
        // Arrange
        var costModel = new TransactionCostModel
        {
            Enabled = true,
            CommissionPerTrade = 5m,
            CommissionPerShare = 0.005m,
            SlippagePercent = 0.05m,
            SpreadPercent = 0.02m,
        };
        var simulator = new TransactionCostSimulator(costModel);
        var order = CreateOrder(OrderSide.Buy, 10000); // Large order
        var fillPrice = 250m;

        // Act
        var totalCost = simulator.CalculateTotalCost(order, fillPrice);

        // Assert
        // Commission: 5 + (10000 * 0.005) = 55
        // Slippage: |250 * 10000 * 0.0005| = 1250
        // Spread: (250 * 10000 * 0.0002) / 2 = 250
        // Total: 55 + 1250 + 250 = 1555
        totalCost.ShouldBe(1555m);
    }

    [Fact]
    public void CalculateSlippage_WithZeroQuantity_ShouldReturnZero()
    {
        // Arrange
        var costModel = new TransactionCostModel
        {
            Enabled = true,
            SlippagePercent = 0.1m,
        };
        var simulator = new TransactionCostSimulator(costModel);
        var order = CreateOrder(OrderSide.Buy, 0);

        // Act
        var slippage = simulator.CalculateSlippage(order, 100m);

        // Assert
        slippage.ShouldBe(0m);
    }

    // Helper methods
    private Order CreateOrder(OrderSide side, decimal quantity)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            Symbol = "SPY",
            Type = OrderType.Market,
            Side = side,
            Quantity = quantity,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            StrategyName = "TestStrategy",
        };
    }
}
