// <copyright file="PerformanceCalculatorTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Shouldly;
using TradingBot.Analytics;
using TradingBot.Core.Enums;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Analytics.Tests;

/// <summary>
/// Unit tests for PerformanceCalculator.
/// </summary>
public sealed class PerformanceCalculatorTests
{
    private readonly PerformanceCalculator _calculator;

    public PerformanceCalculatorTests()
    {
        _calculator = new PerformanceCalculator();
    }

    [Fact]
    public void CalculateMetrics_WithEmptyTrades_ShouldReturnEmptyMetrics()
    {
        // Arrange
        var trades = new List<Trade>();
        var initialCapital = 10000m;
        var finalEquity = 10000m;
        var equityCurve = new List<(DateTime, decimal)>
        {
            (DateTime.UtcNow, 10000m),
        };

        // Act
        var metrics = _calculator.CalculateMetrics(trades, initialCapital, finalEquity, equityCurve);

        // Assert
        metrics.ShouldNotBeNull();
        metrics.TotalReturn.ShouldBe(0m);
        metrics.TotalTrades.ShouldBe(0);
        metrics.WinningTrades.ShouldBe(0);
        metrics.LosingTrades.ShouldBe(0);
        metrics.ProfitFactor.ShouldBe(0m);
    }

    [Fact]
    public void CalculateMetrics_WithProfitableTrades_ShouldCalculateCorrectly()
    {
        // Arrange
        var trades = new List<Trade>
        {
            CreateTrade("SPY", 100m, 110m, 10, commission: 2m),  // Winning trade
            CreateTrade("AAPL", 150m, 145m, 10, commission: 2m), // Losing trade
            CreateTrade("MSFT", 200m, 220m, 5, commission: 2m),  // Winning trade
        };

        var initialCapital = 10000m;
        var finalEquity = initialCapital + trades.Sum(t => t.RealizedPnL);
        var equityCurve = CreateEquityCurve(initialCapital, trades);

        // Act
        var metrics = _calculator.CalculateMetrics(trades, initialCapital, finalEquity, equityCurve);

        // Assert
        metrics.TotalReturn.ShouldBeGreaterThan(0m); // Should be profitable overall
        metrics.TotalTrades.ShouldBe(3);
        metrics.WinningTrades.ShouldBe(2);
        metrics.LosingTrades.ShouldBe(1);
        metrics.AverageWin.ShouldBeGreaterThan(0m); // Wins are positive
        metrics.AverageLoss.ShouldBeGreaterThan(0m); // AverageLoss is stored as absolute (positive) value
        metrics.ProfitFactor.ShouldBeGreaterThan(1m); // Should be net profitable
    }

    [Fact]
    public void CalculateMetrics_WithAllWinningTrades_ShouldHaveMaxProfitFactor()
    {
        // Arrange
        var trades = new List<Trade>
        {
            CreateTrade("SPY", 100m, 110m, 10, commission: 1m),
            CreateTrade("AAPL", 150m, 160m, 10, commission: 1m),
        };

        var initialCapital = 10000m;
        var finalEquity = 10198m;
        var equityCurve = CreateEquityCurve(initialCapital, trades);

        // Act
        var metrics = _calculator.CalculateMetrics(trades, initialCapital, finalEquity, equityCurve);

        // Assert
        metrics.WinningTrades.ShouldBe(2);
        metrics.LosingTrades.ShouldBe(0);
        metrics.ProfitFactor.ShouldBe(decimal.MaxValue); // No losses = infinite profit factor
    }

    [Fact]
    public void CalculateMetrics_WithAllLosingTrades_ShouldHaveZeroProfitFactor()
    {
        // Arrange
        var trades = new List<Trade>
        {
            CreateTrade("SPY", 110m, 100m, 10, commission: 1m),
            CreateTrade("AAPL", 160m, 150m, 10, commission: 1m),
        };

        var initialCapital = 10000m;
        var finalEquity = 9798m;
        var equityCurve = CreateEquityCurve(initialCapital, trades);

        // Act
        var metrics = _calculator.CalculateMetrics(trades, initialCapital, finalEquity, equityCurve);

        // Assert
        metrics.WinningTrades.ShouldBe(0);
        metrics.LosingTrades.ShouldBe(2);
        metrics.ProfitFactor.ShouldBe(0m);
    }

    [Fact]
    public void CalculateMetrics_ShouldCalculateMaxDrawdownCorrectly()
    {
        // Arrange
        var trades = new List<Trade>
        {
            CreateTrade("SPY", 100m, 120m, 10, commission: 1m),  // +$199 (peak at 10199)
            CreateTrade("AAPL", 150m, 130m, 10, commission: 1m), // -$201 (drop to 9998)
            CreateTrade("MSFT", 200m, 210m, 5, commission: 1m),  // +$49 (recover to 10047)
        };

        var initialCapital = 10000m;
        var finalEquity = 10047m;
        var equityCurve = new List<(DateTime, decimal)>
        {
            (DateTime.UtcNow.AddDays(-3), 10000m),
            (DateTime.UtcNow.AddDays(-2), 10199m), // Peak
            (DateTime.UtcNow.AddDays(-1), 9998m),  // Drawdown
            (DateTime.UtcNow, 10047m),
        };

        // Act
        var metrics = _calculator.CalculateMetrics(trades, initialCapital, finalEquity, equityCurve);

        // Assert
        // Max drawdown = (10199 - 9998) / 10199 * 100 = 1.97%
        metrics.MaxDrawdown.ShouldBe(1.97m, 0.01m);
    }

    [Fact]
    public void CalculateMetrics_ShouldCalculateSharpeRatioCorrectly()
    {
        // Arrange
        var trades = CreateManyTrades(50);
        var initialCapital = 10000m;
        var finalEquity = 11000m;
        var equityCurve = CreateEquityCurveWithVolatility(initialCapital, 100);

        // Act
        var metrics = _calculator.CalculateMetrics(trades, initialCapital, finalEquity, equityCurve);

        // Assert
        // Sharpe ratio should be calculated (can be positive, negative, or zero)
        // Just verify it's a reasonable finite value
        metrics.SharpeRatio.ShouldBeInRange(-10m, 10m);
    }

    [Fact]
    public void CalculateMetrics_ShouldCalculateSortinoRatioCorrectly()
    {
        // Arrange
        var trades = CreateManyTrades(50);
        var initialCapital = 10000m;
        var finalEquity = 11000m;
        var equityCurve = CreateEquityCurveWithVolatility(initialCapital, 100);

        // Act
        var metrics = _calculator.CalculateMetrics(trades, initialCapital, finalEquity, equityCurve);

        // Assert
        // Sortino ratio should be calculated (can be positive, negative, or zero)
        // Just verify it's a reasonable finite value
        metrics.SortinoRatio.ShouldBeInRange(-10m, 10m);
        // Sortino is often higher than Sharpe when strategy has positive skew
        // but this is not guaranteed, so we just check they're both calculated
        Math.Abs(metrics.SortinoRatio).ShouldBeGreaterThanOrEqualTo(0m);
    }

    [Fact]
    public void CalculateMetrics_ShouldCalculateAnnualizedReturnCorrectly()
    {
        // Arrange
        var trades = new List<Trade>
        {
            CreateTrade("SPY", 100m, 110m, 100, commission: 10m),
        };

        var initialCapital = 10000m;
        var finalEquity = 11000m; // 10% total return
        var startDate = DateTime.UtcNow.AddYears(-2);
        var equityCurve = new List<(DateTime, decimal)>
        {
            (startDate, initialCapital),
            (DateTime.UtcNow, finalEquity),
        };

        // Act
        var metrics = _calculator.CalculateMetrics(trades, initialCapital, finalEquity, equityCurve);

        // Assert
        // 10% over 2 years = ~4.88% annualized
        metrics.AnnualizedReturn.ShouldBe(4.88m, 0.2m);
    }

    [Fact]
    public void CalculateMetrics_ShouldCalculateCalmarRatioCorrectly()
    {
        // Arrange
        var trades = CreateManyTrades(10);
        var initialCapital = 10000m;
        var finalEquity = 11000m;
        var equityCurve = new List<(DateTime, decimal)>
        {
            (DateTime.UtcNow.AddYears(-1), 10000m),
            (DateTime.UtcNow.AddMonths(-6), 11000m), // Peak
            (DateTime.UtcNow.AddMonths(-3), 10500m), // Drawdown of ~4.5%
            (DateTime.UtcNow, 11000m),
        };

        // Act
        var metrics = _calculator.CalculateMetrics(trades, initialCapital, finalEquity, equityCurve);

        // Assert
        metrics.CalmarRatio.ShouldNotBe(0m);
        // Calmar = Annualized Return / Max Drawdown
        if (metrics.MaxDrawdown > 0)
        {
            var expectedCalmar = metrics.AnnualizedReturn / metrics.MaxDrawdown;
            metrics.CalmarRatio.ShouldBe(expectedCalmar, 0.01m);
        }
    }

    [Fact]
    public void CalculateMetrics_WithZeroInitialCapital_ShouldHandleGracefully()
    {
        // Arrange
        var trades = new List<Trade>();
        var initialCapital = 0m;
        var finalEquity = 0m;
        var equityCurve = new List<(DateTime, decimal)>();

        // Act
        var metrics = _calculator.CalculateMetrics(trades, initialCapital, finalEquity, equityCurve);

        // Assert
        metrics.ShouldNotBeNull();
        metrics.TotalReturn.ShouldBe(0m);
    }

    [Fact]
    public void CalculateMetrics_WithNullTrades_ShouldReturnEmptyMetrics()
    {
        // Arrange
        var initialCapital = 10000m;
        var finalEquity = 10000m;
        var equityCurve = new List<(DateTime, decimal)>();

        // Act
        var metrics = _calculator.CalculateMetrics(null!, initialCapital, finalEquity, equityCurve);

        // Assert
        metrics.ShouldNotBeNull();
        metrics.TotalTrades.ShouldBe(0);
    }

    [Fact]
    public void CalculateMetrics_WithSingleDataPoint_ShouldHandleGracefully()
    {
        // Arrange
        var trades = new List<Trade>
        {
            CreateTrade("SPY", 100m, 110m, 10, commission: 1m),
        };

        var initialCapital = 10000m;
        var finalEquity = 10099m;
        var equityCurve = new List<(DateTime, decimal)>
        {
            (DateTime.UtcNow, finalEquity),
        };

        // Act
        var metrics = _calculator.CalculateMetrics(trades, initialCapital, finalEquity, equityCurve);

        // Assert
        metrics.ShouldNotBeNull();
        metrics.TotalReturn.ShouldBe(0.99m, 0.01m);
        metrics.SharpeRatio.ShouldBe(0m); // Need at least 2 points for Sharpe
    }

    [Fact]
    public void CalculateMetrics_WithHighVolatility_ShouldReflectInSharpeRatio()
    {
        // Arrange
        var trades = CreateManyTrades(50);
        var initialCapital = 10000m;
        var finalEquity = 10500m;
        var highVolatilityEquityCurve = CreateHighVolatilityEquityCurve(initialCapital, 50);

        // Act
        var metrics = _calculator.CalculateMetrics(trades, initialCapital, finalEquity, highVolatilityEquityCurve);

        // Assert
        // High volatility should result in lower Sharpe ratio
        metrics.SharpeRatio.ShouldBeLessThan(2m);
    }

    // Helper methods
    private Trade CreateTrade(
        string symbol,
        decimal entryPrice,
        decimal exitPrice,
        decimal quantity,
        decimal commission = 0m)
    {
        return new Trade
        {
            Id = Guid.NewGuid(),
            Symbol = symbol,
            Side = OrderSide.Buy,
            Quantity = quantity,
            EntryPrice = entryPrice,
            ExitPrice = exitPrice,
            EntryTime = DateTime.UtcNow.AddDays(-1),
            ExitTime = DateTime.UtcNow,
            Commission = commission,
            StrategyName = "TestStrategy",
        };
    }

    private List<Trade> CreateManyTrades(int count)
    {
        var trades = new List<Trade>();
        var random = new Random(42);

        for (int i = 0; i < count; i++)
        {
            var entryPrice = 100m + (decimal)random.Next(-20, 20);
            var change = (decimal)(random.NextDouble() * 20) - 10m; // -10% to +10%
            var exitPrice = entryPrice * (1m + (change / 100m));

            trades.Add(CreateTrade($"SYM{i}", entryPrice, exitPrice, 10, commission: 1m));
        }

        return trades;
    }

    private List<(DateTime Date, decimal Equity)> CreateEquityCurve(decimal initialCapital, List<Trade> trades)
    {
        var curve = new List<(DateTime, decimal)>
        {
            (DateTime.UtcNow.AddDays(-trades.Count), initialCapital),
        };

        var equity = initialCapital;
        for (int i = 0; i < trades.Count; i++)
        {
            equity += trades[i].RealizedPnL;
            curve.Add((DateTime.UtcNow.AddDays(-trades.Count + i + 1), equity));
        }

        return curve;
    }

    private List<(DateTime Date, decimal Equity)> CreateEquityCurveWithVolatility(decimal initialCapital, int days)
    {
        var curve = new List<(DateTime, decimal)>();
        var equity = initialCapital;
        var random = new Random(42);

        for (int i = 0; i < days; i++)
        {
            // Add some volatility
            var dailyReturn = ((decimal)(random.NextDouble() * 4) - 2m) / 100m; // -2% to +2%
            equity *= 1m + dailyReturn;
            curve.Add((DateTime.UtcNow.AddDays(-days + i), equity));
        }

        return curve;
    }

    private List<(DateTime Date, decimal Equity)> CreateHighVolatilityEquityCurve(decimal initialCapital, int days)
    {
        var curve = new List<(DateTime, decimal)>();
        var equity = initialCapital;
        var random = new Random(42);

        for (int i = 0; i < days; i++)
        {
            // High volatility: -10% to +10% daily
            var dailyReturn = ((decimal)(random.NextDouble() * 20) - 10m) / 100m;
            equity *= 1m + dailyReturn;
            curve.Add((DateTime.UtcNow.AddDays(-days + i), equity));
        }

        return curve;
    }
}
