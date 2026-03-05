// <copyright file="BreakoutRuleConfigTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Shouldly;
using TradingBot.Core.ValueObjects;
using Xunit;

namespace TradingBot.Core.Tests.ValueObjects;

/// <summary>
/// Unit tests for <see cref="BreakoutRuleConfig"/> value object validation.
/// </summary>
public sealed class BreakoutRuleConfigTests
{
    [Fact]
    public void Validate_WithValidConfiguration_ShouldNotThrow()
    {
        // Arrange
        var config = new BreakoutRuleConfig(
            isEnabled: true,
            weeklyPriceIncreaseThreshold: 0.10m,
            volumeMultiplier: 1.5m,
            buyRatioMultiplier: 2.0m);

        // Act & Assert
        Should.NotThrow(() => config.Validate());
    }

    [Fact]
    public void Validate_WithDisabledRule_ShouldNotThrow()
    {
        // Arrange
        var config = new BreakoutRuleConfig(isEnabled: false);

        // Act & Assert
        Should.NotThrow(() => config.Validate());
    }

    [Theory]
    [InlineData(-0.01)] // Negative threshold
    [InlineData(1.01)]  // Threshold > 1
    public void Validate_WithInvalidWeeklyPriceIncreaseThreshold_ShouldThrow(decimal threshold)
    {
        // Arrange
        var config = new BreakoutRuleConfig(
            isEnabled: true,
            weeklyPriceIncreaseThreshold: threshold,
            volumeMultiplier: 1.5m,
            buyRatioMultiplier: 2.0m);

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => config.Validate());
        ex.Message.ShouldContain("WeeklyPriceIncreaseThreshold must be between 0 and 1");
    }

    [Theory]
    [InlineData(0.0)]   // Zero
    [InlineData(-1.0)]  // Negative
    public void Validate_WithInvalidVolumeMultiplier_ShouldThrow(decimal volumeMultiplier)
    {
        // Arrange
        var config = new BreakoutRuleConfig(
            isEnabled: true,
            weeklyPriceIncreaseThreshold: 0.10m,
            volumeMultiplier: volumeMultiplier,
            buyRatioMultiplier: 2.0m);

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => config.Validate());
        ex.Message.ShouldContain("VolumeMultiplier must be positive");
    }

    [Theory]
    [InlineData(0.5)]  // Less than 1
    [InlineData(1.0)]  // Exactly 1 (must be > 1)
    public void Validate_WithInvalidBuyRatioMultiplier_ShouldThrow(decimal buyRatioMultiplier)
    {
        // Arrange
        var config = new BreakoutRuleConfig(
            isEnabled: true,
            weeklyPriceIncreaseThreshold: 0.10m,
            volumeMultiplier: 1.5m,
            buyRatioMultiplier: buyRatioMultiplier);

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => config.Validate());
        ex.Message.ShouldContain("BuyRatioMultiplier must be greater than 1");
    }

    [Fact]
    public void GetEqualityComponents_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var config1 = new BreakoutRuleConfig(
            isEnabled: true,
            weeklyPriceIncreaseThreshold: 0.10m,
            volumeMultiplier: 1.5m,
            buyRatioMultiplier: 2.0m);

        var config2 = new BreakoutRuleConfig(
            isEnabled: true,
            weeklyPriceIncreaseThreshold: 0.10m,
            volumeMultiplier: 1.5m,
            buyRatioMultiplier: 2.0m);

        // Act & Assert
        config1.ShouldBe(config2);
    }

    [Fact]
    public void GetEqualityComponents_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var config1 = new BreakoutRuleConfig(
            isEnabled: true,
            weeklyPriceIncreaseThreshold: 0.10m,
            volumeMultiplier: 1.5m,
            buyRatioMultiplier: 2.0m);

        var config2 = new BreakoutRuleConfig(
            isEnabled: false, // Different value
            weeklyPriceIncreaseThreshold: 0.10m,
            volumeMultiplier: 1.5m,
            buyRatioMultiplier: 2.0m);

        // Act & Assert
        config1.ShouldNotBe(config2);
    }

    [Fact]
    public void Constructor_WithDefaultValues_ShouldUseExpectedDefaults()
    {
        // Arrange & Act
        var config = new BreakoutRuleConfig(isEnabled: true);

        // Assert
        config.IsEnabled.ShouldBeTrue();
        config.WeeklyPriceIncreaseThreshold.ShouldBe(0.10m); // 10% default
        config.VolumeMultiplier.ShouldBe(1.5m);              // 150% of average default
        config.BuyRatioMultiplier.ShouldBe(2.0m);            // 2x default
    }
}
