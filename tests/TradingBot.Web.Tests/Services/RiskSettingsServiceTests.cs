// <copyright file="RiskSettingsServiceTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using FakeItEasy;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Shouldly;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Configuration;
using TradingBot.Web.Hubs;
using TradingBot.Web.Services;
using Xunit;

namespace TradingBot.Web.Tests.Services;

/// <summary>
/// Unit tests for RiskSettingsService.
/// </summary>
public class RiskSettingsServiceTests
{
    private readonly IRiskSettingsRepository _fakeRepository;
    private readonly IHubContext<TradingHub, ITradingClient> _fakeHubContext;
    private readonly ILogger<RiskSettingsService> _fakeLogger;
    private readonly RiskSettingsService _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="RiskSettingsServiceTests"/> class.
    /// </summary>
    public RiskSettingsServiceTests()
    {
        _fakeRepository = A.Fake<IRiskSettingsRepository>();
        _fakeHubContext = A.Fake<IHubContext<TradingHub, ITradingClient>>();
        _fakeLogger = A.Fake<ILogger<RiskSettingsService>>();

        _sut = new RiskSettingsService(
            _fakeRepository,
            _fakeHubContext,
            _fakeLogger);
    }

    /// <summary>
    /// Tests that SaveRiskSettingsAsync returns true for valid settings.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task SaveRiskSettingsAsync_ValidSettings_ReturnsTrue()
    {
        // Arrange
        var validSettings = new RiskSettings
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            MaxPositionSizePercent = 15m,
            StopLossPercent = 2.5m,
            TakeProfitPercent = 6m,
            MaxOpenPositions = 8,
            MaxDailyLossPercent = 4m,
            LastModified = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
        };

        A.CallTo(() => _fakeRepository.UpdateAsync(A<RiskSettings>._, A<CancellationToken>._))
            .Returns(Task.CompletedTask);

        var fakeClients = A.Fake<IHubClients<ITradingClient>>();
        var fakeClient = A.Fake<ITradingClient>();
        A.CallTo(() => _fakeHubContext.Clients).Returns(fakeClients);
        A.CallTo(() => fakeClients.All).Returns(fakeClient);

        // Act
        var result = await _sut.SaveRiskSettingsAsync(validSettings);

        // Assert
        result.ShouldBeTrue();
        A.CallTo(() => _fakeRepository.UpdateAsync(
            A<RiskSettings>.That.Matches(s => s.Id == validSettings.Id),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => fakeClient.ReceiveRiskSettingsUpdate(A<RiskSettings>._))
            .MustHaveHappenedOnceExactly();
    }

    /// <summary>
    /// Tests that SaveRiskSettingsAsync returns false for invalid range values.
    /// </summary>
    /// <param name="maxPositionSize">Max position size percent.</param>
    /// <param name="stopLoss">Stop loss percent.</param>
    /// <param name="takeProfit">Take profit percent.</param>
    /// <param name="maxOpenPositions">Max open positions.</param>
    /// <param name="maxDailyLoss">Max daily loss percent.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Theory]
    [InlineData(0, 2, 5, 5, 5)]        // MaxPositionSize too low
    [InlineData(150, 2, 5, 5, 5)]      // MaxPositionSize too high
    [InlineData(10, 0, 5, 5, 5)]       // StopLoss too low
    [InlineData(10, 60, 5, 5, 5)]      // StopLoss too high
    [InlineData(10, 2, 0, 5, 5)]       // TakeProfit too low
    [InlineData(10, 2, 150, 5, 5)]     // TakeProfit too high
    [InlineData(10, 2, 5, 0, 5)]       // MaxOpenPositions too low
    [InlineData(10, 2, 5, 150, 5)]     // MaxOpenPositions too high
    [InlineData(10, 2, 5, 5, 0)]       // MaxDailyLoss too low
    [InlineData(10, 2, 5, 5, 150)]     // MaxDailyLoss too high
    public async Task SaveRiskSettingsAsync_InvalidRange_ReturnsFalse(
        decimal maxPositionSize,
        decimal stopLoss,
        decimal takeProfit,
        int maxOpenPositions,
        decimal maxDailyLoss)
    {
        // Arrange
        var invalidSettings = new RiskSettings
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            MaxPositionSizePercent = maxPositionSize,
            StopLossPercent = stopLoss,
            TakeProfitPercent = takeProfit,
            MaxOpenPositions = maxOpenPositions,
            MaxDailyLossPercent = maxDailyLoss,
            LastModified = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
        };

        // Act
        var result = await _sut.SaveRiskSettingsAsync(invalidSettings);

        // Assert
        result.ShouldBeFalse();
        A.CallTo(() => _fakeRepository.UpdateAsync(A<RiskSettings>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    /// <summary>
    /// Tests that ResetToDefaultsAsync returns default values.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ResetToDefaultsAsync_ReturnsDefaultValues()
    {
        // Arrange
        A.CallTo(() => _fakeRepository.UpdateAsync(A<RiskSettings>._, A<CancellationToken>._))
            .Returns(Task.CompletedTask);

        var fakeClients = A.Fake<IHubClients<ITradingClient>>();
        var fakeClient = A.Fake<ITradingClient>();
        A.CallTo(() => _fakeHubContext.Clients).Returns(fakeClients);
        A.CallTo(() => fakeClients.All).Returns(fakeClient);

        // Act
        var result = await _sut.ResetToDefaultsAsync();

        // Assert
        result.ShouldNotBeNull();
        result.MaxPositionSizePercent.ShouldBe(10m);
        result.StopLossPercent.ShouldBe(2m);
        result.TakeProfitPercent.ShouldBe(5m);
        result.MaxOpenPositions.ShouldBe(5);
        result.MaxDailyLossPercent.ShouldBe(5m);

        A.CallTo(() => _fakeRepository.UpdateAsync(
            A<RiskSettings>.That.Matches(s =>
                s.MaxPositionSizePercent == 10m &&
                s.StopLossPercent == 2m &&
                s.TakeProfitPercent == 5m &&
                s.MaxOpenPositions == 5 &&
                s.MaxDailyLossPercent == 5m),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }
}
