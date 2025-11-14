// <copyright file="StrategyManagementServiceTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using FakeItEasy;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Shouldly;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Configuration;
using TradingBot.Web.Hubs;
using TradingBot.Web.Models;
using TradingBot.Web.Services;
using Xunit;

namespace TradingBot.Web.Tests.Services;

/// <summary>
/// Unit tests for StrategyManagementService.
/// </summary>
public class StrategyManagementServiceTests
{
    private readonly IEnumerable<IStrategy> _fakeStrategies;
    private readonly IStrategyConfigurationRepository _fakeRepository;
    private readonly IHubContext<TradingHub, ITradingClient> _fakeHubContext;
    private readonly ILogger<StrategyManagementService> _fakeLogger;
    private readonly StrategyManagementService _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="StrategyManagementServiceTests"/> class.
    /// </summary>
    public StrategyManagementServiceTests()
    {
        var fakeStrategy = A.Fake<IStrategy>();
        A.CallTo(() => fakeStrategy.Name).Returns("MomentumStrategy");
        _fakeStrategies = new List<IStrategy> { fakeStrategy };

        _fakeRepository = A.Fake<IStrategyConfigurationRepository>();
        _fakeHubContext = A.Fake<IHubContext<TradingHub, ITradingClient>>();
        _fakeLogger = A.Fake<ILogger<StrategyManagementService>>();

        _sut = new StrategyManagementService(
            _fakeStrategies,
            _fakeRepository,
            _fakeHubContext,
            _fakeLogger);
    }

    /// <summary>
    /// Tests that ConfigureStrategyAsync returns false for strategy not found.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ConfigureStrategyAsync_StrategyNotFound_ReturnsFalse()
    {
        // Arrange
        var strategyName = "NonExistentStrategy";
        var parameters = new Dictionary<string, object>
        {
            { "FastPeriod", 10 },
            { "SlowPeriod", 20 },
            { "SignalPeriod", 9 },
        };

        // Act
        var result = await _sut.ConfigureStrategyAsync(strategyName, parameters);

        // Assert
        result.ShouldBeFalse();
        A.CallTo(() => _fakeRepository.UpsertAsync(
            A<StrategyConfiguration>._,
            A<CancellationToken>._)).MustNotHaveHappened();
    }

    /// <summary>
    /// Tests that ConfigureStrategyAsync returns false for invalid parameter.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ConfigureStrategyAsync_InvalidParameter_ReturnsFalse()
    {
        // Arrange
        var strategyName = "NonExistentStrategy";
        var parameters = new Dictionary<string, object>
        {
            { "InvalidParam", "value" },
        };

        // Act
        var result = await _sut.ConfigureStrategyAsync(strategyName, parameters);

        // Assert
        result.ShouldBeFalse();
        A.CallTo(() => _fakeRepository.UpsertAsync(
            A<StrategyConfiguration>._,
            A<CancellationToken>._)).MustNotHaveHappened();
    }

    /// <summary>
    /// Tests that GetStrategyParametersAsync returns metadata.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetStrategyParametersAsync_ReturnsMetadata()
    {
        // Arrange
        var strategyName = "MomentumStrategy";

        // Act
        var result = await _sut.GetStrategyParametersAsync(strategyName);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<StrategyParameterDto>>();
    }

    /// <summary>
    /// Tests that ResetStrategyToDefaultsAsync resets configuration.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ResetStrategyToDefaultsAsync_DeletesConfiguration()
    {
        // Arrange
        var strategyName = "MomentumStrategy";

        A.CallTo(() => _fakeRepository.DeleteAsync(
            A<string>._,
            A<CancellationToken>._)).Returns(true);

        var fakeClients = A.Fake<IHubClients<ITradingClient>>();
        var fakeClient = A.Fake<ITradingClient>();
        A.CallTo(() => _fakeHubContext.Clients).Returns(fakeClients);
        A.CallTo(() => fakeClients.All).Returns(fakeClient);

        // Act
        var result = await _sut.ResetStrategyToDefaultsAsync(strategyName);

        // Assert
        result.ShouldBeTrue();
        A.CallTo(() => _fakeRepository.DeleteAsync(
            strategyName,
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }
}
