// <copyright file="BacktestingEngineTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Backtest;
using TradingBot.Core.Models.MarketData;

namespace TradingBot.Analytics.Tests;

/// <summary>
/// Unit tests for BacktestingEngine.
/// </summary>
public sealed class BacktestingEngineTests
{
    private readonly ILogger<BacktestingEngine> _logger;
    private readonly IMarketDataService _marketDataService;
    private readonly IHistoricalDataCache _cache;
    private readonly BacktestingEngine _engine;

    public BacktestingEngineTests()
    {
        _logger = A.Fake<ILogger<BacktestingEngine>>();
        _marketDataService = A.Fake<IMarketDataService>();
        _cache = A.Fake<IHistoricalDataCache>();
        _engine = new BacktestingEngine(_logger, _marketDataService, _cache);
    }

    [Fact]
    public async Task RunBacktestAsync_WithValidConfiguration_ShouldReturnResult()
    {
        // Arrange
        var config = CreateBacktestConfiguration();
        var historicalData = CreateHistoricalData(30);

        A.CallTo(() => _cache.GetAsync(
            A<string>._,
            A<DateTime>._,
            A<DateTime>._,
            A<string>._,
            A<CancellationToken>._))
            .Returns(historicalData);

        // Act
        var result = await _engine.RunBacktestAsync(config);

        // Assert
        result.ShouldNotBeNull();
        result.BacktestId.ShouldBe(config.BacktestId);
        result.StrategyName.ShouldBe(config.StrategyName);
        result.Symbol.ShouldBe(config.Symbol);
        result.InitialCapital.ShouldBe(config.InitialCapital);
        result.FinalEquity.ShouldBeGreaterThan(0m);
    }

    [Fact]
    public async Task RunBacktestAsync_WithNoHistoricalData_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var config = CreateBacktestConfiguration();

        A.CallTo(() => _cache.GetAsync(
            A<string>._,
            A<DateTime>._,
            A<DateTime>._,
            A<string>._,
            A<CancellationToken>._))
            .Returns([]);

        A.CallTo(() => _marketDataService.GetHistoricalDataAsync(
            A<string>._,
            A<DateTime>._,
            A<DateTime>._,
            A<string>._,
            A<CancellationToken>._))
            .Returns([]);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            async () => await _engine.RunBacktestAsync(config));
    }

    [Fact]
    public async Task RunBacktestAsync_ShouldCacheHistoricalData()
    {
        // Arrange
        var config = CreateBacktestConfiguration();
        var historicalData = CreateHistoricalData(20);

        A.CallTo(() => _cache.GetAsync(
            A<string>._,
            A<DateTime>._,
            A<DateTime>._,
            A<string>._,
            A<CancellationToken>._))
            .Returns((IReadOnlyList<Candle>?)null);

        A.CallTo(() => _marketDataService.GetHistoricalDataAsync(
            A<string>._,
            A<DateTime>._,
            A<DateTime>._,
            A<string>._,
            A<CancellationToken>._))
            .Returns(historicalData);

        // Act
        await _engine.RunBacktestAsync(config);

        // Assert
        A.CallTo(() => _cache.SetAsync(
            config.Symbol,
            config.StartDate,
            config.EndDate,
            "1d",
            historicalData,
            A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task RunBacktestAsync_ShouldCalculateEquityCurve()
    {
        // Arrange
        var config = CreateBacktestConfiguration();
        var historicalData = CreateHistoricalData(50);

        A.CallTo(() => _cache.GetAsync(
            A<string>._,
            A<DateTime>._,
            A<DateTime>._,
            A<string>._,
            A<CancellationToken>._))
            .Returns(historicalData);

        // Act
        var result = await _engine.RunBacktestAsync(config);

        // Assert
        result.EquityCurve.ShouldNotBeNull();
        result.EquityCurve.Count.ShouldBeGreaterThan(0);
        result.EquityCurve[0].Equity.ShouldBe(config.InitialCapital);
    }

    [Fact]
    public async Task RunBacktestAsync_ShouldCalculatePerformanceMetrics()
    {
        // Arrange
        var config = CreateBacktestConfiguration();
        var historicalData = CreateHistoricalData(40);

        A.CallTo(() => _cache.GetAsync(
            A<string>._,
            A<DateTime>._,
            A<DateTime>._,
            A<string>._,
            A<CancellationToken>._))
            .Returns(historicalData);

        // Act
        var result = await _engine.RunBacktestAsync(config);

        // Assert
        result.Performance.ShouldNotBeNull();
        result.Performance.TotalTrades.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task RunBacktestAsync_WithTransactionCosts_ShouldApplyCosts()
    {
        // Arrange
        var configWithCosts = new BacktestConfiguration
        {
            BacktestId = "test-backtest-costs",
            StrategyName = "TestStrategy",
            Symbol = "SPY",
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow,
            InitialCapital = 10000m,
            EnableTransactionCosts = true,
            CommissionPerTrade = 5m,
            SlippagePercent = 0.1m,
        };

        var configWithoutCosts = new BacktestConfiguration
        {
            BacktestId = "test-backtest-no-costs",
            StrategyName = "TestStrategy",
            Symbol = "SPY",
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow,
            InitialCapital = 10000m,
            EnableTransactionCosts = false,
        };

        var historicalData = CreateHistoricalData(30);

        A.CallTo(() => _cache.GetAsync(
            A<string>._,
            A<DateTime>._,
            A<DateTime>._,
            A<string>._,
            A<CancellationToken>._))
            .Returns(historicalData);

        // Act
        var resultWithCosts = await _engine.RunBacktestAsync(configWithCosts);
        var resultWithoutCosts = await _engine.RunBacktestAsync(configWithoutCosts);

        // Assert
        // With transaction costs, final equity should typically be lower
        // (This assumes the strategy makes some trades)
        resultWithCosts.FinalEquity.ShouldBeLessThanOrEqualTo(resultWithoutCosts.FinalEquity);
    }

    [Fact]
    public async Task GetBacktestResultAsync_WithValidId_ShouldReturnResult()
    {
        // Arrange
        var config = CreateBacktestConfiguration();
        var historicalData = CreateHistoricalData(20);

        A.CallTo(() => _cache.GetAsync(
            A<string>._,
            A<DateTime>._,
            A<DateTime>._,
            A<string>._,
            A<CancellationToken>._))
            .Returns(historicalData);

        await _engine.RunBacktestAsync(config);

        // Act
        var result = await _engine.GetBacktestResultAsync(config.BacktestId);

        // Assert
        result.ShouldNotBeNull();
        result.BacktestId.ShouldBe(config.BacktestId);
    }

    [Fact]
    public async Task GetBacktestResultAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _engine.GetBacktestResultAsync("non-existent-id");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetAllBacktestResultsAsync_ShouldReturnAllResults()
    {
        // Arrange
        var config1 = CreateBacktestConfiguration("backtest-1");
        var config2 = CreateBacktestConfiguration("backtest-2");
        var historicalData = CreateHistoricalData(20);

        A.CallTo(() => _cache.GetAsync(
            A<string>._,
            A<DateTime>._,
            A<DateTime>._,
            A<string>._,
            A<CancellationToken>._))
            .Returns(historicalData);

        await _engine.RunBacktestAsync(config1);
        await _engine.RunBacktestAsync(config2);

        // Act
        var results = await _engine.GetAllBacktestResultsAsync();

        // Assert
        results.ShouldNotBeNull();
        results.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetLatestBacktestResultAsync_ShouldReturnMostRecent()
    {
        // Arrange
        var config1 = CreateBacktestConfiguration("backtest-old");
        var config2 = CreateBacktestConfiguration("backtest-new");
        var historicalData = CreateHistoricalData(20);

        A.CallTo(() => _cache.GetAsync(
            A<string>._,
            A<DateTime>._,
            A<DateTime>._,
            A<string>._,
            A<CancellationToken>._))
            .Returns(historicalData);

        await _engine.RunBacktestAsync(config1);
        await Task.Delay(100); // Ensure different timestamps
        await _engine.RunBacktestAsync(config2);

        // Act
        var latest = await _engine.GetLatestBacktestResultAsync();

        // Assert
        latest.ShouldNotBeNull();
        latest.BacktestId.ShouldBe(config2.BacktestId);
    }

    [Fact]
    public async Task RunBacktestAsync_ShouldRecordDuration()
    {
        // Arrange
        var config = CreateBacktestConfiguration();
        var historicalData = CreateHistoricalData(100);

        A.CallTo(() => _cache.GetAsync(
            A<string>._,
            A<DateTime>._,
            A<DateTime>._,
            A<string>._,
            A<CancellationToken>._))
            .Returns(historicalData);

        // Act
        var result = await _engine.RunBacktestAsync(config);

        // Assert
        result.Duration.ShouldBeGreaterThan(TimeSpan.Zero);
        result.Duration.ShouldBeLessThan(TimeSpan.FromSeconds(10)); // Should be fast
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new BacktestingEngine(null!, _marketDataService, _cache));
    }

    [Fact]
    public void Constructor_WithNullMarketDataService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new BacktestingEngine(_logger, null!, _cache));
    }

    [Fact]
    public void Constructor_WithNullCache_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new BacktestingEngine(_logger, _marketDataService, null!));
    }

    // Helper methods
    private BacktestConfiguration CreateBacktestConfiguration(string? backtestId = null)
    {
        return new BacktestConfiguration
        {
            BacktestId = backtestId ?? "test-backtest",
            StrategyName = "TestStrategy",
            Symbol = "SPY",
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow,
            InitialCapital = 10000m,
            EnableTransactionCosts = false,
            CommissionPerTrade = 0m,
            SlippagePercent = 0m,
        };
    }

    private IReadOnlyList<Candle> CreateHistoricalData(int days)
    {
        var candles = new List<Candle>();
        var startDate = DateTime.UtcNow.AddDays(-days);
        var basePrice = 100m;
        var random = new Random(42);

        for (int i = 0; i < days; i++)
        {
            var change = (decimal)((random.NextDouble() * 4) - 2) / 100m; // -2% to +2%
            basePrice *= 1m + change;

            var open = basePrice;
            var high = basePrice * 1.02m;
            var low = basePrice * 0.98m;
            var close = basePrice;

            candles.Add(new Candle
            {
                Symbol = "SPY",
                Timestamp = startDate.AddDays(i),
                Timeframe = "1d",
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = random.Next(1000000, 10000000),
            });
        }

        return candles;
    }
}
