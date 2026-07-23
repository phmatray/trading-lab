// <copyright file="StrategyConfigurationTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Shouldly;
using TradingBot.Core.ValueObjects;
using Xunit;

namespace TradingBot.Core.Tests.ValueObjects;

/// <summary>
/// Unit tests for <see cref="StrategyConfiguration"/> value object validation.
/// </summary>
public sealed class StrategyConfigurationTests
{
    [Fact]
    public void Validate_WithValidConfiguration_ShouldNotThrow()
    {
        // Arrange
        var config = new StrategyConfiguration(
            minCashRatio: 0.15m,
            maxCashRatio: 0.25m,
            weeklyBuyRatio: 0.05m,
            weeklySellRatio: 0.10m,
            executionDayOfWeek: 5);

        // Act & Assert
        Should.NotThrow(() => config.Validate());
    }

    [Theory]
    [InlineData(-0.01, 0.25, 0.05, 0.10, 5)] // Negative MinCashRatio
    [InlineData(1.01, 0.25, 0.05, 0.10, 5)]  // MinCashRatio > 1
    public void Validate_WithInvalidMinCashRatio_ShouldThrow(
        decimal minCashRatio,
        decimal maxCashRatio,
        decimal weeklyBuyRatio,
        decimal weeklySellRatio,
        int executionDayOfWeek)
    {
        // Arrange
        var config = new StrategyConfiguration(
            minCashRatio,
            maxCashRatio,
            weeklyBuyRatio,
            weeklySellRatio,
            executionDayOfWeek);

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => config.Validate());
        ex.Message.ShouldContain("MinCashRatio must be between 0 and 1");
    }

    [Theory]
    [InlineData(0.15, -0.01, 0.05, 0.10, 5)] // Negative MaxCashRatio
    [InlineData(0.15, 1.01, 0.05, 0.10, 5)]  // MaxCashRatio > 1
    public void Validate_WithInvalidMaxCashRatio_ShouldThrow(
        decimal minCashRatio,
        decimal maxCashRatio,
        decimal weeklyBuyRatio,
        decimal weeklySellRatio,
        int executionDayOfWeek)
    {
        // Arrange
        var config = new StrategyConfiguration(
            minCashRatio,
            maxCashRatio,
            weeklyBuyRatio,
            weeklySellRatio,
            executionDayOfWeek);

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => config.Validate());
        ex.Message.ShouldContain("MaxCashRatio must be between 0 and 1");
    }

    [Fact]
    public void Validate_WhenMinCashRatioGreaterThanOrEqualToMaxCashRatio_ShouldThrow()
    {
        // Arrange
        var config = new StrategyConfiguration(
            minCashRatio: 0.25m,
            maxCashRatio: 0.25m,
            weeklyBuyRatio: 0.05m,
            weeklySellRatio: 0.10m,
            executionDayOfWeek: 5);

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => config.Validate());
        ex.Message.ShouldContain("MinCashRatio must be less than MaxCashRatio");
    }

    [Theory]
    [InlineData(0.15, 0.25, -0.01, 0.10, 5)] // Negative WeeklyBuyRatio
    [InlineData(0.15, 0.25, 1.01, 0.10, 5)]  // WeeklyBuyRatio > 1
    public void Validate_WithInvalidWeeklyBuyRatio_ShouldThrow(
        decimal minCashRatio,
        decimal maxCashRatio,
        decimal weeklyBuyRatio,
        decimal weeklySellRatio,
        int executionDayOfWeek)
    {
        // Arrange
        var config = new StrategyConfiguration(
            minCashRatio,
            maxCashRatio,
            weeklyBuyRatio,
            weeklySellRatio,
            executionDayOfWeek);

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => config.Validate());
        ex.Message.ShouldContain("WeeklyBuyRatio must be between 0 and 1");
    }

    [Theory]
    [InlineData(0.15, 0.25, 0.05, -0.01, 5)] // Negative WeeklySellRatio
    [InlineData(0.15, 0.25, 0.05, 1.01, 5)]  // WeeklySellRatio > 1
    public void Validate_WithInvalidWeeklySellRatio_ShouldThrow(
        decimal minCashRatio,
        decimal maxCashRatio,
        decimal weeklyBuyRatio,
        decimal weeklySellRatio,
        int executionDayOfWeek)
    {
        // Arrange
        var config = new StrategyConfiguration(
            minCashRatio,
            maxCashRatio,
            weeklyBuyRatio,
            weeklySellRatio,
            executionDayOfWeek);

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => config.Validate());
        ex.Message.ShouldContain("WeeklySellRatio must be between 0 and 1");
    }

    [Theory]
    [InlineData(-1)] // Below Sunday
    [InlineData(7)]  // Above Saturday
    public void Validate_WithInvalidExecutionDayOfWeek_ShouldThrow(int executionDayOfWeek)
    {
        // Arrange
        var config = new StrategyConfiguration(
            minCashRatio: 0.15m,
            maxCashRatio: 0.25m,
            weeklyBuyRatio: 0.05m,
            weeklySellRatio: 0.10m,
            executionDayOfWeek: executionDayOfWeek);

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => config.Validate());
        ex.Message.ShouldContain("ExecutionDayOfWeek must be between 0 (Sunday) and 6 (Saturday)");
    }

    [Fact]
    public void GetEqualityComponents_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var config1 = new StrategyConfiguration(
            minCashRatio: 0.15m,
            maxCashRatio: 0.25m,
            weeklyBuyRatio: 0.05m,
            weeklySellRatio: 0.10m,
            executionDayOfWeek: 5,
            breakoutRuleConfigJson: "{\"isEnabled\":false}");

        var config2 = new StrategyConfiguration(
            minCashRatio: 0.15m,
            maxCashRatio: 0.25m,
            weeklyBuyRatio: 0.05m,
            weeklySellRatio: 0.10m,
            executionDayOfWeek: 5,
            breakoutRuleConfigJson: "{\"isEnabled\":false}");

        // Act & Assert
        config1.ShouldBe(config2);
    }

    [Fact]
    public void GetEqualityComponents_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var config1 = new StrategyConfiguration(
            minCashRatio: 0.15m,
            maxCashRatio: 0.25m,
            weeklyBuyRatio: 0.05m,
            weeklySellRatio: 0.10m,
            executionDayOfWeek: 5);

        var config2 = new StrategyConfiguration(
            minCashRatio: 0.20m, // Different value
            maxCashRatio: 0.25m,
            weeklyBuyRatio: 0.05m,
            weeklySellRatio: 0.10m,
            executionDayOfWeek: 5);

        // Act & Assert
        config1.ShouldNotBe(config2);
    }
}
