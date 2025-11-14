// <copyright file="PositionSizeCalculatorTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using FakeItEasy;
using Microsoft.Extensions.Logging;
using Shouldly;
using TradingBot.Engine;

namespace TradingBot.Engine.Tests;

/// <summary>
/// Unit tests for PositionSizeCalculator.
/// </summary>
public class PositionSizeCalculatorTests
{
    private readonly PositionSizeCalculator _calculator;
    private readonly ILogger<PositionSizeCalculator> _logger;

    public PositionSizeCalculatorTests()
    {
        _logger = A.Fake<ILogger<PositionSizeCalculator>>();
        _calculator = new PositionSizeCalculator(_logger);
    }

    #region CalculateFixedAmount Tests

    [Fact]
    public void CalculateFixedAmount_WithValidInputs_ShouldCalculateCorrectly()
    {
        // Arrange
        var fixedAmount = 1000m;
        var currentPrice = 50m;
        var leverage = 1.0m;

        // Act
        var result = _calculator.CalculateFixedAmount(fixedAmount, currentPrice, leverage);

        // Assert
        result.ShouldBe(20m); // 1000 / 50 = 20 units
    }

    [Fact]
    public void CalculateFixedAmount_WithLeverage_ShouldMultiplyByLeverage()
    {
        // Arrange
        var fixedAmount = 1000m;
        var currentPrice = 50m;
        var leverage = 2.0m;

        // Act
        var result = _calculator.CalculateFixedAmount(fixedAmount, currentPrice, leverage);

        // Assert
        result.ShouldBe(40m); // (1000 * 2) / 50 = 40 units
    }

    [Fact]
    public void CalculateFixedAmount_WithNegativeAmount_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            _calculator.CalculateFixedAmount(-100m, 50m, 1.0m))
            .Message.ShouldContain("Fixed amount must be positive");
    }

    [Fact]
    public void CalculateFixedAmount_WithZeroPrice_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            _calculator.CalculateFixedAmount(1000m, 0m, 1.0m))
            .Message.ShouldContain("Current price must be positive");
    }

    [Fact]
    public void CalculateFixedAmount_WithLeverageLessThanOne_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            _calculator.CalculateFixedAmount(1000m, 50m, 0.5m))
            .Message.ShouldContain("Leverage must be at least 1.0");
    }

    #endregion

    #region CalculateFixedPercent Tests

    [Fact]
    public void CalculateFixedPercent_WithValidInputs_ShouldCalculateCorrectly()
    {
        // Arrange
        var accountBalance = 10000m;
        var percentOfAccount = 10m; // 10%
        var currentPrice = 50m;
        var leverage = 1.0m;

        // Act
        var result = _calculator.CalculateFixedPercent(
            accountBalance,
            percentOfAccount,
            currentPrice,
            leverage);

        // Assert
        result.ShouldBe(20m); // (10000 * 0.10) / 50 = 20 units
    }

    [Fact]
    public void CalculateFixedPercent_WithLeverage_ShouldMultiplyByLeverage()
    {
        // Arrange
        var accountBalance = 10000m;
        var percentOfAccount = 10m;
        var currentPrice = 50m;
        var leverage = 2.0m;

        // Act
        var result = _calculator.CalculateFixedPercent(
            accountBalance,
            percentOfAccount,
            currentPrice,
            leverage);

        // Assert
        result.ShouldBe(40m); // ((10000 * 0.10) * 2) / 50 = 40 units
    }

    [Fact]
    public void CalculateFixedPercent_WithNegativeBalance_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            _calculator.CalculateFixedPercent(-1000m, 10m, 50m, 1.0m))
            .Message.ShouldContain("Account balance must be positive");
    }

    [Fact]
    public void CalculateFixedPercent_WithPercentOver100_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            _calculator.CalculateFixedPercent(10000m, 150m, 50m, 1.0m))
            .Message.ShouldContain("Percent of account must be between 0 and 100");
    }

    [Fact]
    public void CalculateFixedPercent_WithZeroPercent_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            _calculator.CalculateFixedPercent(10000m, 0m, 50m, 1.0m))
            .Message.ShouldContain("Percent of account must be between 0 and 100");
    }

    #endregion

    #region CalculateRiskBased Tests

    [Fact]
    public void CalculateRiskBased_WithValidInputs_ShouldCalculateCorrectly()
    {
        // Arrange
        var accountBalance = 10000m;
        var riskPercent = 2m; // 2% risk
        var entryPrice = 100m;
        var stopLossPrice = 95m; // $5 stop distance
        var leverage = 1.0m;

        // Act
        var result = _calculator.CalculateRiskBased(
            accountBalance,
            riskPercent,
            entryPrice,
            stopLossPrice,
            leverage);

        // Assert
        result.ShouldBe(40m); // (10000 * 0.02) / 5 = 40 units
    }

    [Fact]
    public void CalculateRiskBased_WithStopAboveEntry_ShouldCalculateCorrectly()
    {
        // Arrange
        var accountBalance = 10000m;
        var riskPercent = 2m;
        var entryPrice = 100m;
        var stopLossPrice = 105m; // Stop above entry (short position)
        var leverage = 1.0m;

        // Act
        var result = _calculator.CalculateRiskBased(
            accountBalance,
            riskPercent,
            entryPrice,
            stopLossPrice,
            leverage);

        // Assert
        result.ShouldBe(40m); // (10000 * 0.02) / 5 = 40 units
    }

    [Fact]
    public void CalculateRiskBased_WithSameEntryAndStop_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            _calculator.CalculateRiskBased(10000m, 2m, 100m, 100m, 1.0m))
            .Message.ShouldContain("Entry price and stop-loss price cannot be the same");
    }

    [Fact]
    public void CalculateRiskBased_WithNegativeStopPrice_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            _calculator.CalculateRiskBased(10000m, 2m, 100m, -10m, 1.0m))
            .Message.ShouldContain("Stop-loss price must be positive");
    }

    #endregion

    #region CalculateKelly Tests

    [Fact]
    public void CalculateKelly_WithValidInputs_ShouldCalculateCorrectly()
    {
        // Arrange
        var accountBalance = 10000m;
        var winRate = 0.6m; // 60% win rate
        var avgWin = 100m;
        var avgLoss = 50m;
        var currentPrice = 50m;
        var leverage = 1.0m;
        var kellyFraction = 0.25m; // Quarter Kelly

        // Act
        var result = _calculator.CalculateKelly(
            accountBalance,
            winRate,
            avgWin,
            avgLoss,
            currentPrice,
            leverage,
            kellyFraction);

        // Assert
        result.ShouldBeGreaterThan(0m);
        result.ShouldBeLessThan(10000m / currentPrice); // Should not exceed full account
    }

    [Fact]
    public void CalculateKelly_WithHighWinRate_ShouldSuggestLargerPosition()
    {
        // Arrange
        var accountBalance = 10000m;
        var currentPrice = 50m;

        // Act
        var highWinRateResult = _calculator.CalculateKelly(
            accountBalance,
            0.7m, // 70% win rate
            100m,
            50m,
            currentPrice);

        var lowWinRateResult = _calculator.CalculateKelly(
            accountBalance,
            0.5m, // 50% win rate
            100m,
            50m,
            currentPrice);

        // Assert
        highWinRateResult.ShouldBeGreaterThan(lowWinRateResult);
    }

    [Fact]
    public void CalculateKelly_WithWinRateOver1_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            _calculator.CalculateKelly(10000m, 1.5m, 100m, 50m, 50m))
            .Message.ShouldContain("Win rate must be between 0 and 1");
    }

    [Fact]
    public void CalculateKelly_WithNegativeAvgWin_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            _calculator.CalculateKelly(10000m, 0.6m, -100m, 50m, 50m))
            .Message.ShouldContain("Average win must be positive");
    }

    [Fact]
    public void CalculateKelly_WithKellyFractionOver1_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            _calculator.CalculateKelly(10000m, 0.6m, 100m, 50m, 50m, 1.0m, 1.5m))
            .Message.ShouldContain("Kelly fraction must be between 0 and 1");
    }

    [Fact]
    public void CalculateKelly_WithPoorEdge_ShouldReturnZeroOrSmallPosition()
    {
        // Arrange - Poor edge: low win rate, bad win/loss ratio
        var accountBalance = 10000m;
        var winRate = 0.4m; // 40% win rate
        var avgWin = 50m;
        var avgLoss = 100m; // Lose more than we win
        var currentPrice = 50m;

        // Act
        var result = _calculator.CalculateKelly(
            accountBalance,
            winRate,
            avgWin,
            avgLoss,
            currentPrice);

        // Assert
        result.ShouldBeGreaterThanOrEqualTo(0m); // Kelly caps at 0 for negative edge
    }

    #endregion

    #region CalculateVolatilityBased Tests

    [Fact]
    public void CalculateVolatilityBased_WithValidInputs_ShouldCalculateCorrectly()
    {
        // Arrange
        var accountBalance = 10000m;
        var riskPercent = 2m; // 2% risk
        var atr = 5m; // Average True Range
        var atrMultiplier = 2m;
        var currentPrice = 100m;
        var leverage = 1.0m;

        // Act
        var result = _calculator.CalculateVolatilityBased(
            accountBalance,
            riskPercent,
            atr,
            atrMultiplier,
            currentPrice,
            leverage);

        // Assert
        result.ShouldBe(20m); // (10000 * 0.02) / (5 * 2) = 20 units
    }

    [Fact]
    public void CalculateVolatilityBased_WithHigherVolatility_ShouldReducePosition()
    {
        // Arrange
        var accountBalance = 10000m;
        var riskPercent = 2m;
        var currentPrice = 100m;

        // Act
        var lowVolResult = _calculator.CalculateVolatilityBased(
            accountBalance,
            riskPercent,
            2m, // Low ATR
            2m,
            currentPrice);

        var highVolResult = _calculator.CalculateVolatilityBased(
            accountBalance,
            riskPercent,
            10m, // High ATR
            2m,
            currentPrice);

        // Assert
        lowVolResult.ShouldBeGreaterThan(highVolResult);
    }

    [Fact]
    public void CalculateVolatilityBased_WithNegativeATR_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            _calculator.CalculateVolatilityBased(10000m, 2m, -5m, 2m, 100m))
            .Message.ShouldContain("ATR must be positive");
    }

    [Fact]
    public void CalculateVolatilityBased_WithZeroATRMultiplier_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            _calculator.CalculateVolatilityBased(10000m, 2m, 5m, 0m, 100m))
            .Message.ShouldContain("ATR multiplier must be positive");
    }

    [Fact]
    public void CalculateVolatilityBased_WithLeverage_ShouldMultiplyByLeverage()
    {
        // Arrange
        var accountBalance = 10000m;
        var riskPercent = 2m;
        var atr = 5m;
        var atrMultiplier = 2m;
        var currentPrice = 100m;

        // Act
        var noLeverageResult = _calculator.CalculateVolatilityBased(
            accountBalance,
            riskPercent,
            atr,
            atrMultiplier,
            currentPrice,
            1.0m);

        var withLeverageResult = _calculator.CalculateVolatilityBased(
            accountBalance,
            riskPercent,
            atr,
            atrMultiplier,
            currentPrice,
            2.0m);

        // Assert
        withLeverageResult.ShouldBe(noLeverageResult * 2m);
    }

    #endregion

    #region Common Validation Tests

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new PositionSizeCalculator(null!));
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(2.0)]
    [InlineData(5.0)]
    [InlineData(10.0)]
    public void AllMethods_WithVariousLeverages_ShouldScaleProportionally(double leverageDouble)
    {
        // Arrange
        var leverage = (decimal)leverageDouble;
        var accountBalance = 10000m;
        var currentPrice = 50m;

        // Act
        var fixedAmount = _calculator.CalculateFixedAmount(1000m, currentPrice, leverage);
        var fixedPercent = _calculator.CalculateFixedPercent(accountBalance, 10m, currentPrice, leverage);

        // Assert - All should scale with leverage
        fixedAmount.ShouldBeGreaterThan(0m);
        fixedPercent.ShouldBeGreaterThan(0m);

        if (leverage > 1.0m)
        {
            var baseFixedAmount = _calculator.CalculateFixedAmount(1000m, currentPrice, 1.0m);
            fixedAmount.ShouldBe(baseFixedAmount * leverage);
        }
    }

    #endregion
}
