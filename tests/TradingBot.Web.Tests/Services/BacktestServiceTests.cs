// <copyright file="BacktestServiceTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using FakeItEasy;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Backtest;
using TradingBot.Web.Hubs;
using TradingBot.Web.Models;
using TradingBot.Web.Services;
using Xunit;

namespace TradingBot.Web.Tests.Services;

/// <summary>
/// Unit tests for BacktestService.
/// </summary>
public class BacktestServiceTests
{
    private readonly IBacktestResultRepository _fakeRepository;
    private readonly IBackgroundTaskQueue _fakeTaskQueue;
    private readonly IServiceProvider _fakeServiceProvider;
    private readonly IHubContext<TradingHub, ITradingClient> _fakeHubContext;
    private readonly ILogger<BacktestService> _fakeLogger;
    private readonly BacktestService _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="BacktestServiceTests"/> class.
    /// </summary>
    public BacktestServiceTests()
    {
        _fakeRepository = A.Fake<IBacktestResultRepository>();
        _fakeTaskQueue = A.Fake<IBackgroundTaskQueue>();
        _fakeServiceProvider = A.Fake<IServiceProvider>();
        _fakeHubContext = A.Fake<IHubContext<TradingHub, ITradingClient>>();
        _fakeLogger = A.Fake<ILogger<BacktestService>>();

        _sut = new BacktestService(
            _fakeLogger,
            _fakeRepository,
            _fakeTaskQueue,
            _fakeServiceProvider,
            _fakeHubContext);
    }

    /// <summary>
    /// Tests that RunBacktestAsync returns a backtest ID for valid request.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task RunBacktestAsync_ValidRequest_ReturnsBacktestId()
    {
        // Arrange
        var request = new BacktestRequest
        {
            StrategyName = "MomentumStrategy",
            Symbol = "AAPL",
            StartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            InitialCapital = 100000m,
        };

        // Act
        var result = await _sut.RunBacktestAsync(request);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldStartWith("bt_");
    }

    /// <summary>
    /// Tests that ExportBacktestTradesToCsvAsync returns CSV for valid backtest.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExportBacktestTradesToCsvAsync_ValidBacktest_ReturnsCsv()
    {
        // Arrange
        var backtestId = "bt_momentum_aapl_20240101";
        var backtestResult = new BacktestResult
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
            TradesJson = "[{\"Symbol\":\"AAPL\",\"Side\":\"Buy\",\"Quantity\":100,\"EntryPrice\":150,\"ExitPrice\":160,\"EntryTime\":\"2024-01-15T10:00:00Z\",\"ExitTime\":\"2024-01-20T15:00:00Z\",\"RealizedPnL\":1000}]",
            EquityCurveJson = "[]",
            CreatedAt = DateTime.UtcNow,
        };

        A.CallTo(() => _fakeRepository.GetByIdAsync(backtestId, A<CancellationToken>._))
            .Returns(backtestResult);

        // Act
        var result = await _sut.ExportBacktestTradesToCsvAsync(backtestId);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("Symbol");
        result.ShouldContain("AAPL");
        result.ShouldContain("EntryPrice");
        result.ShouldContain("ExitPrice");
    }

    /// <summary>
    /// Tests that DeleteBacktestAsync removes backtest from repository.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteBacktestAsync_ValidBacktest_RemovesFromRepository()
    {
        // Arrange
        var backtestId = "bt_momentum_aapl_20240101";

        A.CallTo(() => _fakeRepository.DeleteAsync(backtestId, A<CancellationToken>._))
            .Returns(Task.FromResult(true));

        // Act
        await _sut.DeleteBacktestAsync(backtestId);

        // Assert
        A.CallTo(() => _fakeRepository.DeleteAsync(backtestId, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }
}
