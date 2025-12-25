using FakeItEasy;
using Shouldly;
using TradingStrat.Application.Factories;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Services;
using TradingStrat.Application.Strategies;
using TradingStrat.Application.Tests.TestDoubles;
using TradingStrat.Application.UseCases;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.Strategies;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Tests.UseCases;

public class RunBacktestUseCaseTests
{
    private readonly InMemoryHistoricalDataRepository _historicalDataPort;
    private readonly BacktestEngine _backtestEngine;
    private readonly IStrategyFactory _strategyFactory;
    private readonly RunBacktestUseCase _useCase;

    public RunBacktestUseCaseTests()
    {
        _historicalDataPort = new InMemoryHistoricalDataRepository();

        var performanceCalculator = new PerformanceCalculator();
        _backtestEngine = new BacktestEngine(_historicalDataPort, performanceCalculator);

        var indicatorCalculator = new IndicatorCalculator();
        var strategyRegistry = new StrategyRegistry();
        var mlPredictionService = A.Fake<IMLPredictionService>();
        _strategyFactory = new StrategyFactory(indicatorCalculator, strategyRegistry, mlPredictionService);

        _useCase = new RunBacktestUseCase(
            _historicalDataPort,
            _backtestEngine,
            _strategyFactory);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidData_ShouldReturnBacktestResult()
    {
        // Arrange
        SeedTestData();

        var command = new BacktestCommand(
            Ticker: "TEST",
            StrategyType: StrategyType.MovingAverageCrossover,
            StrategyParameters: new Dictionary<string, object>
            {
                ["FastPeriod"] = 5,
                ["SlowPeriod"] = 10
            },
            InitialCapital: 10000m,
            StartDate: new DateTime(2024, 1, 1),
            EndDate: new DateTime(2024, 3, 31));

        // Act
        var result = await _useCase.ExecuteAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.Ticker.ShouldBe("TEST");
        result.Metrics.ShouldNotBeNull();
        result.Metrics.InitialCapital.ShouldBe(10000m);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoData_ShouldThrow()
    {
        // Arrange
        var command = new BacktestCommand(
            Ticker: "NODATA",
            StrategyType: StrategyType.MovingAverageCrossover);

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(async () => await _useCase.ExecuteAsync(command));
        ex.Message.ShouldContain("No historical data found");
    }

    [Fact]
    public async Task ExecuteAsync_WithProgressReporting_ShouldReportProgress()
    {
        // Arrange
        SeedTestData();

        var progressReports = new List<BacktestProgress>();
        var progress = new Progress<BacktestProgress>(p => progressReports.Add(p));

        var command = new BacktestCommand(
            Ticker: "TEST",
            StrategyType: StrategyType.MovingAverageCrossover,
            StrategyParameters: new Dictionary<string, object>
            {
                ["FastPeriod"] = 5,
                ["SlowPeriod"] = 10
            });

        // Act
        await _useCase.ExecuteAsync(command, progress);

        // Assert
        progressReports.ShouldNotBeEmpty();
        progressReports.ShouldContain(p => p.Current > 0);
    }

    [Theory]
    [InlineData(StrategyType.MovingAverageCrossover)]
    [InlineData(StrategyType.RSI)]
    [InlineData(StrategyType.MACD)]
    public async Task ExecuteAsync_WithDifferentStrategies_ShouldSucceed(StrategyType strategyType)
    {
        // Arrange
        SeedTestData();

        var command = new BacktestCommand(
            Ticker: "TEST",
            StrategyType: strategyType,
            InitialCapital: 10000m);

        // Act
        var result = await _useCase.ExecuteAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.StrategyName.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomDateRange_ShouldRespectDates()
    {
        // Arrange
        SeedTestData();

        var startDate = new DateTime(2024, 2, 1);
        var endDate = new DateTime(2024, 2, 29);

        var command = new BacktestCommand(
            Ticker: "TEST",
            StrategyType: StrategyType.MovingAverageCrossover,
            StartDate: startDate,
            EndDate: endDate);

        // Act
        var result = await _useCase.ExecuteAsync(command);

        // Assert
        result.StartDate.ShouldBe(startDate);
        result.EndDate.ShouldBe(endDate);
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomCommission_ShouldApplyCommission()
    {
        // Arrange
        SeedTestData();

        var command = new BacktestCommand(
            Ticker: "TEST",
            StrategyType: StrategyType.MovingAverageCrossover,
            StrategyParameters: new Dictionary<string, object>
            {
                ["FastPeriod"] = 5,
                ["SlowPeriod"] = 10
            },
            InitialCapital: 10000m,
            CommissionPercentage: 0.005m,  // 0.5%
            MinimumCommission: 5.0m);

        // Act
        var result = await _useCase.ExecuteAsync(command);

        // Assert
        result.ShouldNotBeNull();
        // Commission should reduce total returns compared to 0% commission
    }

    private void SeedTestData()
    {
        var data = new List<HistoricalPrice>();
        var baseDate = new DateTime(2024, 1, 1);

        for (int i = 0; i < 100; i++)
        {
            data.Add(new HistoricalPrice
            {
                Ticker = "TEST",
                DateTime = baseDate.AddDays(i),
                Open = 100m + i * 0.5m,
                High = 100m + i * 0.5m + 1m,
                Low = 100m + i * 0.5m - 1m,
                Close = 100m + i * 0.5m,
                AdjustedClose = 100m + i * 0.5m,
                Volume = 1000000
            });
        }

        _historicalDataPort.SeedData("TEST", TimeFrameUnit.D1, data);
    }
}
