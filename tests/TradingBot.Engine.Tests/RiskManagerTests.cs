// <copyright file="RiskManagerTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;

namespace TradingBot.Engine.Tests;

public sealed class RiskManagerTests
{
    private readonly ILogger<RiskManager> _logger;
    private readonly RiskManager _riskManager;

    public RiskManagerTests()
    {
        _logger = A.Fake<ILogger<RiskManager>>();
        _riskManager = new RiskManager(_logger);
    }

    [Fact]
    public async Task GetRiskSettingsAsync_ShouldReturnDefaultSettings()
    {
        // Act
        var settings = await _riskManager.GetRiskSettingsAsync();

        // Assert
        settings.ShouldNotBeNull();
        settings.Leverage.ShouldBe(1.0m);
        settings.StopLossPercent.ShouldBe(2.0m);
        settings.TakeProfitPercent.ShouldBe(5.0m);
        settings.DailyLossLimit.ShouldBe(1000m);
        settings.MaxDrawdownPercent.ShouldBe(10.0m);
        settings.MaxPositionSizePercent.ShouldBe(10.0m);
        settings.RiskLimitsEnabled.ShouldBeTrue();
    }

    [Fact]
    public async Task SetLeverageAsync_WithValidValue_ShouldUpdateLeverage()
    {
        // Arrange
        var newLeverage = 3.5m;

        // Act
        await _riskManager.SetLeverageAsync(newLeverage);
        var settings = await _riskManager.GetRiskSettingsAsync();

        // Assert
        settings.Leverage.ShouldBe(newLeverage);
    }

    [Theory]
    [InlineData(0.5)]  // Too low
    [InlineData(11.0)] // Too high
    [InlineData(-1.0)] // Negative
    public async Task SetLeverageAsync_WithInvalidValue_ShouldThrowArgumentException(decimal invalidLeverage)
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            async () => await _riskManager.SetLeverageAsync(invalidLeverage));
    }

    [Fact]
    public async Task SetStopLossAsync_WithValidValue_ShouldUpdateStopLoss()
    {
        // Arrange
        var newStopLoss = 3.5m;

        // Act
        await _riskManager.SetStopLossAsync(newStopLoss);
        var settings = await _riskManager.GetRiskSettingsAsync();

        // Assert
        settings.StopLossPercent.ShouldBe(newStopLoss);
    }

    [Theory]
    [InlineData(0.05)] // Too low
    [InlineData(25.0)] // Too high
    [InlineData(-1.0)] // Negative
    public async Task SetStopLossAsync_WithInvalidValue_ShouldThrowArgumentException(decimal invalidStopLoss)
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            async () => await _riskManager.SetStopLossAsync(invalidStopLoss));
    }

    [Fact]
    public async Task SetTakeProfitAsync_WithValidValue_ShouldUpdateTakeProfit()
    {
        // Arrange
        var newTakeProfit = 10.0m;

        // Act
        await _riskManager.SetTakeProfitAsync(newTakeProfit);
        var settings = await _riskManager.GetRiskSettingsAsync();

        // Assert
        settings.TakeProfitPercent.ShouldBe(newTakeProfit);
    }

    [Theory]
    [InlineData(0.05)] // Too low
    [InlineData(60.0)] // Too high
    public async Task SetTakeProfitAsync_WithInvalidValue_ShouldThrowArgumentException(decimal invalidTakeProfit)
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            async () => await _riskManager.SetTakeProfitAsync(invalidTakeProfit));
    }

    [Fact]
    public async Task SetDailyLossLimitAsync_WithValidValue_ShouldUpdateLimit()
    {
        // Arrange
        var newLimit = 2500m;

        // Act
        await _riskManager.SetDailyLossLimitAsync(newLimit);
        var settings = await _riskManager.GetRiskSettingsAsync();

        // Assert
        settings.DailyLossLimit.ShouldBe(newLimit);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task SetDailyLossLimitAsync_WithInvalidValue_ShouldThrowArgumentException(decimal invalidLimit)
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            async () => await _riskManager.SetDailyLossLimitAsync(invalidLimit));
    }

    [Fact]
    public async Task SetMaxDrawdownAsync_WithValidValue_ShouldUpdateDrawdown()
    {
        // Arrange
        var newDrawdown = 15.0m;

        // Act
        await _riskManager.SetMaxDrawdownAsync(newDrawdown);
        var settings = await _riskManager.GetRiskSettingsAsync();

        // Assert
        settings.MaxDrawdownPercent.ShouldBe(newDrawdown);
    }

    [Theory]
    [InlineData(0.5)]  // Too low
    [InlineData(60.0)] // Too high
    public async Task SetMaxDrawdownAsync_WithInvalidValue_ShouldThrowArgumentException(decimal invalidDrawdown)
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            async () => await _riskManager.SetMaxDrawdownAsync(invalidDrawdown));
    }

    [Fact]
    public async Task SetMaxPositionSizeAsync_WithValidValue_ShouldUpdatePositionSize()
    {
        // Arrange
        var newPositionSize = 25.0m;

        // Act
        await _riskManager.SetMaxPositionSizeAsync(newPositionSize);
        var settings = await _riskManager.GetRiskSettingsAsync();

        // Assert
        settings.MaxPositionSizePercent.ShouldBe(newPositionSize);
    }

    [Theory]
    [InlineData(0.5)]   // Too low
    [InlineData(150.0)] // Too high
    public async Task SetMaxPositionSizeAsync_WithInvalidValue_ShouldThrowArgumentException(decimal invalidSize)
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            async () => await _riskManager.SetMaxPositionSizeAsync(invalidSize));
    }

    [Fact]
    public async Task ResetToDefaultsAsync_ShouldResetAllSettings()
    {
        // Arrange - Change all settings
        await _riskManager.SetLeverageAsync(5.0m);
        await _riskManager.SetStopLossAsync(10.0m);
        await _riskManager.SetTakeProfitAsync(20.0m);
        await _riskManager.SetDailyLossLimitAsync(5000m);
        await _riskManager.SetMaxDrawdownAsync(25.0m);
        await _riskManager.SetMaxPositionSizeAsync(50.0m);

        // Act
        await _riskManager.ResetToDefaultsAsync();
        var settings = await _riskManager.GetRiskSettingsAsync();

        // Assert - Verify defaults
        settings.Leverage.ShouldBe(1.0m);
        settings.StopLossPercent.ShouldBe(2.0m);
        settings.TakeProfitPercent.ShouldBe(5.0m);
        settings.DailyLossLimit.ShouldBe(1000m);
        settings.MaxDrawdownPercent.ShouldBe(10.0m);
        settings.MaxPositionSizePercent.ShouldBe(10.0m);
        settings.RiskLimitsEnabled.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidatePositionSizeAsync_WithValidSize_ShouldReturnTrue()
    {
        // Arrange
        var positionValue = 5000m;  // 5% of account
        var accountEquity = 100000m;

        // Act
        var isValid = await _riskManager.ValidatePositionSizeAsync(positionValue, accountEquity);

        // Assert
        isValid.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidatePositionSizeAsync_WithOversizedPosition_ShouldReturnFalse()
    {
        // Arrange
        var positionValue = 15000m;  // 15% of account (exceeds 10% default limit)
        var accountEquity = 100000m;

        // Act
        var isValid = await _riskManager.ValidatePositionSizeAsync(positionValue, accountEquity);

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidatePositionSizeAsync_WithCustomLimit_ShouldValidateAgainstCustomLimit()
    {
        // Arrange
        await _riskManager.SetMaxPositionSizeAsync(20.0m); // Set to 20%
        var positionValue = 18000m;  // 18% of account (under new limit)
        var accountEquity = 100000m;

        // Act
        var isValid = await _riskManager.ValidatePositionSizeAsync(positionValue, accountEquity);

        // Assert
        isValid.ShouldBeTrue();
    }

    [Fact]
    public async Task IsDailyLossLimitExceededAsync_WithinLimit_ShouldReturnFalse()
    {
        // Arrange
        var currentDailyLoss = -500m; // Within $1000 limit

        // Act
        var exceeded = await _riskManager.IsDailyLossLimitExceededAsync(currentDailyLoss);

        // Assert
        exceeded.ShouldBeFalse();
    }

    [Fact]
    public async Task IsDailyLossLimitExceededAsync_ExceedingLimit_ShouldReturnTrue()
    {
        // Arrange
        var currentDailyLoss = -1500m; // Exceeds $1000 limit

        // Act
        var exceeded = await _riskManager.IsDailyLossLimitExceededAsync(currentDailyLoss);

        // Assert
        exceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task IsDailyLossLimitExceededAsync_ExactlyAtLimit_ShouldReturnTrue()
    {
        // Arrange
        var currentDailyLoss = -1000m; // Exactly at $1000 limit

        // Act
        var exceeded = await _riskManager.IsDailyLossLimitExceededAsync(currentDailyLoss);

        // Assert
        exceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task IsDailyLossLimitExceededAsync_WithCustomLimit_ShouldValidateAgainstCustomLimit()
    {
        // Arrange
        await _riskManager.SetDailyLossLimitAsync(2000m); // Set to $2000
        var currentDailyLoss = -1800m; // Under new limit

        // Act
        var exceeded = await _riskManager.IsDailyLossLimitExceededAsync(currentDailyLoss);

        // Assert
        exceeded.ShouldBeFalse();
    }

    [Fact]
    public async Task MultipleSettingsUpdates_ShouldUpdateLastUpdatedTimestamp()
    {
        // Arrange
        var initialSettings = await _riskManager.GetRiskSettingsAsync();
        var initialTimestamp = initialSettings.LastUpdated;

        // Wait a bit to ensure timestamp changes
        await Task.Delay(10);

        // Act
        await _riskManager.SetLeverageAsync(2.0m);
        var updatedSettings = await _riskManager.GetRiskSettingsAsync();

        // Assert
        updatedSettings.LastUpdated.ShouldBeGreaterThan(initialTimestamp);
    }
}
