using FakeItEasy;
using Shouldly;
using TradingStrat.Application.Factories;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Services;
using TradingStrat.Application.Strategies;
using TradingStrat.Application.Tests.TestDoubles;
using TradingStrat.Application.UseCases;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.Strategies;
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
        StrategyRegistry strategyRegistry = new StrategyRegistry();
        IMLPredictionService mlPredictionService = A.Fake<IMLPredictionService>();
        StrategyParameterDefaults parameterDefaults = new StrategyParameterDefaults();
        StrategyFactory strategyFactory = new StrategyFactory(
            indicatorCalculator,
            strategyRegistry,
            mlPredictionService,
            parameterDefaults);

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
            StrategyType.MovingAverageCrossover,
            new Dictionary<string, object> { ["FastPeriod"] = 5, ["SlowPeriod"] = 10 },
            "Fast (5/10)");

        StrategyVariant variantB = new StrategyVariant(
            "Variant B",
            StrategyType.MovingAverageCrossover,
            new Dictionary<string, object> { ["FastPeriod"] = 10, ["SlowPeriod"] = 20 },
            "Slow (10/20)");

        ParameterOptimizationCommand command = new ParameterOptimizationCommand(
            "TEST",
            variantA,
            variantB);

        // Act
        Result<ParameterOptimizationResult> result = await _useCase.ExecuteAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.Value.Comparison.ShouldNotBeNull();
        result.Value.Comparison.VariantA.ShouldBe(variantA);
        result.Value.Comparison.VariantB.ShouldBe(variantB);
        result.Value.Comparison.ResultA.ShouldNotBeNull();
        result.Value.Comparison.ResultB.ShouldNotBeNull();
        result.Value.Comparison.Ranking.ShouldNotBeNull();
        result.Value.ExecutionTime.ShouldBeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoData_ShouldReturnFailure()
    {
        // Arrange
        StrategyVariant variantA = new StrategyVariant("A", StrategyType.MovingAverageCrossover, new Dictionary<string, object>(), "Test");
        StrategyVariant variantB = new StrategyVariant("B", StrategyType.MovingAverageCrossover, new Dictionary<string, object>(), "Test");

        ParameterOptimizationCommand command = new ParameterOptimizationCommand("NODATA", variantA, variantB);

        // Act
        Result<ParameterOptimizationResult> result = await _useCase.ExecuteAsync(command);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("No historical data found");
        result.Errors[0].Code.ShouldBe("NO_HISTORICAL_DATA");
    }

    [Fact]
    public async Task ExecuteAsync_WithProgressReporting_ShouldReportBothVariants()
    {
        // Arrange
        SeedTestData();

        StrategyVariant variantA = new StrategyVariant("Variant A", StrategyType.MovingAverageCrossover,
            new Dictionary<string, object> { ["FastPeriod"] = 5, ["SlowPeriod"] = 10 }, "Fast");
        StrategyVariant variantB = new StrategyVariant("Variant B", StrategyType.MovingAverageCrossover,
            new Dictionary<string, object> { ["FastPeriod"] = 10, ["SlowPeriod"] = 20 }, "Slow");

        ParameterOptimizationCommand command = new ParameterOptimizationCommand("TEST", variantA, variantB);

        List<Ports.Inbound.OptimizationProgress> progressReports = new List<Ports.Inbound.OptimizationProgress>();
        Progress<Ports.Inbound.OptimizationProgress> progress = new Progress<Ports.Inbound.OptimizationProgress>(p => progressReports.Add(p));

        // Act
        await _useCase.ExecuteAsync(command, progress);

        // Assert
        progressReports.ShouldNotBeEmpty();
        progressReports.ShouldContain(p => p.CurrentVariant == "Variant A");
        progressReports.ShouldContain(p => p.CurrentVariant == "Variant B");
    }

    [Theory]
    [InlineData(StrategyType.MovingAverageCrossover)]
    [InlineData(StrategyType.RSI)]
    [InlineData(StrategyType.MACD)]
    public async Task ExecuteAsync_WithDifferentStrategyTypes_ShouldSucceed(StrategyType strategyType)
    {
        // Arrange
        SeedTestData();

        StrategyVariant variantA = new StrategyVariant("A", strategyType, new Dictionary<string, object>(), "Variant A");
        StrategyVariant variantB = new StrategyVariant("B", strategyType, new Dictionary<string, object>(), "Variant B");

        ParameterOptimizationCommand command = new ParameterOptimizationCommand("TEST", variantA, variantB);

        // Act
        Result<ParameterOptimizationResult> result = await _useCase.ExecuteAsync(command);

        // Assert
        result.Value.ShouldNotBeNull();
        result.Value.Comparison.Ranking.WinnerIndex.ShouldBeInRange(0, 2);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCalculateRankingCorrectly()
    {
        // Arrange
        SeedTestData();

        StrategyVariant variantA = new StrategyVariant(
            "Variant A",
            StrategyType.MovingAverageCrossover,
            new Dictionary<string, object> { ["FastPeriod"] = 5, ["SlowPeriod"] = 10 },
            "Fast");

        StrategyVariant variantB = new StrategyVariant(
            "Variant B",
            StrategyType.MovingAverageCrossover,
            new Dictionary<string, object> { ["FastPeriod"] = 10, ["SlowPeriod"] = 20 },
            "Slow");

        ParameterOptimizationCommand command = new ParameterOptimizationCommand("TEST", variantA, variantB);

        // Act
        Result<ParameterOptimizationResult> result = await _useCase.ExecuteAsync(command);

        // Assert
        result.Value.Comparison.Ranking.MetricBreakdown.ShouldContainKey("Sharpe Ratio");
        result.Value.Comparison.Ranking.MetricBreakdown.ShouldContainKey("Annualized Return");
        result.Value.Comparison.Ranking.MetricBreakdown.ShouldContainKey("Max Drawdown %");
        result.Value.Comparison.Ranking.MetricBreakdown.ShouldContainKey("Win Rate");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSetComparisonMetadata()
    {
        // Arrange
        SeedTestData();

        StrategyVariant variantA = new StrategyVariant("A", StrategyType.MovingAverageCrossover, new Dictionary<string, object>(), "Test A");
        StrategyVariant variantB = new StrategyVariant("B", StrategyType.MovingAverageCrossover, new Dictionary<string, object>(), "Test B");

        ParameterOptimizationCommand command = new ParameterOptimizationCommand("TEST", variantA, variantB);

        // Act
        Result<ParameterOptimizationResult> result = await _useCase.ExecuteAsync(command);

        // Assert
        result.Value.Comparison.Ticker.ShouldBe("TEST");
        result.Value.Comparison.ComparisonDate.ShouldBeGreaterThan(DateTime.Now.AddMinutes(-1));
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

        _historicalDataPort.SeedData("TEST", TimeFrameUnit.D1, data);
    }
}
