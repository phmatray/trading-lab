using System.Text.Json;
using FakeItEasy;
using Shouldly;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.UseCases;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.Tests.UseCases;

public class GetTopStrategiesUseCaseTests
{
    private readonly IBacktestArchivePort _backtestArchivePort;
    private readonly GetTopStrategiesUseCase _useCase;

    public GetTopStrategiesUseCaseTests()
    {
        _backtestArchivePort = A.Fake<IBacktestArchivePort>();
        _useCase = new GetTopStrategiesUseCase(_backtestArchivePort);
    }

    [Fact]
    public async Task ExecuteAsync_WithDefaultLimit_Returns5TopStrategies()
    {
        // Arrange
        var backtestRuns = CreateBacktestRuns(5);
        A.CallTo(() => _backtestArchivePort.GetTopBacktestRunsAsync(5)).Returns(backtestRuns);

        // Act
        var resultWrapper = await _useCase.ExecuteAsync();

        // Assert
        resultWrapper.IsSuccess.ShouldBeTrue();
        resultWrapper.Value.Count.ShouldBe(5);
        A.CallTo(() => _backtestArchivePort.GetTopBacktestRunsAsync(5)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomLimit_ReturnsSpecifiedNumber()
    {
        // Arrange
        int customLimit = 10;
        var backtestRuns = CreateBacktestRuns(10);
        A.CallTo(() => _backtestArchivePort.GetTopBacktestRunsAsync(customLimit)).Returns(backtestRuns);

        // Act
        var resultWrapper = await _useCase.ExecuteAsync(customLimit);

        // Assert
        resultWrapper.IsSuccess.ShouldBeTrue();
        resultWrapper.Value.Count.ShouldBe(10);
        A.CallTo(() => _backtestArchivePort.GetTopBacktestRunsAsync(customLimit)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteAsync_WithNoBacktests_ReturnsEmptyList()
    {
        // Arrange
        A.CallTo(() => _backtestArchivePort.GetTopBacktestRunsAsync(5)).Returns(new List<BacktestRun>());

        // Act
        var resultWrapper = await _useCase.ExecuteAsync();

        // Assert
        resultWrapper.IsSuccess.ShouldBeTrue();
        resultWrapper.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithValidBacktests_DesializesAndMapsCorrectly()
    {
        // Arrange
        var backtestRun = new BacktestRun
        {
            Id = 1,
            StrategyName = "RSI Strategy",
            Ticker = "AAPL",
            ExecutedAt = new DateTime(2024, 12, 25),
            ResultsJson = CreateBacktestResultJson(25.5m, 1.85m, 12.3m, 42)
        };

        A.CallTo(() => _backtestArchivePort.GetTopBacktestRunsAsync(5))
            .Returns(new List<BacktestRun> { backtestRun });

        // Act
        var resultWrapper = await _useCase.ExecuteAsync();

        // Assert
        resultWrapper.IsSuccess.ShouldBeTrue();
        var result = resultWrapper.Value;
        result.Count.ShouldBe(1);
        result[0].StrategyName.ShouldBe("RSI Strategy");
        result[0].Ticker.ShouldBe("AAPL");
        result[0].TotalReturn.ShouldBe(25.5m);
        result[0].SharpeRatio.ShouldBe(1.85m);
        result[0].MaxDrawdown.ShouldBe(12.3m);
        result[0].TotalTrades.ShouldBe(42);
        result[0].LastBacktestDate.ShouldBe(new DateTime(2024, 12, 25));
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidJson_SkipsInvalidEntry()
    {
        // Arrange
        var backtestRuns = new List<BacktestRun>
        {
            new()
            {
                Id = 1,
                StrategyName = "Valid Strategy",
                Ticker = "AAPL",
                ExecutedAt = DateTime.UtcNow,
                ResultsJson = CreateBacktestResultJson(15.0m, 1.5m, 10.0m, 30)
            },
            new()
            {
                Id = 2,
                StrategyName = "Invalid Strategy",
                Ticker = "GOOGL",
                ExecutedAt = DateTime.UtcNow,
                ResultsJson = "{ invalid json }"
            },
            new()
            {
                Id = 3,
                StrategyName = "Another Valid Strategy",
                Ticker = "MSFT",
                ExecutedAt = DateTime.UtcNow,
                ResultsJson = CreateBacktestResultJson(20.0m, 1.8m, 8.5m, 35)
            }
        };

        A.CallTo(() => _backtestArchivePort.GetTopBacktestRunsAsync(5)).Returns(backtestRuns);

        // Act
        var resultWrapper = await _useCase.ExecuteAsync();

        // Assert - Should skip the invalid entry
        resultWrapper.IsSuccess.ShouldBeTrue();
        var result = resultWrapper.Value;
        result.Count.ShouldBe(2);
        result[0].StrategyName.ShouldBe("Valid Strategy");
        result[1].StrategyName.ShouldBe("Another Valid Strategy");
    }

    [Fact]
    public async Task ExecuteAsync_WithNullDeserializedResult_SkipsEntry()
    {
        // Arrange
        var backtestRun = new BacktestRun
        {
            Id = 1,
            StrategyName = "Null Result Strategy",
            Ticker = "AAPL",
            ExecutedAt = DateTime.UtcNow,
            ResultsJson = "null"
        };

        A.CallTo(() => _backtestArchivePort.GetTopBacktestRunsAsync(5))
            .Returns(new List<BacktestRun> { backtestRun });

        // Act
        var resultWrapper = await _useCase.ExecuteAsync();

        // Assert - Should skip the null result
        resultWrapper.IsSuccess.ShouldBeTrue();
        resultWrapper.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_OrdersStrategiesByPerformance()
    {
        // Arrange
        var backtestRuns = new List<BacktestRun>
        {
            new()
            {
                Id = 1,
                StrategyName = "High Performer",
                Ticker = "AAPL",
                ExecutedAt = DateTime.UtcNow,
                ResultsJson = CreateBacktestResultJson(50.0m, 2.5m, 15.0m, 50)
            },
            new()
            {
                Id = 2,
                StrategyName = "Medium Performer",
                Ticker = "GOOGL",
                ExecutedAt = DateTime.UtcNow,
                ResultsJson = CreateBacktestResultJson(25.0m, 1.5m, 10.0m, 40)
            },
            new()
            {
                Id = 3,
                StrategyName = "Low Performer",
                Ticker = "MSFT",
                ExecutedAt = DateTime.UtcNow,
                ResultsJson = CreateBacktestResultJson(10.0m, 1.0m, 8.0m, 30)
            }
        };

        A.CallTo(() => _backtestArchivePort.GetTopBacktestRunsAsync(5)).Returns(backtestRuns);

        // Act
        var resultWrapper = await _useCase.ExecuteAsync();

        // Assert - Port should return pre-ordered results
        resultWrapper.IsSuccess.ShouldBeTrue();
        var result = resultWrapper.Value;
        result.Count.ShouldBe(3);
        result[0].StrategyName.ShouldBe("High Performer");
        result[1].StrategyName.ShouldBe("Medium Performer");
        result[2].StrategyName.ShouldBe("Low Performer");
    }

    // Helper methods
    private List<BacktestRun> CreateBacktestRuns(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new BacktestRun
            {
                Id = i,
                StrategyName = $"Strategy {i}",
                Ticker = "AAPL",
                ExecutedAt = DateTime.UtcNow.AddDays(-i),
                ResultsJson = CreateBacktestResultJson(20.0m + i, 1.5m + (i * 0.1m), 10.0m - i, 30 + i)
            })
            .ToList();
    }

    private string CreateBacktestResultJson(decimal totalReturn, decimal sharpeRatio, decimal maxDrawdown, int totalTrades)
    {
        var metrics = new PerformanceMetrics(
            InitialCapital: 10000m,
            FinalEquity: 10000m + (10000m * totalReturn / 100),
            TotalReturn: 10000m * totalReturn / 100,
            TotalReturnPercentage: totalReturn,
            AnnualizedReturn: totalReturn * 0.8m,
            TotalTrades: totalTrades,
            WinningTrades: (int)(totalTrades * 0.6),
            LosingTrades: (int)(totalTrades * 0.4),
            WinRate: 60m,
            AverageWin: 150m,
            AverageLoss: 100m,
            LargestWin: 500m,
            LargestLoss: 300m,
            ProfitFactor: 1.5m,
            MaxConsecutiveWins: 5,
            MaxConsecutiveLosses: 3,
            MaxDrawdown: 10000m * maxDrawdown / 100,
            MaxDrawdownPercentage: maxDrawdown,
            SharpeRatio: sharpeRatio,
            Volatility: 15m,
            TotalDays: 365,
            DaysInMarket: 200,
            MarketExposurePercentage: 54.8m
        );

        var backtestResult = new BacktestResult(
            StrategyName: "Test Strategy",
            StrategyDescription: "Test Description",
            StrategyParameters: new Dictionary<string, object>(),
            Ticker: "AAPL",
            StartDate: DateTime.Today.AddYears(-1),
            EndDate: DateTime.Today,
            InitialCapital: 10000m,
            CommissionPercentage: 0.001m,
            MinimumCommission: 1.0m,
            Trades: new List<Trade>(),
            EquityCurve: new List<EquityPoint>(),
            Metrics: metrics
        );

        return JsonSerializer.Serialize(backtestResult);
    }
}
