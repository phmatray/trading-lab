// <copyright file="RiskManagerTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using Moq;
using TradingBot.Core.Enums;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Portfolio;
using TradingBot.Core.Models.Risk;
using TradingBot.Core.Models.Trading;
using TradingBot.Engine;
using Xunit;

namespace TradingBot.Engine.Tests;

/// <summary>
/// Unit tests for the RiskManager class.
/// </summary>
public sealed class RiskManagerTests
{
    private readonly Mock<ILogger<RiskManager>> _loggerMock;
    private readonly Mock<IPortfolioManager> _portfolioManagerMock;
    private readonly RiskManager _riskManager;

    public RiskManagerTests()
    {
        _loggerMock = new Mock<ILogger<RiskManager>>();
        _portfolioManagerMock = new Mock<IPortfolioManager>();
        _riskManager = new RiskManager(_loggerMock.Object, _portfolioManagerMock.Object);
    }

    [Fact]
    public async Task GetRiskSettingsAsync_ReturnsDefaultSettings()
    {
        // Act
        var settings = await _riskManager.GetRiskSettingsAsync();

        // Assert
        Assert.NotNull(settings);
        Assert.Equal(1.0m, settings.Leverage);
        Assert.Equal(2.0m, settings.StopLossPercent);
        Assert.Equal(5.0m, settings.TakeProfitPercent);
        Assert.True(settings.RiskLimitsEnabled);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(5.0)]
    [InlineData(10.0)]
    public async Task SetLeverageAsync_WithValidValue_UpdatesLeverage(decimal leverage)
    {
        // Act
        await _riskManager.SetLeverageAsync(leverage);
        var settings = await _riskManager.GetRiskSettingsAsync();

        // Assert
        Assert.Equal(leverage, settings.Leverage);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(11.0)]
    public async Task SetLeverageAsync_WithInvalidValue_ThrowsException(decimal leverage)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _riskManager.SetLeverageAsync(leverage));
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(2.0)]
    [InlineData(10.0)]
    public async Task SetStopLossAsync_WithValidValue_UpdatesStopLoss(decimal stopLossPercent)
    {
        // Act
        await _riskManager.SetStopLossAsync(stopLossPercent);
        var settings = await _riskManager.GetRiskSettingsAsync();

        // Assert
        Assert.Equal(stopLossPercent, settings.StopLossPercent);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(21.0)]
    public async Task SetStopLossAsync_WithInvalidValue_ThrowsException(decimal stopLossPercent)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _riskManager.SetStopLossAsync(stopLossPercent));
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(5.0)]
    [InlineData(25.0)]
    public async Task SetTakeProfitAsync_WithValidValue_UpdatesTakeProfit(decimal takeProfitPercent)
    {
        // Act
        await _riskManager.SetTakeProfitAsync(takeProfitPercent);
        var settings = await _riskManager.GetRiskSettingsAsync();

        // Assert
        Assert.Equal(takeProfitPercent, settings.TakeProfitPercent);
    }

    [Fact]
    public async Task SetDailyLossLimitAsync_WithValidValue_UpdatesLimit()
    {
        // Arrange
        var dailyLossLimit = 500m;

        // Act
        await _riskManager.SetDailyLossLimitAsync(dailyLossLimit);
        var settings = await _riskManager.GetRiskSettingsAsync();

        // Assert
        Assert.Equal(dailyLossLimit, settings.DailyLossLimit);
    }

    [Theory]
    [InlineData(5.0)]
    [InlineData(20.0)]
    [InlineData(50.0)]
    public async Task SetMaxDrawdownAsync_WithValidValue_UpdatesDrawdown(decimal maxDrawdownPercent)
    {
        // Act
        await _riskManager.SetMaxDrawdownAsync(maxDrawdownPercent);
        var settings = await _riskManager.GetRiskSettingsAsync();

        // Assert
        Assert.Equal(maxDrawdownPercent, settings.MaxDrawdownPercent);
    }

    [Fact]
    public async Task SetMaxPositionSizeAsync_WithValidValue_UpdatesPositionSize()
    {
        // Arrange
        var maxPositionSize = 15.0m;

        // Act
        await _riskManager.SetMaxPositionSizeAsync(maxPositionSize);
        var settings = await _riskManager.GetRiskSettingsAsync();

        // Assert
        Assert.Equal(maxPositionSize, settings.MaxPositionSizePercent);
    }

    [Fact]
    public async Task ResetToDefaultsAsync_ResetsAllSettings()
    {
        // Arrange - Change settings
        await _riskManager.SetLeverageAsync(5.0m);
        await _riskManager.SetStopLossAsync(5.0m);
        await _riskManager.SetTakeProfitAsync(15.0m);

        // Act
        await _riskManager.ResetToDefaultsAsync();
        var settings = await _riskManager.GetRiskSettingsAsync();

        // Assert
        Assert.Equal(1.0m, settings.Leverage);
        Assert.Equal(2.0m, settings.StopLossPercent);
        Assert.Equal(5.0m, settings.TakeProfitPercent);
    }

    [Fact]
    public async Task ValidatePositionSizeAsync_WithinLimits_ReturnsTrue()
    {
        // Arrange
        var account = new Account
        {
            AccountId = Guid.NewGuid(),
            Equity = 100000m,
            Cash = 100000m,
        };

        _portfolioManagerMock
            .Setup(x => x.GetAccountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var positionSize = 5000m; // 5% of equity

        // Act
        var isValid = await _riskManager.ValidatePositionSizeAsync(
            "SPY",
            positionSize,
            CancellationToken.None);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidatePositionSizeAsync_ExceedsMaxPositionSize_ReturnsFalse()
    {
        // Arrange
        var account = new Account
        {
            AccountId = Guid.NewGuid(),
            Equity = 100000m,
            Cash = 100000m,
        };

        _portfolioManagerMock
            .Setup(x => x.GetAccountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        await _riskManager.SetMaxPositionSizeAsync(5.0m); // 5% max
        var positionSize = 15000m; // 15% of equity - exceeds limit

        // Act
        var isValid = await _riskManager.ValidatePositionSizeAsync(
            "SPY",
            positionSize,
            CancellationToken.None);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task IsDailyLossLimitExceededAsync_WithinLimit_ReturnsFalse()
    {
        // Arrange
        var account = new Account
        {
            AccountId = Guid.NewGuid(),
            Equity = 100000m,
            Cash = 99500m, // Down $500
            RealizedPnL = -500m,
        };

        _portfolioManagerMock
            .Setup(x => x.GetAccountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        await _riskManager.SetDailyLossLimitAsync(1000m);

        // Act
        var isExceeded = await _riskManager.IsDailyLossLimitExceededAsync(CancellationToken.None);

        // Assert
        Assert.False(isExceeded);
    }

    [Fact]
    public async Task IsDailyLossLimitExceededAsync_ExceedsLimit_ReturnsTrue()
    {
        // Arrange
        var account = new Account
        {
            AccountId = Guid.NewGuid(),
            Equity = 100000m,
            Cash = 98500m, // Down $1500
            RealizedPnL = -1500m,
        };

        _portfolioManagerMock
            .Setup(x => x.GetAccountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        await _riskManager.SetDailyLossLimitAsync(1000m);

        // Act
        var isExceeded = await _riskManager.IsDailyLossLimitExceededAsync(CancellationToken.None);

        // Assert
        Assert.True(isExceeded);
    }

    [Fact]
    public async Task CalculatePositionSize_ReturnsCorrectSize()
    {
        // Arrange
        var account = new Account
        {
            AccountId = Guid.NewGuid(),
            Equity = 100000m,
        };

        _portfolioManagerMock
            .Setup(x => x.GetAccountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var price = 450.00m;
        var confidence = 0.8m;

        // Act
        var positionSize = await _riskManager.CalculatePositionSizeAsync(
            "SPY",
            price,
            confidence,
            CancellationToken.None);

        // Assert
        Assert.True(positionSize > 0);
        Assert.True(positionSize <= account.Equity * 0.1m); // Should not exceed 10% default
    }
}
