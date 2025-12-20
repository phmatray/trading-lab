using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradingStrat.Application.Factories;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Services;
using TradingStrat.Application.Tests.TestDoubles;
using TradingStrat.Application.UseCases;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Tests.UseCases;

public class RunParameterOptimizationUseCaseTests
{
    private readonly InMemoryHistoricalDataRepository _historicalDataPort;
    private readonly RunParameterOptimizationUseCase _useCase;

    public RunParameterOptimizationUseCaseTests()
    {
        _historicalDataPort = new InMemoryHistoricalDataRepository();

        PerformanceCalculator performanceCalculator = new PerformanceCalculator();
        BacktestEngine backtestEngine = new BacktestEngine(_historicalDataPort, performanceCalculator);

        IndicatorCalculator indicatorCalculator = new IndicatorCalculator();
        NullLoggerFactory loggerFactory = new NullLoggerFactory();
        StrategyFactory strategyFactory = new StrategyFactory(indicatorCalculator, loggerFactory);

        _useCase = new RunParameterOptimizationUseCase(
            _historicalDataPort,
            backtestEngine,
            strategyFactory);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidVariants_ShouldReturnComparison()
    {
        // Arrange
        SeedTestData();

        StrategyVariant variantA = new StrategyVariant(
            "Variant A",
            "ma",
            new Dictionary<string, object> { ["FastPeriod"] = 5, ["SlowPeriod"] = 10 },
            "Fast (5/10)");

        StrategyVariant variantB = new StrategyVariant(
            "Variant B",
            "ma",
            new Dictionary<string, object> { ["FastPeriod"] = 10, ["SlowPeriod"] = 20 },
            "Slow (10/20)");

        ParameterOptimizationCommand command = new ParameterOptimizationCommand(
            "TEST",
            variantA,
            variantB);

        // Act
        ParameterOptimizationResult result = await _useCase.ExecuteAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.Comparison.ShouldNotBeNull();
        result.Comparison.VariantA.ShouldBe(variantA);
        result.Comparison.VariantB.ShouldBe(variantB);
        result.Comparison.ResultA.ShouldNotBeNull();
        result.Comparison.ResultB.ShouldNotBeNull();
        result.Comparison.Ranking.ShouldNotBeNull();
        result.ExecutionTime.ShouldBeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoData_ShouldThrow()
    {
        // Arrange
        StrategyVariant variantA = new StrategyVariant("A", "ma", new Dictionary<string, object>(), "Test");
        StrategyVariant variantB = new StrategyVariant("B", "ma", new Dictionary<string, object>(), "Test");

        ParameterOptimizationCommand command = new ParameterOptimizationCommand("NODATA", variantA, variantB);

        // Act & Assert
        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
            async () => await _useCase.ExecuteAsync(command));
        ex.Message.ShouldContain("No historical data found");
    }

    [Fact]
    public async Task ExecuteAsync_WithProgressReporting_ShouldReportBothVariants()
    {
        // Arrange
        SeedTestData();

        StrategyVariant variantA = new StrategyVariant("Variant A", "ma",
            new Dictionary<string, object> { ["FastPeriod"] = 5, ["SlowPeriod"] = 10 }, "Fast");
        StrategyVariant variantB = new StrategyVariant("Variant B", "ma",
            new Dictionary<string, object> { ["FastPeriod"] = 10, ["SlowPeriod"] = 20 }, "Slow");

        ParameterOptimizationCommand command = new ParameterOptimizationCommand("TEST", variantA, variantB);

        List<OptimizationProgress> progressReports = new List<OptimizationProgress>();
        Progress<OptimizationProgress> progress = new Progress<OptimizationProgress>(p => progressReports.Add(p));

        // Act
        await _useCase.ExecuteAsync(command, progress);

        // Assert
        progressReports.ShouldNotBeEmpty();
        progressReports.ShouldContain(p => p.CurrentVariant == "Variant A");
        progressReports.ShouldContain(p => p.CurrentVariant == "Variant B");
    }

    [Theory]
    [InlineData("ma")]
    [InlineData("rsi")]
    [InlineData("macd")]
    public async Task ExecuteAsync_WithDifferentStrategyTypes_ShouldSucceed(string strategyType)
    {
        // Arrange
        SeedTestData();

        StrategyVariant variantA = new StrategyVariant("A", strategyType, new Dictionary<string, object>(), "Variant A");
        StrategyVariant variantB = new StrategyVariant("B", strategyType, new Dictionary<string, object>(), "Variant B");

        ParameterOptimizationCommand command = new ParameterOptimizationCommand("TEST", variantA, variantB);

        // Act
        ParameterOptimizationResult result = await _useCase.ExecuteAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.Comparison.Ranking.WinnerIndex.ShouldBeInRange(0, 2);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCalculateRankingCorrectly()
    {
        // Arrange
        SeedTestData();

        StrategyVariant variantA = new StrategyVariant(
            "Variant A",
            "ma",
            new Dictionary<string, object> { ["FastPeriod"] = 5, ["SlowPeriod"] = 10 },
            "Fast");

        StrategyVariant variantB = new StrategyVariant(
            "Variant B",
            "ma",
            new Dictionary<string, object> { ["FastPeriod"] = 10, ["SlowPeriod"] = 20 },
            "Slow");

        ParameterOptimizationCommand command = new ParameterOptimizationCommand("TEST", variantA, variantB);

        // Act
        ParameterOptimizationResult result = await _useCase.ExecuteAsync(command);

        // Assert
        result.Comparison.Ranking.MetricBreakdown.ShouldContainKey("Sharpe Ratio");
        result.Comparison.Ranking.MetricBreakdown.ShouldContainKey("Annualized Return");
        result.Comparison.Ranking.MetricBreakdown.ShouldContainKey("Max Drawdown %");
        result.Comparison.Ranking.MetricBreakdown.ShouldContainKey("Win Rate");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSetComparisonMetadata()
    {
        // Arrange
        SeedTestData();

        StrategyVariant variantA = new StrategyVariant("A", "ma", new Dictionary<string, object>(), "Test A");
        StrategyVariant variantB = new StrategyVariant("B", "ma", new Dictionary<string, object>(), "Test B");

        ParameterOptimizationCommand command = new ParameterOptimizationCommand("TEST", variantA, variantB);

        // Act
        ParameterOptimizationResult result = await _useCase.ExecuteAsync(command);

        // Assert
        result.Comparison.Ticker.ShouldBe("TEST");
        result.Comparison.ComparisonDate.ShouldBeGreaterThan(DateTime.Now.AddMinutes(-1));
    }

    private void SeedTestData()
    {
        List<HistoricalPrice> data = new List<HistoricalPrice>();
        DateTime baseDate = new DateTime(2024, 1, 1);

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
