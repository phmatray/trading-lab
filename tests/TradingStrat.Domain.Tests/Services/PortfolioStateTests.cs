using Shouldly;
using TradingStrat.Domain.Services;

namespace TradingStrat.Domain.Tests.Services;

public class PortfolioStateTests
{
    #region GetEquity Tests (5 tests)

    [Fact]
    public void GetEquity_WithCashOnly_ReturnsCash()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 10000m,
            Position = 0,
            AverageEntryPrice = 0,
            TotalCommissionPaid = 0
        };

        // Act
        decimal equity = portfolio.GetEquity(currentPrice: 100m);

        // Assert
        equity.ShouldBe(10000m);
    }

    [Fact]
    public void GetEquity_WithPosition_ReturnsCashPlusPositionValue()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 5000m,
            Position = 50,
            AverageEntryPrice = 100m,
            TotalCommissionPaid = 10m
        };

        // Act
        decimal equity = portfolio.GetEquity(currentPrice: 110m);

        // Assert
        // Equity = Cash + (Position * CurrentPrice)
        // Equity = 5000 + (50 * 110) = 5000 + 5500 = 10500
        equity.ShouldBe(10500m);
    }

    [Fact]
    public void GetEquity_WithZeroPosition_ReturnsCash()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 8000m,
            Position = 0,
            AverageEntryPrice = 0,
            TotalCommissionPaid = 50m
        };

        // Act
        decimal equity = portfolio.GetEquity(currentPrice: 150m);

        // Assert
        equity.ShouldBe(8000m); // Only cash, no position
    }

    [Fact]
    public void GetEquity_WithNegativePrice_HandlesCorrectly()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 10000m,
            Position = 100,
            AverageEntryPrice = 50m,
            TotalCommissionPaid = 5m
        };

        // Act - Negative price scenario (unusual but should handle)
        decimal equity = portfolio.GetEquity(currentPrice: -10m);

        // Assert
        equity.ShouldBe(9000m); // 10000 + (100 * -10) = 9000
    }

    [Fact]
    public void GetEquity_AfterMultipleTrades_CalculatesCorrectly()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 10000m,
            Position = 0,
            AverageEntryPrice = 0,
            TotalCommissionPaid = 0
        };

        // Execute multiple trades
        portfolio.ExecuteBuy(10, 100m, 1m);  // Cash: 9000, Position: 10
        portfolio.ExecuteBuy(5, 110m, 1m);   // Cash: 8449, Position: 15
        portfolio.ExecuteSell(5, 120m, 1m);  // Cash: 9048, Position: 10

        // Act
        decimal equity = portfolio.GetEquity(currentPrice: 115m);

        // Assert
        // Equity = 9047 + (10 * 115) = 9047 + 1150 = 10197
        equity.ShouldBe(10197m);
    }

    #endregion

    #region ExecuteBuy Tests (10 tests)

    [Fact]
    public void ExecuteBuy_FirstBuy_SetsAverageEntryPriceToPrice()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 10000m,
            Position = 0,
            AverageEntryPrice = 0,
            TotalCommissionPaid = 0
        };

        // Act
        portfolio.ExecuteBuy(quantity: 10, price: 100m, commission: 1m);

        // Assert
        portfolio.AverageEntryPrice.ShouldBe(100m);
        portfolio.Position.ShouldBe(10);
    }

    [Fact]
    public void ExecuteBuy_SecondBuy_CalculatesWeightedAverageEntryPrice()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 10000m,
            Position = 10,
            AverageEntryPrice = 100m,
            TotalCommissionPaid = 1m
        };

        // Act
        portfolio.ExecuteBuy(quantity: 10, price: 110m, commission: 1m);

        // Assert
        // Average = (100*10 + 110*10) / (10+10) = 2100 / 20 = 105
        portfolio.AverageEntryPrice.ShouldBe(105m);
        portfolio.Position.ShouldBe(20);
    }

    [Fact]
    public void ExecuteBuy_DeductsCashByQuantityTimesPricePlusCommission()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 10000m,
            Position = 0,
            AverageEntryPrice = 0,
            TotalCommissionPaid = 0
        };

        // Act
        portfolio.ExecuteBuy(quantity: 10, price: 100m, commission: 5m);

        // Assert
        // Cash = 10000 - (10 * 100) - 5 = 10000 - 1005 = 8995
        portfolio.Cash.ShouldBe(8995m);
    }

    [Fact]
    public void ExecuteBuy_IncreasesPositionByQuantity()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 10000m,
            Position = 5,
            AverageEntryPrice = 95m,
            TotalCommissionPaid = 2m
        };

        // Act
        portfolio.ExecuteBuy(quantity: 15, price: 100m, commission: 3m);

        // Assert
        portfolio.Position.ShouldBe(20); // 5 + 15
    }

    [Fact]
    public void ExecuteBuy_IncrementsCommissionPaid()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 10000m,
            Position = 0,
            AverageEntryPrice = 0,
            TotalCommissionPaid = 10m
        };

        // Act
        portfolio.ExecuteBuy(quantity: 10, price: 100m, commission: 5m);

        // Assert
        portfolio.TotalCommissionPaid.ShouldBe(15m); // 10 + 5
    }

    [Fact]
    public void ExecuteBuy_WithDifferentPrices_AveragesPricesCorrectly()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 20000m,
            Position = 0,
            AverageEntryPrice = 0,
            TotalCommissionPaid = 0
        };

        // Act
        portfolio.ExecuteBuy(10, 100m, 1m);  // Position: 10, Avg: 100
        portfolio.ExecuteBuy(20, 110m, 1m);  // Position: 30, Avg: ?

        // Assert
        // Average = (100*10 + 110*20) / 30 = (1000 + 2200) / 30 = 3200 / 30 = 106.67
        portfolio.AverageEntryPrice.ShouldBe(106.666666666666666666666666667m, 0.01m);
        portfolio.Position.ShouldBe(30);
    }

    [Fact]
    public void ExecuteBuy_WithZeroPosition_ResetsAverageEntryPrice()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 10000m,
            Position = 0,
            AverageEntryPrice = 150m, // Old value from previous trades
            TotalCommissionPaid = 50m
        };

        // Act
        portfolio.ExecuteBuy(quantity: 10, price: 100m, commission: 1m);

        // Assert
        portfolio.AverageEntryPrice.ShouldBe(100m); // Should be reset to new price
    }

    [Fact]
    public void ExecuteBuy_MultipleOrders_MaintainsCorrectAverage()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 30000m,
            Position = 0,
            AverageEntryPrice = 0,
            TotalCommissionPaid = 0
        };

        // Act - Execute 3 buy orders at different prices
        portfolio.ExecuteBuy(10, 100m, 1m);  // Avg: 100
        portfolio.ExecuteBuy(20, 120m, 1m);  // Avg: 113.33
        portfolio.ExecuteBuy(30, 90m, 1m);   // Avg: ?

        // Assert
        // Average = (100*10 + 120*20 + 90*30) / 60 = (1000 + 2400 + 2700) / 60 = 6100 / 60 = 101.67
        portfolio.AverageEntryPrice.ShouldBe(101.666666666666666666666666667m, 0.01m);
        portfolio.Position.ShouldBe(60);
    }

    [Fact]
    public void ExecuteBuy_ReducesCashCorrectly()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 15000m,
            Position = 0,
            AverageEntryPrice = 0,
            TotalCommissionPaid = 0
        };

        // Act
        portfolio.ExecuteBuy(quantity: 25, price: 200m, commission: 10m);

        // Assert
        // Cash = 15000 - (25 * 200) - 10 = 15000 - 5010 = 9990
        portfolio.Cash.ShouldBe(9990m);
    }

    [Fact]
    public void ExecuteBuy_WithHighCommission_AccountsForIt()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 10000m,
            Position = 0,
            AverageEntryPrice = 0,
            TotalCommissionPaid = 0
        };

        // Act
        portfolio.ExecuteBuy(quantity: 10, price: 100m, commission: 100m); // High commission

        // Assert
        portfolio.Cash.ShouldBe(8900m); // 10000 - 1000 - 100
        portfolio.TotalCommissionPaid.ShouldBe(100m);
    }

    #endregion

    #region ExecuteSell Tests (10 tests)

    [Fact]
    public void ExecuteSell_IncreasesCashByQuantityTimesPriceMinusCommission()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 5000m,
            Position = 20,
            AverageEntryPrice = 100m,
            TotalCommissionPaid = 5m
        };

        // Act
        portfolio.ExecuteSell(quantity: 10, price: 110m, commission: 2m);

        // Assert
        // Cash = 5000 + (10 * 110) - 2 = 5000 + 1100 - 2 = 6098
        portfolio.Cash.ShouldBe(6098m);
    }

    [Fact]
    public void ExecuteSell_DecreasesPositionByQuantity()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 5000m,
            Position = 20,
            AverageEntryPrice = 100m,
            TotalCommissionPaid = 5m
        };

        // Act
        portfolio.ExecuteSell(quantity: 10, price: 110m, commission: 2m);

        // Assert
        portfolio.Position.ShouldBe(10); // 20 - 10
    }

    [Fact]
    public void ExecuteSell_IncrementsCommissionPaid()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 5000m,
            Position = 20,
            AverageEntryPrice = 100m,
            TotalCommissionPaid = 10m
        };

        // Act
        portfolio.ExecuteSell(quantity: 10, price: 110m, commission: 3m);

        // Assert
        portfolio.TotalCommissionPaid.ShouldBe(13m); // 10 + 3
    }

    [Fact]
    public void ExecuteSell_SellAllShares_ResetsAverageEntryPriceToZero()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 5000m,
            Position = 20,
            AverageEntryPrice = 100m,
            TotalCommissionPaid = 5m
        };

        // Act
        portfolio.ExecuteSell(quantity: 20, price: 110m, commission: 2m); // Sell all

        // Assert
        portfolio.Position.ShouldBe(0);
        portfolio.AverageEntryPrice.ShouldBe(0m); // Reset when position = 0
    }

    [Fact]
    public void ExecuteSell_PartialSell_MaintainsAverageEntryPrice()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 5000m,
            Position = 20,
            AverageEntryPrice = 105m,
            TotalCommissionPaid = 5m
        };

        // Act
        portfolio.ExecuteSell(quantity: 10, price: 110m, commission: 2m); // Partial sell

        // Assert
        portfolio.Position.ShouldBe(10);
        portfolio.AverageEntryPrice.ShouldBe(105m); // Unchanged for partial sell
    }

    [Fact]
    public void ExecuteSell_WithProfit_IncreasesEquity()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 5000m,
            Position = 10,
            AverageEntryPrice = 100m,
            TotalCommissionPaid = 2m
        };

        decimal equityBefore = portfolio.GetEquity(currentPrice: 110m);

        // Act
        portfolio.ExecuteSell(quantity: 10, price: 110m, commission: 1m);

        decimal equityAfter = portfolio.GetEquity(currentPrice: 110m);

        // Assert
        // Before: 5000 + (10 * 110) = 6100
        // After: 6099 + 0 = 6099 (sold all shares at profit, minus commission)
        equityBefore.ShouldBe(6100m);
        equityAfter.ShouldBe(6099m); // Slight decrease due to commission
        portfolio.Position.ShouldBe(0);
    }

    [Fact]
    public void ExecuteSell_WithLoss_DecreasesEquity()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 5000m,
            Position = 10,
            AverageEntryPrice = 100m,
            TotalCommissionPaid = 2m
        };

        decimal equityBefore = portfolio.GetEquity(currentPrice: 90m);

        // Act
        portfolio.ExecuteSell(quantity: 10, price: 90m, commission: 1m);

        decimal equityAfter = portfolio.GetEquity(currentPrice: 90m);

        // Assert
        // Before: 5000 + (10 * 90) = 5900
        // After: 5899 + 0 = 5899 (sold all shares at loss, minus commission)
        equityBefore.ShouldBe(5900m);
        equityAfter.ShouldBe(5899m);
        portfolio.Position.ShouldBe(0);
    }

    [Fact]
    public void ExecuteSell_MultiplePartialSells_ReducesPositionCorrectly()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 5000m,
            Position = 50,
            AverageEntryPrice = 100m,
            TotalCommissionPaid = 5m
        };

        // Act - Multiple partial sells
        portfolio.ExecuteSell(10, 105m, 1m); // Position: 40
        portfolio.ExecuteSell(15, 110m, 1m); // Position: 25
        portfolio.ExecuteSell(10, 108m, 1m); // Position: 15

        // Assert
        portfolio.Position.ShouldBe(15); // 50 - 10 - 15 - 10 = 15
        portfolio.AverageEntryPrice.ShouldBe(100m); // Unchanged
    }

    [Fact]
    public void ExecuteSell_WithCommission_ReducesProceedsCorrectly()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 5000m,
            Position = 20,
            AverageEntryPrice = 100m,
            TotalCommissionPaid = 5m
        };

        // Act
        portfolio.ExecuteSell(quantity: 10, price: 120m, commission: 20m); // High commission

        // Assert
        // Cash = 5000 + (10 * 120) - 20 = 5000 + 1200 - 20 = 6180
        portfolio.Cash.ShouldBe(6180m);
        portfolio.TotalCommissionPaid.ShouldBe(25m); // 5 + 20
    }

    [Fact]
    public void ExecuteSell_ToZeroPosition_ClearsAverageEntryPrice()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 8000m,
            Position = 15,
            AverageEntryPrice = 125m,
            TotalCommissionPaid = 10m
        };

        // Act
        portfolio.ExecuteSell(quantity: 15, price: 130m, commission: 3m);

        // Assert
        portfolio.Position.ShouldBe(0);
        portfolio.AverageEntryPrice.ShouldBe(0m); // Cleared
        portfolio.Cash.ShouldBe(9947m); // 8000 + (15 * 130) - 3
    }

    #endregion

    #region Integration Tests (5 tests)

    [Fact]
    public void Portfolio_BuyAndSellCycle_MaintainsCorrectState()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 10000m,
            Position = 0,
            AverageEntryPrice = 0,
            TotalCommissionPaid = 0
        };

        // Act - Complete buy/sell cycle
        portfolio.ExecuteBuy(10, 100m, 1m);      // Buy 10 @ 100
        portfolio.ExecuteBuy(10, 110m, 1m);      // Buy 10 @ 110
        portfolio.ExecuteSell(15, 120m, 1m);     // Sell 15 @ 120
        portfolio.ExecuteSell(5, 115m, 1m);      // Sell 5 @ 115

        // Assert
        portfolio.Position.ShouldBe(0);
        portfolio.AverageEntryPrice.ShouldBe(0m);
        portfolio.TotalCommissionPaid.ShouldBe(4m); // 1+1+1+1
        // Cash calculation: 10000 - 1001 - 1101 + 1799 + 574 = 10271
        portfolio.Cash.ShouldBe(10271m);
    }

    [Fact]
    public void Portfolio_MultipleBuysAtDifferentPrices_CalculatesCorrectAverage()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 50000m,
            Position = 0,
            AverageEntryPrice = 0,
            TotalCommissionPaid = 0
        };

        // Act
        portfolio.ExecuteBuy(50, 100m, 5m);   // 50 @ 100
        portfolio.ExecuteBuy(30, 120m, 3m);   // 30 @ 120
        portfolio.ExecuteBuy(20, 80m, 2m);    // 20 @ 80

        // Assert
        portfolio.Position.ShouldBe(100);
        // Average = (50*100 + 30*120 + 20*80) / 100 = (5000 + 3600 + 1600) / 100 = 102
        portfolio.AverageEntryPrice.ShouldBe(102m);
    }

    [Fact]
    public void Portfolio_BuyLowSellHigh_GeneratesProfit()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 10000m,
            Position = 0,
            AverageEntryPrice = 0,
            TotalCommissionPaid = 0
        };

        // Act
        portfolio.ExecuteBuy(100, 90m, 10m);    // Buy 100 @ 90
        decimal equityAtPurchase = portfolio.GetEquity(90m);

        portfolio.ExecuteSell(100, 110m, 10m);  // Sell 100 @ 110
        decimal finalCash = portfolio.Cash;

        // Assert
        equityAtPurchase.ShouldBe(9990m); // 10000 - 10 (commission) = 9990 equity
        finalCash.ShouldBe(11980m); // (10000 - 9010) + 10990 = 990 + 10990 = 11980
        portfolio.Position.ShouldBe(0);
        portfolio.TotalCommissionPaid.ShouldBe(20m);
    }

    [Fact]
    public void Portfolio_BuyHighSellLow_GeneratesLoss()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 10000m,
            Position = 0,
            AverageEntryPrice = 0,
            TotalCommissionPaid = 0
        };

        // Act
        portfolio.ExecuteBuy(100, 110m, 10m);   // Buy 100 @ 110
        decimal equityAtPurchase = portfolio.GetEquity(110m);

        portfolio.ExecuteSell(100, 90m, 10m);   // Sell 100 @ 90
        decimal finalCash = portfolio.Cash;

        // Assert
        equityAtPurchase.ShouldBe(9990m); // 10000 - 10 (commission) = 9990 equity
        finalCash.ShouldBe(7980m); // 10000 - 11010 + 8990 = 7980
        portfolio.Position.ShouldBe(0);
        portfolio.TotalCommissionPaid.ShouldBe(20m);
    }

    [Fact]
    public void Portfolio_PartialPositions_TracksCorrectly()
    {
        // Arrange
        PortfolioState portfolio = new()
        {
            Cash = 20000m,
            Position = 0,
            AverageEntryPrice = 0,
            TotalCommissionPaid = 0
        };

        // Act - Buy in stages, sell in stages
        portfolio.ExecuteBuy(100, 100m, 5m);    // Position: 100
        portfolio.ExecuteSell(30, 110m, 2m);    // Position: 70
        portfolio.ExecuteBuy(50, 95m, 3m);      // Position: 120
        portfolio.ExecuteSell(120, 105m, 6m);   // Position: 0

        // Assert
        portfolio.Position.ShouldBe(0);
        portfolio.AverageEntryPrice.ShouldBe(0m);
        portfolio.TotalCommissionPaid.ShouldBe(16m); // 5+2+3+6
        // Cash: 20000 - 10005 + 3298 - 4753 + 12594 = 21134
        portfolio.Cash.ShouldBe(21134m);
    }

    #endregion
}
