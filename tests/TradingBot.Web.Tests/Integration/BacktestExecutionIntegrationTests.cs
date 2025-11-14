// <copyright file="BacktestExecutionIntegrationTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using FakeItEasy;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Shouldly;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Backtest;
using TradingBot.Web.Hubs;
using TradingBot.Web.Models;
using TradingBot.Web.Services;
using Xunit;

namespace TradingBot.Web.Tests.Integration;

/// <summary>
/// Integration tests for backtest execution end-to-end flow.
/// </summary>
public class BacktestExecutionIntegrationTests
{
    /// <summary>
    /// Tests the end-to-end backtest execution flow.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task RunBacktest_EndToEnd_CompletesSuccessfully()
    {
        // Arrange
        var fakeRepository = A.Fake<IBacktestResultRepository>();
        var fakeTaskQueue = A.Fake<IBackgroundTaskQueue>();
        var fakeServiceProvider = A.Fake<IServiceProvider>();
        var fakeHubContext = A.Fake<IHubContext<TradingHub, ITradingClient>>();
        var fakeLogger = A.Fake<ILogger<BacktestService>>();

        var fakeClients = A.Fake<IHubClients<ITradingClient>>();
        var fakeClient = A.Fake<ITradingClient>();
        A.CallTo(() => fakeHubContext.Clients).Returns(fakeClients);
        A.CallTo(() => fakeClients.All).Returns(fakeClient);

        var backtestService = new BacktestService(
            fakeLogger,
            fakeRepository,
            fakeTaskQueue,
            fakeServiceProvider,
            fakeHubContext);

        var request = new BacktestRequest
        {
            StrategyName = "MomentumStrategy",
            Symbol = "AAPL",
            StartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            InitialCapital = 100000m,
        };

        // Act
        var backtestId = await backtestService.RunBacktestAsync(request);

        // Assert
        backtestId.ShouldNotBeNullOrEmpty();
        backtestId.ShouldStartWith("bt_");
        backtestId.ShouldContain(request.StrategyName.ToLowerInvariant());
        backtestId.ShouldContain(request.Symbol.ToLowerInvariant());
    }

    /// <summary>
    /// Tests that backtest results can be retrieved after execution.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetBacktestResults_AfterExecution_ReturnsResults()
    {
        // Arrange
        var backtestId = "bt_momentum_aapl_20240101";
        var expectedResult = new BacktestResult
        {
            BacktestId = backtestId,
            StrategyName = "MomentumStrategy",
            Symbol = "AAPL",
            StartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            InitialCapital = 100000m,
            FinalEquity = 110000m,
            SharpeRatio = 1.5m,
            MaxDrawdown = 5m,
            WinRate = 60m,
            ProfitFactor = 1.8m,
            TotalTrades = 10,
            TradesJson = "[]",
            EquityCurveJson = "[]",
            CreatedAt = DateTime.UtcNow,
        };

        var fakeRepository = A.Fake<IBacktestResultRepository>();
        var results = new List<BacktestResult> { expectedResult };
        A.CallTo(() => fakeRepository.GetAllAsync(A<CancellationToken>._))
            .Returns(results);

        var fakeTaskQueue = A.Fake<IBackgroundTaskQueue>();
        var fakeServiceProvider = A.Fake<IServiceProvider>();
        var fakeHubContext = A.Fake<IHubContext<TradingHub, ITradingClient>>();
        var fakeLogger = A.Fake<ILogger<BacktestService>>();

        var backtestService = new BacktestService(
            fakeLogger,
            fakeRepository,
            fakeTaskQueue,
            fakeServiceProvider,
            fakeHubContext);

        // Act
        var allResults = await backtestService.GetBacktestResultsAsync();

        // Assert
        allResults.ShouldNotBeNull();
        allResults.Count.ShouldBe(1);
        var result = allResults.First();
        result.BacktestId.ShouldBe(backtestId);
        result.StrategyName.ShouldBe("MomentumStrategy");
        result.Symbol.ShouldBe("AAPL");
    }

    /// <summary>
    /// Tests that backtest can be deleted successfully.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteBacktest_AfterExecution_RemovesResult()
    {
        // Arrange
        var backtestId = "bt_momentum_aapl_20240101";
        var fakeRepository = A.Fake<IBacktestResultRepository>();

        A.CallTo(() => fakeRepository.DeleteAsync(
            A<string>._,
            A<CancellationToken>._)).Returns(Task.FromResult(true));

        var fakeTaskQueue = A.Fake<IBackgroundTaskQueue>();
        var fakeServiceProvider = A.Fake<IServiceProvider>();
        var fakeHubContext = A.Fake<IHubContext<TradingHub, ITradingClient>>();
        var fakeLogger = A.Fake<ILogger<BacktestService>>();

        var backtestService = new BacktestService(
            fakeLogger,
            fakeRepository,
            fakeTaskQueue,
            fakeServiceProvider,
            fakeHubContext);

        // Act
        await backtestService.DeleteBacktestAsync(backtestId);

        // Assert
        A.CallTo(() => fakeRepository.DeleteAsync(
            backtestId,
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }
}
