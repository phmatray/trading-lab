// <copyright file="TradingHubTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using FakeItEasy;
using Microsoft.AspNetCore.SignalR;
using Shouldly;
using TradingBot.Web.Hubs;
using TradingBot.Web.Models;

namespace TradingBot.Web.Tests.Hubs;

/// <summary>
/// Unit tests for TradingHub SignalR hub.
/// </summary>
public class TradingHubTests
{
    /// <summary>
    /// Tests that SendStrategyStateUpdate method can be called on hub context.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendStrategyStateUpdate_WithValidState_BroadcastsToAllClients()
    {
        // Arrange
        var mockClients = A.Fake<IHubCallerClients>();
        var mockClientProxy = A.Fake<IClientProxy>();
        var mockContext = A.Fake<IHubContext<TradingHub>>();

        A.CallTo(() => mockContext.Clients).Returns(mockClients);
        A.CallTo(() => mockClients.All).Returns(mockClientProxy);

        var strategyState = new StrategyStateDto
        {
            StrategyId = Guid.NewGuid(),
            Name = "Weekly Cash Strategy",
            EtpSymbol = "BTCW",
            UnderlyingSymbol = "COIN",
            IsEnabled = true,
            CurrentUnderlyingPrice = 150.00m,
            CurrentEtpPrice = 45.00m,
            CurrentMA20 = 145.00m,
            DaysBelowMA20 = 0,
            CurrentCashRatio = 0.20m,
            MinCashRatio = 0.15m,
            MaxCashRatio = 0.25m,
            IsBuyConditionMet = true,
            IsSellConditionMet = false,
            IsBullish = true,
            ExecutionDayOfWeek = 5,
            NextScheduledExecution = DateTime.UtcNow.AddDays(1),
        };

        // Act
        await mockContext.Clients.All.SendCoreAsync(
            "ReceiveStrategyStateUpdate",
            new object[] { strategyState },
            default);

        // Assert
        A.CallTo(() => mockContext.Clients.All.SendCoreAsync(
            "ReceiveStrategyStateUpdate",
            A<object[]>.That.Matches(args =>
                args.Length == 1 &&
                args[0] is StrategyStateDto state &&
                state.StrategyId == strategyState.StrategyId),
            A<CancellationToken>._))
            .MustHaveHappened();
    }

    /// <summary>
    /// Tests that multiple strategy states can be broadcast.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendStrategyStateUpdate_MultipleStrategies_BroadcastsEachState()
    {
        // Arrange
        var mockClients = A.Fake<IHubCallerClients>();
        var mockClientProxy = A.Fake<IClientProxy>();
        var mockContext = A.Fake<IHubContext<TradingHub>>();

        A.CallTo(() => mockContext.Clients).Returns(mockClients);
        A.CallTo(() => mockClients.All).Returns(mockClientProxy);

        var strategy1 = new StrategyStateDto
        {
            StrategyId = Guid.NewGuid(),
            Name = "Weekly Cash Strategy",
            EtpSymbol = "BTCW",
            UnderlyingSymbol = "COIN",
            IsEnabled = true,
            MinCashRatio = 0.15m,
            MaxCashRatio = 0.25m,
            DaysBelowMA20 = 0,
            ExecutionDayOfWeek = 5,
        };

        var strategy2 = new StrategyStateDto
        {
            StrategyId = Guid.NewGuid(),
            Name = "Weekly Cash Strategy",
            EtpSymbol = "ETHW",
            UnderlyingSymbol = "ETH",
            IsEnabled = true,
            MinCashRatio = 0.15m,
            MaxCashRatio = 0.25m,
            DaysBelowMA20 = 0,
            ExecutionDayOfWeek = 5,
        };

        // Act
        await mockContext.Clients.All.SendCoreAsync(
            "ReceiveStrategyStateUpdate",
            new object[] { strategy1 },
            default);

        await mockContext.Clients.All.SendCoreAsync(
            "ReceiveStrategyStateUpdate",
            new object[] { strategy2 },
            default);

        // Assert
        A.CallTo(() => mockContext.Clients.All.SendCoreAsync(
            "ReceiveStrategyStateUpdate",
            A<object[]>._,
            A<CancellationToken>._))
            .MustHaveHappenedTwiceExactly();
    }

    /// <summary>
    /// Tests that strategy state with null values can be broadcast.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendStrategyStateUpdate_WithNullValues_BroadcastsSuccessfully()
    {
        // Arrange
        var mockClients = A.Fake<IHubCallerClients>();
        var mockClientProxy = A.Fake<IClientProxy>();
        var mockContext = A.Fake<IHubContext<TradingHub>>();

        A.CallTo(() => mockContext.Clients).Returns(mockClients);
        A.CallTo(() => mockClients.All).Returns(mockClientProxy);

        var strategyState = new StrategyStateDto
        {
            StrategyId = Guid.NewGuid(),
            Name = "Weekly Cash Strategy",
            EtpSymbol = "BTCW",
            UnderlyingSymbol = "COIN",
            IsEnabled = false,
            CurrentUnderlyingPrice = null,
            CurrentEtpPrice = null,
            CurrentMA20 = null,
            DaysBelowMA20 = 0,
            CurrentCashRatio = null,
            MinCashRatio = 0.15m,
            MaxCashRatio = 0.25m,
            IsBuyConditionMet = false,
            IsSellConditionMet = false,
            IsBullish = false,
            ExecutionDayOfWeek = 5,
            NextScheduledExecution = null,
            LastExecutionTimestamp = null,
        };

        // Act
        await mockContext.Clients.All.SendCoreAsync(
            "ReceiveStrategyStateUpdate",
            new object[] { strategyState },
            default);

        // Assert
        A.CallTo(() => mockContext.Clients.All.SendCoreAsync(
            "ReceiveStrategyStateUpdate",
            A<object[]>.That.Matches(args =>
                args.Length == 1 &&
                args[0] is StrategyStateDto state &&
                state.CurrentUnderlyingPrice == null),
            A<CancellationToken>._))
            .MustHaveHappened();
    }

    /// <summary>
    /// Tests that hub context clients property is accessible.
    /// </summary>
    [Fact]
    public void HubContext_Clients_IsAccessible()
    {
        // Arrange
        var mockClients = A.Fake<IHubCallerClients>();
        var mockContext = A.Fake<IHubContext<TradingHub>>();
        A.CallTo(() => mockContext.Clients).Returns(mockClients);

        // Act
        var clients = mockContext.Clients;

        // Assert
        clients.ShouldNotBeNull();
        clients.ShouldBe(mockClients);
    }

    /// <summary>
    /// Tests that All clients property returns client proxy.
    /// </summary>
    [Fact]
    public void HubClients_All_ReturnsClientProxy()
    {
        // Arrange
        var mockClients = A.Fake<IHubCallerClients>();
        var mockClientProxy = A.Fake<IClientProxy>();
        var mockContext = A.Fake<IHubContext<TradingHub>>();

        A.CallTo(() => mockContext.Clients).Returns(mockClients);
        A.CallTo(() => mockClients.All).Returns(mockClientProxy);

        // Act
        var all = mockContext.Clients.All;

        // Assert
        all.ShouldNotBeNull();
        all.ShouldBe(mockClientProxy);
    }
}
