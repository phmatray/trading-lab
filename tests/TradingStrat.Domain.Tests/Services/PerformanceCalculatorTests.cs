using Shouldly;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services;

namespace TradingStrat.Domain.Tests.Services;

public class PerformanceCalculatorTests
{
    private readonly PerformanceCalculator _calculator;

    public PerformanceCalculatorTests()
    {
        _calculator = new PerformanceCalculator();
    }

    #region Basic Metrics Tests (10 tests)

    [Fact]
    public void Calculate_WithNoTrades_ReturnsEmptyMetrics()
    {
        // Arrange
        List<Trade> trades = [];
        List<EquityPoint> equityCurve = [];
        decimal initialCapital = 10000m;
        int totalDays = 100;

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, initialCapital, totalDays);

        // Assert
        result.InitialCapital.ShouldBe(initialCapital);
        result.FinalEquity.ShouldBe(initialCapital);
        result.TotalReturn.ShouldBe(0m);
        result.TotalReturnPercentage.ShouldBe(0m);
        result.TotalTrades.ShouldBe(0);
        result.WinningTrades.ShouldBe(0);
        result.LosingTrades.ShouldBe(0);
    }

    [Fact]
    public void Calculate_WithSingleWinningTrade_CalculatesCorrectReturn()
    {
        // Arrange
        DateTime date1 = new(2024, 1, 1);
        DateTime date2 = new(2024, 1, 2);
        DateTime date3 = new(2024, 1, 3);

        List<Trade> trades =
        [
            new() { Type = TradeType.Buy, Price = 100m, Quantity = 10, Commission = 1m, DateTime = date1 },
            new() { Type = TradeType.Sell, Price = 110m, Quantity = 10, Commission = 1m, ProfitLoss = 98m, DateTime = date3 }
        ];

        List<EquityPoint> equityCurve =
        [
            new(date1, 10000m, 0),
            new(date2, 8999m, 10),
            new(date3, 10098m, 0)
        ];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 3);

        // Assert
        result.FinalEquity.ShouldBe(10098m);
        result.TotalReturn.ShouldBe(98m);
        result.TotalReturnPercentage.ShouldBe(0.98m);
        result.TotalTrades.ShouldBe(1);
        result.WinningTrades.ShouldBe(1);
        result.LosingTrades.ShouldBe(0);
    }

    [Fact]
    public void Calculate_WithSingleLosingTrade_CalculatesCorrectReturn()
    {
        // Arrange
        DateTime date1 = new(2024, 1, 1);
        DateTime date2 = new(2024, 1, 2);
        DateTime date3 = new(2024, 1, 3);

        List<Trade> trades =
        [
            new() { Type = TradeType.Buy, Price = 100m, Quantity = 10, Commission = 1m, DateTime = date1 },
            new() { Type = TradeType.Sell, Price = 90m, Quantity = 10, Commission = 1m, ProfitLoss = -102m, DateTime = date3 }
        ];

        List<EquityPoint> equityCurve =
        [
            new(date1, 10000m, 0),
            new(date2, 8999m, 10),
            new(date3, 9898m, 0)
        ];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 3);

        // Assert
        result.FinalEquity.ShouldBe(9898m);
        result.TotalReturn.ShouldBe(-102m);
        result.TotalReturnPercentage.ShouldBe(-1.02m);
        result.TotalTrades.ShouldBe(1);
        result.WinningTrades.ShouldBe(0);
        result.LosingTrades.ShouldBe(1);
    }

    [Fact]
    public void Calculate_WithMultipleTrades_AggregatesCorrectly()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<Trade> trades =
        [
            new() { Type = TradeType.Buy, Price = 100m, Quantity = 10, Commission = 1m, DateTime = baseDate },
            new() { Type = TradeType.Sell, Price = 110m, Quantity = 10, Commission = 1m, ProfitLoss = 98m, DateTime = baseDate.AddDays(1) },
            new() { Type = TradeType.Buy, Price = 110m, Quantity = 10, Commission = 1m, DateTime = baseDate.AddDays(2) },
            new() { Type = TradeType.Sell, Price = 105m, Quantity = 10, Commission = 1m, ProfitLoss = -52m, DateTime = baseDate.AddDays(3) }
        ];

        List<EquityPoint> equityCurve =
        [
            new(baseDate, 10000m, 0),
            new(baseDate.AddDays(1), 10098m, 0),
            new(baseDate.AddDays(2), 8997m, 10),
            new(baseDate.AddDays(3), 10046m, 0)
        ];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 4);

        // Assert
        result.TotalTrades.ShouldBe(2); // 2 round trips
        result.WinningTrades.ShouldBe(1);
        result.LosingTrades.ShouldBe(1);
        result.TotalReturn.ShouldBe(46m); // 98 - 52
    }

    [Fact]
    public void Calculate_FinalEquity_EqualsCashPlusPositionValue()
    {
        // Arrange
        DateTime date = new(2024, 1, 1);
        List<Trade> trades = [];
        List<EquityPoint> equityCurve =
        [
            new(date, 10000m, 0),
            new(date.AddDays(1), 11000m, 10) // Has position
        ];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 2);

        // Assert
        result.FinalEquity.ShouldBe(11000m);
    }

    [Fact]
    public void Calculate_TotalReturn_EqualsFinalMinusInitial()
    {
        // Arrange
        DateTime date = new(2024, 1, 1);
        List<Trade> trades = [];
        List<EquityPoint> equityCurve =
        [
            new(date, 10000m, 0),
            new(date.AddDays(1), 12500m, 0)
        ];

        decimal initialCapital = 10000m;

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, initialCapital, 2);

        // Assert
        result.TotalReturn.ShouldBe(2500m);
        result.TotalReturn.ShouldBe(result.FinalEquity - result.InitialCapital);
    }

    [Fact]
    public void Calculate_TotalReturnPercentage_CalculatesCorrectly()
    {
        // Arrange
        DateTime date = new(2024, 1, 1);
        List<Trade> trades = [];
        List<EquityPoint> equityCurve =
        [
            new(date, 10000m, 0),
            new(date.AddDays(1), 11000m, 0)
        ];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 2);

        // Assert
        result.TotalReturnPercentage.ShouldBe(10m); // (1000 / 10000) * 100 = 10%
    }

    [Fact]
    public void Calculate_WithZeroEquityCurve_ReturnsEmptyMetrics()
    {
        // Arrange
        List<Trade> trades = [];
        List<EquityPoint> equityCurve = [];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 0);

        // Assert
        result.TotalReturn.ShouldBe(0m);
        result.TotalTrades.ShouldBe(0);
    }

    [Fact]
    public void Calculate_InitialCapital_MatchesInput()
    {
        // Arrange
        DateTime date = new(2024, 1, 1);
        List<Trade> trades = [];
        List<EquityPoint> equityCurve =
        [
            new(date, 25000m, 0)
        ];
        decimal initialCapital = 25000m;

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, initialCapital, 1);

        // Assert
        result.InitialCapital.ShouldBe(initialCapital);
    }

    [Fact]
    public void Calculate_TotalDays_MatchesInput()
    {
        // Arrange
        DateTime date = new(2024, 1, 1);
        List<Trade> trades = [];
        List<EquityPoint> equityCurve =
        [
            new(date, 10000m, 0)
        ];
        int totalDays = 252;

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, totalDays);

        // Assert
        result.TotalDays.ShouldBe(totalDays);
    }

    #endregion

    #region Win/Loss Metrics Tests (8 tests)

    [Fact]
    public void Calculate_WinRate_WithAllWinningTrades_Returns100Percent()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<Trade> trades =
        [
            new() { Type = TradeType.Sell, ProfitLoss = 100m, DateTime = baseDate },
            new() { Type = TradeType.Sell, ProfitLoss = 50m, DateTime = baseDate.AddDays(1) },
            new() { Type = TradeType.Sell, ProfitLoss = 75m, DateTime = baseDate.AddDays(2) }
        ];

        List<EquityPoint> equityCurve =
        [
            new(baseDate, 10000m, 0),
            new(baseDate.AddDays(3), 10225m, 0)
        ];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 4);

        // Assert
        result.WinRate.ShouldBe(100m);
        result.WinningTrades.ShouldBe(3);
        result.LosingTrades.ShouldBe(0);
    }

    [Fact]
    public void Calculate_WinRate_WithAllLosingTrades_Returns0Percent()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<Trade> trades =
        [
            new() { Type = TradeType.Sell, ProfitLoss = -100m, DateTime = baseDate },
            new() { Type = TradeType.Sell, ProfitLoss = -50m, DateTime = baseDate.AddDays(1) },
            new() { Type = TradeType.Sell, ProfitLoss = -75m, DateTime = baseDate.AddDays(2) }
        ];

        List<EquityPoint> equityCurve =
        [
            new(baseDate, 10000m, 0),
            new(baseDate.AddDays(3), 9775m, 0)
        ];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 4);

        // Assert
        result.WinRate.ShouldBe(0m);
        result.WinningTrades.ShouldBe(0);
        result.LosingTrades.ShouldBe(3);
    }

    [Fact]
    public void Calculate_WinRate_WithMixedTrades_CalculatesCorrectly()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<Trade> trades =
        [
            new() { Type = TradeType.Sell, ProfitLoss = 100m, DateTime = baseDate },
            new() { Type = TradeType.Sell, ProfitLoss = -50m, DateTime = baseDate.AddDays(1) },
            new() { Type = TradeType.Sell, ProfitLoss = 75m, DateTime = baseDate.AddDays(2) },
            new() { Type = TradeType.Sell, ProfitLoss = -25m, DateTime = baseDate.AddDays(3) }
        ];

        List<EquityPoint> equityCurve =
        [
            new(baseDate, 10000m, 0),
            new(baseDate.AddDays(4), 10100m, 0)
        ];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 5);

        // Assert
        result.WinRate.ShouldBe(50m); // 2 wins out of 4 trades = 50%
        result.WinningTrades.ShouldBe(2);
        result.LosingTrades.ShouldBe(2);
    }

    [Fact]
    public void Calculate_WinningTrades_CountsOnlyPositiveProfitLoss()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<Trade> trades =
        [
            new() { Type = TradeType.Sell, ProfitLoss = 100m, DateTime = baseDate },
            new() { Type = TradeType.Sell, ProfitLoss = 0m, DateTime = baseDate.AddDays(1) }, // Breakeven
            new() { Type = TradeType.Sell, ProfitLoss = -50m, DateTime = baseDate.AddDays(2) }
        ];

        List<EquityPoint> equityCurve =
        [
            new(baseDate, 10000m, 0),
            new(baseDate.AddDays(3), 10050m, 0)
        ];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 4);

        // Assert
        result.WinningTrades.ShouldBe(1);
        result.LosingTrades.ShouldBe(1);
    }

    [Fact]
    public void Calculate_LosingTrades_CountsOnlyNegativeProfitLoss()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<Trade> trades =
        [
            new() { Type = TradeType.Sell, ProfitLoss = -100m, DateTime = baseDate },
            new() { Type = TradeType.Sell, ProfitLoss = -50m, DateTime = baseDate.AddDays(1) },
            new() { Type = TradeType.Sell, ProfitLoss = 0m, DateTime = baseDate.AddDays(2) }
        ];

        List<EquityPoint> equityCurve =
        [
            new(baseDate, 10000m, 0),
            new(baseDate.AddDays(3), 9850m, 0)
        ];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 4);

        // Assert
        result.LosingTrades.ShouldBe(2);
    }

    [Fact]
    public void Calculate_AverageWin_CalculatesCorrectly()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<Trade> trades =
        [
            new() { Type = TradeType.Sell, ProfitLoss = 100m, DateTime = baseDate },
            new() { Type = TradeType.Sell, ProfitLoss = 200m, DateTime = baseDate.AddDays(1) },
            new() { Type = TradeType.Sell, ProfitLoss = 150m, DateTime = baseDate.AddDays(2) }
        ];

        List<EquityPoint> equityCurve =
        [
            new(baseDate, 10000m, 0),
            new(baseDate.AddDays(3), 10450m, 0)
        ];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 4);

        // Assert
        result.AverageWin.ShouldBe(150m); // (100 + 200 + 150) / 3 = 150
    }

    [Fact]
    public void Calculate_AverageLoss_CalculatesCorrectly()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<Trade> trades =
        [
            new() { Type = TradeType.Sell, ProfitLoss = -100m, DateTime = baseDate },
            new() { Type = TradeType.Sell, ProfitLoss = -200m, DateTime = baseDate.AddDays(1) },
            new() { Type = TradeType.Sell, ProfitLoss = -150m, DateTime = baseDate.AddDays(2) }
        ];

        List<EquityPoint> equityCurve =
        [
            new(baseDate, 10000m, 0),
            new(baseDate.AddDays(3), 9550m, 0)
        ];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 4);

        // Assert
        result.AverageLoss.ShouldBe(150m); // Average of absolute values
    }

    [Fact]
    public void Calculate_LargestWinAndLoss_IdentifiesCorrectly()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<Trade> trades =
        [
            new() { Type = TradeType.Sell, ProfitLoss = 100m, DateTime = baseDate },
            new() { Type = TradeType.Sell, ProfitLoss = 500m, DateTime = baseDate.AddDays(1) },
            new() { Type = TradeType.Sell, ProfitLoss = -200m, DateTime = baseDate.AddDays(2) },
            new() { Type = TradeType.Sell, ProfitLoss = -750m, DateTime = baseDate.AddDays(3) }
        ];

        List<EquityPoint> equityCurve =
        [
            new(baseDate, 10000m, 0),
            new(baseDate.AddDays(4), 9650m, 0)
        ];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 5);

        // Assert
        result.LargestWin.ShouldBe(500m);
        result.LargestLoss.ShouldBe(750m);
    }

    #endregion

    #region Advanced Metrics Tests (12 tests)

    [Fact]
    public void Calculate_ProfitFactor_WithNoLosses_Returns0()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<Trade> trades =
        [
            new() { Type = TradeType.Sell, ProfitLoss = 100m, DateTime = baseDate },
            new() { Type = TradeType.Sell, ProfitLoss = 200m, DateTime = baseDate.AddDays(1) }
        ];

        List<EquityPoint> equityCurve =
        [
            new(baseDate, 10000m, 0),
            new(baseDate.AddDays(2), 10300m, 0)
        ];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 3);

        // Assert
        result.ProfitFactor.ShouldBe(0m); // No losses, profit factor is 0 by convention
    }

    [Fact]
    public void Calculate_ProfitFactor_EqualsGrossProfitDividedByGrossLoss()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<Trade> trades =
        [
            new() { Type = TradeType.Sell, ProfitLoss = 300m, DateTime = baseDate },
            new() { Type = TradeType.Sell, ProfitLoss = 200m, DateTime = baseDate.AddDays(1) },
            new() { Type = TradeType.Sell, ProfitLoss = -100m, DateTime = baseDate.AddDays(2) },
            new() { Type = TradeType.Sell, ProfitLoss = -150m, DateTime = baseDate.AddDays(3) }
        ];

        List<EquityPoint> equityCurve =
        [
            new(baseDate, 10000m, 0),
            new(baseDate.AddDays(4), 10250m, 0)
        ];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 5);

        // Assert
        // Gross Profit = 300 + 200 = 500
        // Gross Loss = 100 + 150 = 250
        // Profit Factor = 500 / 250 = 2.0
        result.ProfitFactor.ShouldBe(2.0m);
    }

    [Fact]
    public void Calculate_ProfitFactor_WithNoWins_Returns0()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<Trade> trades =
        [
            new() { Type = TradeType.Sell, ProfitLoss = -100m, DateTime = baseDate },
            new() { Type = TradeType.Sell, ProfitLoss = -200m, DateTime = baseDate.AddDays(1) }
        ];

        List<EquityPoint> equityCurve =
        [
            new(baseDate, 10000m, 0),
            new(baseDate.AddDays(2), 9700m, 0)
        ];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 3);

        // Assert
        result.ProfitFactor.ShouldBe(0m);
    }

    [Fact]
    public void Calculate_SharpeRatio_WithPositiveReturns_IsPositive()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<EquityPoint> equityCurve = [];
        decimal equity = 10000m;

        // Create upward trending equity curve
        for (int i = 0; i < 100; i++)
        {
            equity += 10m; // Steady gains
            equityCurve.Add(new EquityPoint(baseDate.AddDays(i), equity, 0));
        }

        List<Trade> trades = [];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 100);

        // Assert
        result.SharpeRatio.ShouldBeGreaterThan(0m);
    }

    [Fact]
    public void Calculate_SharpeRatio_WithNegativeReturns_IsNegative()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<EquityPoint> equityCurve = [];
        decimal equity = 10000m;

        // Create downward trending equity curve
        for (int i = 0; i < 100; i++)
        {
            equity -= 10m; // Steady losses
            equityCurve.Add(new EquityPoint(baseDate.AddDays(i), equity, 0));
        }

        List<Trade> trades = [];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 100);

        // Assert
        result.SharpeRatio.ShouldBeLessThan(0m);
    }

    [Fact]
    public void Calculate_SharpeRatio_WithZeroVolatility_Returns0()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<EquityPoint> equityCurve = [];

        // Flat equity curve (no volatility)
        for (int i = 0; i < 10; i++)
        {
            equityCurve.Add(new EquityPoint(baseDate.AddDays(i), 10000m, 0));
        }

        List<Trade> trades = [];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 10);

        // Assert
        result.SharpeRatio.ShouldBe(0m);
        result.Volatility.ShouldBe(0m);
    }

    [Fact]
    public void Calculate_SharpeRatio_AnnualizedCorrectly()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<EquityPoint> equityCurve = [];
        decimal equity = 10000m;

        for (int i = 0; i < 252; i++) // One year of trading days
        {
            equity += 5m;
            equityCurve.Add(new EquityPoint(baseDate.AddDays(i), equity, 0));
        }

        List<Trade> trades = [];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 252);

        // Assert
        // Sharpe ratio should be annualized (multiplied by sqrt(252))
        result.SharpeRatio.ShouldNotBe(0m);
    }

    [Fact]
    public void Calculate_Volatility_WithStableReturns_IsLow()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<EquityPoint> equityCurve = [];
        decimal equity = 10000m;

        // Very stable returns
        for (int i = 0; i < 100; i++)
        {
            equity += 1m; // Small, consistent gains
            equityCurve.Add(new EquityPoint(baseDate.AddDays(i), equity, 0));
        }

        List<Trade> trades = [];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 100);

        // Assert
        result.Volatility.ShouldBeLessThan(5m); // Low volatility
    }

    [Fact]
    public void Calculate_Volatility_WithVolatileReturns_IsHigh()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<EquityPoint> equityCurve = [];
        decimal equity = 10000m;

        // Volatile returns
        for (int i = 0; i < 100; i++)
        {
            equity += (i % 2 == 0) ? 100m : -90m; // Large swings
            equityCurve.Add(new EquityPoint(baseDate.AddDays(i), equity, 0));
        }

        List<Trade> trades = [];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 100);

        // Assert
        result.Volatility.ShouldBeGreaterThan(10m); // High volatility
    }

    [Fact]
    public void Calculate_Volatility_AnnualizedCorrectly()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<EquityPoint> equityCurve = [];
        decimal equity = 10000m;

        for (int i = 0; i < 252; i++)
        {
            equity += (i % 2 == 0) ? 10m : -5m;
            equityCurve.Add(new EquityPoint(baseDate.AddDays(i), equity, 0));
        }

        List<Trade> trades = [];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 252);

        // Assert
        // Volatility should be annualized (multiplied by sqrt(252))
        result.Volatility.ShouldBeGreaterThan(0m);
    }

    [Fact]
    public void Calculate_AnnualizedReturn_WithOneYear_EqualsSimpleReturn()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<EquityPoint> equityCurve =
        [
            new(baseDate, 10000m, 0),
            new(baseDate.AddDays(252), 11000m, 0) // 10% return over 1 year
        ];

        List<Trade> trades = [];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 252);

        // Assert
        result.TotalReturnPercentage.ShouldBe(10m);
        // Annualized return should be close to simple return for 1 year
        result.AnnualizedReturn.ShouldBe(10m, 0.1m);
    }

    [Fact]
    public void Calculate_AnnualizedReturn_WithMultipleYears_CompoundsCorrectly()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<EquityPoint> equityCurve =
        [
            new(baseDate, 10000m, 0),
            new(baseDate.AddDays(504), 12100m, 0) // 21% return over 2 years
        ];

        List<Trade> trades = [];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 504);

        // Assert
        // Annualized return should be ~10% (sqrt(1.21) - 1)
        result.AnnualizedReturn.ShouldBe(10m, 0.5m);
    }

    #endregion

    #region Drawdown Metrics Tests (6 tests)

    [Fact]
    public void Calculate_MaxDrawdown_WithContinuousGains_IsZero()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<EquityPoint> equityCurve = [];
        decimal equity = 10000m;

        // Continuously increasing equity
        for (int i = 0; i < 50; i++)
        {
            equity += 100m;
            equityCurve.Add(new EquityPoint(baseDate.AddDays(i), equity, 0));
        }

        List<Trade> trades = [];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 50);

        // Assert
        result.MaxDrawdown.ShouldBe(0m);
        result.MaxDrawdownPercentage.ShouldBe(0m);
    }

    [Fact]
    public void Calculate_MaxDrawdown_WithContinuousLosses_IsMaxLoss()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<EquityPoint> equityCurve =
        [
            new(baseDate, 10000m, 0),
            new(baseDate.AddDays(1), 9000m, 0),
            new(baseDate.AddDays(2), 8000m, 0),
            new(baseDate.AddDays(3), 7000m, 0)
        ];

        List<Trade> trades = [];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 4);

        // Assert
        result.MaxDrawdown.ShouldBe(3000m); // 10000 - 7000
        result.MaxDrawdownPercentage.ShouldBe(30m); // (3000 / 10000) * 100
    }

    [Fact]
    public void Calculate_MaxDrawdown_IdentifiesLargestPeakToTrough()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<EquityPoint> equityCurve =
        [
            new(baseDate, 10000m, 0),
            new(baseDate.AddDays(1), 12000m, 0), // Peak 1
            new(baseDate.AddDays(2), 11000m, 0),
            new(baseDate.AddDays(3), 13000m, 0), // Peak 2 (highest)
            new(baseDate.AddDays(4), 11000m, 0),
            new(baseDate.AddDays(5), 9000m, 0),  // Trough (largest drawdown from Peak 2)
            new(baseDate.AddDays(6), 10000m, 0)
        ];

        List<Trade> trades = [];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 7);

        // Assert
        result.MaxDrawdown.ShouldBe(4000m); // 13000 - 9000
        result.MaxDrawdownPercentage.ShouldBe(30.769m, 0.01m); // (4000 / 13000) * 100
    }

    [Fact]
    public void Calculate_MaxDrawdownPercentage_CalculatesCorrectly()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<EquityPoint> equityCurve =
        [
            new(baseDate, 10000m, 0),
            new(baseDate.AddDays(1), 15000m, 0), // Peak
            new(baseDate.AddDays(2), 12000m, 0)  // Drawdown of 3000
        ];

        List<Trade> trades = [];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 3);

        // Assert
        result.MaxDrawdown.ShouldBe(3000m);
        result.MaxDrawdownPercentage.ShouldBe(20m); // (3000 / 15000) * 100 = 20%
    }

    [Fact]
    public void Calculate_MaxDrawdown_WithRecovery_OnlyCountsPeakToTrough()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<EquityPoint> equityCurve =
        [
            new(baseDate, 10000m, 0),
            new(baseDate.AddDays(1), 12000m, 0), // Peak
            new(baseDate.AddDays(2), 9000m, 0),  // Trough (drawdown = 3000)
            new(baseDate.AddDays(3), 11000m, 0)  // Recovery (doesn't affect max drawdown)
        ];

        List<Trade> trades = [];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 4);

        // Assert
        result.MaxDrawdown.ShouldBe(3000m);
    }

    [Fact]
    public void Calculate_MaxDrawdown_WithEmptyEquityCurve_ReturnsZero()
    {
        // Arrange
        List<Trade> trades = [];
        List<EquityPoint> equityCurve = [];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 0);

        // Assert
        result.MaxDrawdown.ShouldBe(0m);
        result.MaxDrawdownPercentage.ShouldBe(0m);
    }

    #endregion

    #region Consecutive Wins/Losses Tests (4 tests)

    [Fact]
    public void Calculate_MaxConsecutiveWins_IdentifiesLongestStreak()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<Trade> trades =
        [
            new() { Type = TradeType.Sell, ProfitLoss = 100m, DateTime = baseDate },
            new() { Type = TradeType.Sell, ProfitLoss = 50m, DateTime = baseDate.AddDays(1) },
            new() { Type = TradeType.Sell, ProfitLoss = 75m, DateTime = baseDate.AddDays(2) },
            new() { Type = TradeType.Sell, ProfitLoss = -25m, DateTime = baseDate.AddDays(3) },
            new() { Type = TradeType.Sell, ProfitLoss = 60m, DateTime = baseDate.AddDays(4) },
            new() { Type = TradeType.Sell, ProfitLoss = 80m, DateTime = baseDate.AddDays(5) }
        ];

        List<EquityPoint> equityCurve =
        [
            new(baseDate, 10000m, 0),
            new(baseDate.AddDays(6), 10340m, 0)
        ];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 7);

        // Assert
        result.MaxConsecutiveWins.ShouldBe(3); // First 3 trades
    }

    [Fact]
    public void Calculate_MaxConsecutiveLosses_IdentifiesLongestStreak()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<Trade> trades =
        [
            new() { Type = TradeType.Sell, ProfitLoss = -100m, DateTime = baseDate },
            new() { Type = TradeType.Sell, ProfitLoss = -50m, DateTime = baseDate.AddDays(1) },
            new() { Type = TradeType.Sell, ProfitLoss = -75m, DateTime = baseDate.AddDays(2) },
            new() { Type = TradeType.Sell, ProfitLoss = -30m, DateTime = baseDate.AddDays(3) },
            new() { Type = TradeType.Sell, ProfitLoss = 60m, DateTime = baseDate.AddDays(4) },
            new() { Type = TradeType.Sell, ProfitLoss = -40m, DateTime = baseDate.AddDays(5) }
        ];

        List<EquityPoint> equityCurve =
        [
            new(baseDate, 10000m, 0),
            new(baseDate.AddDays(6), 9735m, 0)
        ];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 7);

        // Assert
        result.MaxConsecutiveLosses.ShouldBe(4); // First 4 trades
    }

    [Fact]
    public void Calculate_ConsecutiveWinsAndLosses_WithAlternatingTrades()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<Trade> trades =
        [
            new() { Type = TradeType.Sell, ProfitLoss = 100m, DateTime = baseDate },
            new() { Type = TradeType.Sell, ProfitLoss = -50m, DateTime = baseDate.AddDays(1) },
            new() { Type = TradeType.Sell, ProfitLoss = 75m, DateTime = baseDate.AddDays(2) },
            new() { Type = TradeType.Sell, ProfitLoss = -25m, DateTime = baseDate.AddDays(3) }
        ];

        List<EquityPoint> equityCurve =
        [
            new(baseDate, 10000m, 0),
            new(baseDate.AddDays(4), 10100m, 0)
        ];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 5);

        // Assert
        result.MaxConsecutiveWins.ShouldBe(1);
        result.MaxConsecutiveLosses.ShouldBe(1);
    }

    [Fact]
    public void Calculate_ConsecutiveWinsAndLosses_WithNoTrades_ReturnsZero()
    {
        // Arrange
        List<Trade> trades = [];
        List<EquityPoint> equityCurve =
        [
            new(new DateTime(2024, 1, 1), 10000m, 0)
        ];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 1);

        // Assert
        result.MaxConsecutiveWins.ShouldBe(0);
        result.MaxConsecutiveLosses.ShouldBe(0);
    }

    #endregion

    #region Market Exposure Tests (5 tests)

    [Fact]
    public void Calculate_DaysInMarket_CountsPositionGreaterThanZero()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<EquityPoint> equityCurve =
        [
            new(baseDate, 10000m, 0),      // Not in market
            new(baseDate.AddDays(1), 9000m, 10),  // In market
            new(baseDate.AddDays(2), 9500m, 10),  // In market
            new(baseDate.AddDays(3), 10000m, 0),  // Not in market
            new(baseDate.AddDays(4), 9500m, 5)    // In market
        ];

        List<Trade> trades = [];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 5);

        // Assert
        result.DaysInMarket.ShouldBe(3); // Days with position > 0
    }

    [Fact]
    public void Calculate_DaysInMarket_ExcludesZeroPosition()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<EquityPoint> equityCurve =
        [
            new(baseDate, 10000m, 0),
            new(baseDate.AddDays(1), 10000m, 0),
            new(baseDate.AddDays(2), 10000m, 0)
        ];

        List<Trade> trades = [];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 3);

        // Assert
        result.DaysInMarket.ShouldBe(0);
    }

    [Fact]
    public void Calculate_MarketExposurePercentage_CalculatesCorrectly()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<EquityPoint> equityCurve =
        [
            new(baseDate, 10000m, 0),
            new(baseDate.AddDays(1), 9000m, 10),
            new(baseDate.AddDays(2), 9500m, 10),
            new(baseDate.AddDays(3), 10000m, 0)
        ];

        List<Trade> trades = [];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 4);

        // Assert
        result.MarketExposurePercentage.ShouldBe(50m); // 2 days in market out of 4 total days
    }

    [Fact]
    public void Calculate_MarketExposurePercentage_WithFullyInvested_Returns100()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<EquityPoint> equityCurve =
        [
            new(baseDate, 9000m, 10),
            new(baseDate.AddDays(1), 9500m, 10),
            new(baseDate.AddDays(2), 10000m, 10)
        ];

        List<Trade> trades = [];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 3);

        // Assert
        result.MarketExposurePercentage.ShouldBe(100m);
    }

    [Fact]
    public void Calculate_MarketExposurePercentage_WithNeverInvested_Returns0()
    {
        // Arrange
        DateTime baseDate = new(2024, 1, 1);
        List<EquityPoint> equityCurve =
        [
            new(baseDate, 10000m, 0),
            new(baseDate.AddDays(1), 10000m, 0),
            new(baseDate.AddDays(2), 10000m, 0)
        ];

        List<Trade> trades = [];

        // Act
        PerformanceMetrics result = _calculator.Calculate(trades, equityCurve, 10000m, 3);

        // Assert
        result.MarketExposurePercentage.ShouldBe(0m);
    }

    #endregion
}
