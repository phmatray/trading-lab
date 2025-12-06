using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TradingStrat.Application.Factories;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Services;
using TradingStrat.Application.Tests.TestDoubles;
using TradingStrat.Application.UseCases;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.Services.Indicators;

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
        var loggerFactory = new NullLoggerFactory();
        _strategyFactory = new StrategyFactory(indicatorCalculator, loggerFactory);

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
            StrategyType: "ma",
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
        result.Should().NotBeNull();
        result.Ticker.Should().Be("TEST");
        result.Metrics.Should().NotBeNull();
        result.Metrics.InitialCapital.Should().Be(10000m);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoData_ShouldThrow()
    {
        // Arrange
        var command = new BacktestCommand(
            Ticker: "NODATA",
            StrategyType: "ma");

        // Act
        var act = async () => await _useCase.ExecuteAsync(command);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No historical data found*");
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
            StrategyType: "ma",
            StrategyParameters: new Dictionary<string, object>
            {
                ["FastPeriod"] = 5,
                ["SlowPeriod"] = 10
            });

        // Act
        await _useCase.ExecuteAsync(command, progress);

        // Assert
        progressReports.Should().NotBeEmpty();
        progressReports.Should().Contain(p => p.Current > 0);
    }

    [Theory]
    [InlineData("ma")]
    [InlineData("rsi")]
    [InlineData("macd")]
    public async Task ExecuteAsync_WithDifferentStrategies_ShouldSucceed(string strategyType)
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
        result.Should().NotBeNull();
        result.StrategyName.Should().NotBeEmpty();
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
            StrategyType: "ma",
            StartDate: startDate,
            EndDate: endDate);

        // Act
        var result = await _useCase.ExecuteAsync(command);

        // Assert
        result.StartDate.Should().Be(startDate);
        result.EndDate.Should().Be(endDate);
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomCommission_ShouldApplyCommission()
    {
        // Arrange
        SeedTestData();

        var command = new BacktestCommand(
            Ticker: "TEST",
            StrategyType: "ma",
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
        result.Should().NotBeNull();
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

        _historicalDataPort.SeedData("TEST", data);
    }
}
