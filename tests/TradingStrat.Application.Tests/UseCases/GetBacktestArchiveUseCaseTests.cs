using System.Text.Json;
using FakeItEasy;
using Shouldly;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.UseCases;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.Tests.UseCases;

public class GetBacktestArchiveUseCaseTests
{
    private readonly IBacktestArchivePort _backtestArchivePort;
    private readonly GetBacktestArchiveUseCase _useCase;

    public GetBacktestArchiveUseCaseTests()
    {
        _backtestArchivePort = A.Fake<IBacktestArchivePort>();
        _useCase = new GetBacktestArchiveUseCase(_backtestArchivePort);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoFilters_ReturnsAllBacktests()
    {
        // Arrange
        var query = new GetBacktestArchiveQuery();
        List<BacktestRun> backtestRuns = CreateBacktestRuns(5);

        A.CallTo(() => _backtestArchivePort.GetBacktestRunsAsync(null, null, 100))
            .Returns(backtestRuns);
        A.CallTo(() => _backtestArchivePort.GetBacktestRunCountAsync()).Returns(5);
        A.CallTo(() => _backtestArchivePort.GetLastBacktestDateAsync())
            .Returns(backtestRuns[0].ExecutedAt);
        A.CallTo(() => _backtestArchivePort.GetTopBacktestRunsAsync(1))
            .Returns(new List<BacktestRun> { backtestRuns[0] });

        // Act
        Result<BacktestArchiveResult> resultWrapper = await _useCase.ExecuteAsync(query);

        // Assert
        resultWrapper.IsSuccess.ShouldBeTrue();
        BacktestArchiveResult result = resultWrapper.Value;
        result.BacktestRuns.Count.ShouldBe(5);
        result.TotalCount.ShouldBe(5);
        result.MostRecentDate.ShouldBe(backtestRuns[0].ExecutedAt);
        result.TopPerformer.ShouldNotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithTickerFilter_ReturnsFilteredResults()
    {
        // Arrange
        var query = new GetBacktestArchiveQuery(Ticker: "AAPL");
        List<BacktestRun> backtestRuns = CreateBacktestRuns(3, ticker: "AAPL");

        A.CallTo(() => _backtestArchivePort.GetBacktestRunsAsync("AAPL", null, 100))
            .Returns(backtestRuns);
        A.CallTo(() => _backtestArchivePort.GetBacktestRunCountAsync()).Returns(10);
        A.CallTo(() => _backtestArchivePort.GetLastBacktestDateAsync()).Returns(DateTime.UtcNow);
        A.CallTo(() => _backtestArchivePort.GetTopBacktestRunsAsync(1))
            .Returns(new List<BacktestRun> { backtestRuns[0] });

        // Act
        Result<BacktestArchiveResult> resultWrapper = await _useCase.ExecuteAsync(query);

        // Assert
        resultWrapper.IsSuccess.ShouldBeTrue();
        BacktestArchiveResult result = resultWrapper.Value;
        result.BacktestRuns.Count.ShouldBe(3);
        result.BacktestRuns.ShouldAllBe(s => s.Ticker == "AAPL");
    }

    [Fact]
    public async Task ExecuteAsync_WithStrategyTypeFilter_ReturnsFilteredResults()
    {
        // Arrange
        var query = new GetBacktestArchiveQuery(StrategyType: "rsi");
        List<BacktestRun> backtestRuns = CreateBacktestRuns(2, strategyType: "rsi");

        A.CallTo(() => _backtestArchivePort.GetBacktestRunsAsync(null, "rsi", 100))
            .Returns(backtestRuns);
        A.CallTo(() => _backtestArchivePort.GetBacktestRunCountAsync()).Returns(10);
        A.CallTo(() => _backtestArchivePort.GetLastBacktestDateAsync()).Returns(DateTime.UtcNow);
        A.CallTo(() => _backtestArchivePort.GetTopBacktestRunsAsync(1))
            .Returns(new List<BacktestRun> { backtestRuns[0] });

        // Act
        Result<BacktestArchiveResult> resultWrapper = await _useCase.ExecuteAsync(query);

        // Assert
        resultWrapper.IsSuccess.ShouldBeTrue();
        BacktestArchiveResult result = resultWrapper.Value;
        result.BacktestRuns.Count.ShouldBe(2);
        result.BacktestRuns.ShouldAllBe(s => s.StrategyType == "rsi");
    }

    [Fact]
    public async Task ExecuteAsync_WithDateRangeFilter_FiltersCorrectly()
    {
        // Arrange
        DateTime startDate = new(2024, 1, 1);
        DateTime endDate = new(2024, 12, 31);
        var query = new GetBacktestArchiveQuery(StartDate: startDate, EndDate: endDate);

        var backtestRuns = new List<BacktestRun>
        {
            CreateBacktestRun(id: 1, executedAt: new DateTime(2023, 12, 31)), // Before range
            CreateBacktestRun(id: 2, executedAt: new DateTime(2024, 6, 15)),  // In range
            CreateBacktestRun(id: 3, executedAt: new DateTime(2024, 12, 1)),  // In range
            CreateBacktestRun(id: 4, executedAt: new DateTime(2025, 1, 1))    // After range
        };

        A.CallTo(() => _backtestArchivePort.GetBacktestRunsAsync(null, null, 100))
            .Returns(backtestRuns);
        A.CallTo(() => _backtestArchivePort.GetBacktestRunCountAsync()).Returns(4);
        A.CallTo(() => _backtestArchivePort.GetLastBacktestDateAsync()).Returns(DateTime.UtcNow);
        A.CallTo(() => _backtestArchivePort.GetTopBacktestRunsAsync(1))
            .Returns(new List<BacktestRun> { backtestRuns[0] });

        // Act
        Result<BacktestArchiveResult> resultWrapper = await _useCase.ExecuteAsync(query);

        // Assert
        resultWrapper.IsSuccess.ShouldBeTrue();
        BacktestArchiveResult result = resultWrapper.Value;
        result.BacktestRuns.Count.ShouldBe(2);
        result.BacktestRuns.ShouldAllBe(s => s.ExecutedAt >= startDate && s.ExecutedAt <= endDate);
    }

    [Fact]
    public async Task ExecuteAsync_SortByTotalReturn_Ascending_SortsCorrectly()
    {
        // Arrange
        var query = new GetBacktestArchiveQuery(SortBy: "totalreturn", SortDescending: false);
        var backtestRuns = new List<BacktestRun>
        {
            CreateBacktestRun(id: 1, totalReturn: 30.0m),
            CreateBacktestRun(id: 2, totalReturn: 10.0m),
            CreateBacktestRun(id: 3, totalReturn: 20.0m)
        };

        A.CallTo(() => _backtestArchivePort.GetBacktestRunsAsync(null, null, 100))
            .Returns(backtestRuns);
        A.CallTo(() => _backtestArchivePort.GetBacktestRunCountAsync()).Returns(3);
        A.CallTo(() => _backtestArchivePort.GetLastBacktestDateAsync()).Returns(DateTime.UtcNow);
        A.CallTo(() => _backtestArchivePort.GetTopBacktestRunsAsync(1))
            .Returns(new List<BacktestRun> { backtestRuns[0] });

        // Act
        Result<BacktestArchiveResult> resultWrapper = await _useCase.ExecuteAsync(query);

        // Assert
        resultWrapper.IsSuccess.ShouldBeTrue();
        BacktestArchiveResult result = resultWrapper.Value;
        result.BacktestRuns.Count.ShouldBe(3);
        result.BacktestRuns[0].TotalReturnPercentage.ShouldBe(10.0m);
        result.BacktestRuns[1].TotalReturnPercentage.ShouldBe(20.0m);
        result.BacktestRuns[2].TotalReturnPercentage.ShouldBe(30.0m);
    }

    [Fact]
    public async Task ExecuteAsync_SortByTotalReturn_Descending_SortsCorrectly()
    {
        // Arrange
        var query = new GetBacktestArchiveQuery(SortBy: "totalreturn", SortDescending: true);
        var backtestRuns = new List<BacktestRun>
        {
            CreateBacktestRun(id: 1, totalReturn: 10.0m),
            CreateBacktestRun(id: 2, totalReturn: 30.0m),
            CreateBacktestRun(id: 3, totalReturn: 20.0m)
        };

        A.CallTo(() => _backtestArchivePort.GetBacktestRunsAsync(null, null, 100))
            .Returns(backtestRuns);
        A.CallTo(() => _backtestArchivePort.GetBacktestRunCountAsync()).Returns(3);
        A.CallTo(() => _backtestArchivePort.GetLastBacktestDateAsync()).Returns(DateTime.UtcNow);
        A.CallTo(() => _backtestArchivePort.GetTopBacktestRunsAsync(1))
            .Returns(new List<BacktestRun> { backtestRuns[1] });

        // Act
        Result<BacktestArchiveResult> resultWrapper = await _useCase.ExecuteAsync(query);

        // Assert
        resultWrapper.IsSuccess.ShouldBeTrue();
        BacktestArchiveResult result = resultWrapper.Value;
        result.BacktestRuns[0].TotalReturnPercentage.ShouldBe(30.0m);
        result.BacktestRuns[1].TotalReturnPercentage.ShouldBe(20.0m);
        result.BacktestRuns[2].TotalReturnPercentage.ShouldBe(10.0m);
    }

    [Fact]
    public async Task ExecuteAsync_SortBySharpeRatio_SortsCorrectly()
    {
        // Arrange
        var query = new GetBacktestArchiveQuery(SortBy: "sharperatio", SortDescending: true);
        var backtestRuns = new List<BacktestRun>
        {
            CreateBacktestRun(id: 1, sharpeRatio: 1.0m),
            CreateBacktestRun(id: 2, sharpeRatio: 2.5m),
            CreateBacktestRun(id: 3, sharpeRatio: 1.8m)
        };

        A.CallTo(() => _backtestArchivePort.GetBacktestRunsAsync(null, null, 100))
            .Returns(backtestRuns);
        A.CallTo(() => _backtestArchivePort.GetBacktestRunCountAsync()).Returns(3);
        A.CallTo(() => _backtestArchivePort.GetLastBacktestDateAsync()).Returns(DateTime.UtcNow);
        A.CallTo(() => _backtestArchivePort.GetTopBacktestRunsAsync(1))
            .Returns(new List<BacktestRun> { backtestRuns[1] });

        // Act
        Result<BacktestArchiveResult> resultWrapper = await _useCase.ExecuteAsync(query);

        // Assert
        resultWrapper.IsSuccess.ShouldBeTrue();
        BacktestArchiveResult result = resultWrapper.Value;
        result.BacktestRuns[0].SharpeRatio.ShouldBe(2.5m);
        result.BacktestRuns[1].SharpeRatio.ShouldBe(1.8m);
        result.BacktestRuns[2].SharpeRatio.ShouldBe(1.0m);
    }

    [Fact]
    public async Task ExecuteAsync_SortByWinRate_SortsCorrectly()
    {
        // Arrange
        var query = new GetBacktestArchiveQuery(SortBy: "winrate", SortDescending: true);
        var backtestRuns = new List<BacktestRun>
        {
            CreateBacktestRun(id: 1, winRate: 50.0m),
            CreateBacktestRun(id: 2, winRate: 75.0m),
            CreateBacktestRun(id: 3, winRate: 60.0m)
        };

        A.CallTo(() => _backtestArchivePort.GetBacktestRunsAsync(null, null, 100))
            .Returns(backtestRuns);
        A.CallTo(() => _backtestArchivePort.GetBacktestRunCountAsync()).Returns(3);
        A.CallTo(() => _backtestArchivePort.GetLastBacktestDateAsync()).Returns(DateTime.UtcNow);
        A.CallTo(() => _backtestArchivePort.GetTopBacktestRunsAsync(1))
            .Returns(new List<BacktestRun> { backtestRuns[1] });

        // Act
        Result<BacktestArchiveResult> resultWrapper = await _useCase.ExecuteAsync(query);

        // Assert
        resultWrapper.IsSuccess.ShouldBeTrue();
        BacktestArchiveResult result = resultWrapper.Value;
        result.BacktestRuns[0].WinRate.ShouldBe(75.0m);
        result.BacktestRuns[1].WinRate.ShouldBe(60.0m);
        result.BacktestRuns[2].WinRate.ShouldBe(50.0m);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoTopPerformer_ReturnsNullTopPerformer()
    {
        // Arrange
        var query = new GetBacktestArchiveQuery();
        var backtestRuns = new List<BacktestRun>();

        A.CallTo(() => _backtestArchivePort.GetBacktestRunsAsync(null, null, 100))
            .Returns(backtestRuns);
        A.CallTo(() => _backtestArchivePort.GetBacktestRunCountAsync()).Returns(0);
        A.CallTo(() => _backtestArchivePort.GetLastBacktestDateAsync()).Returns((DateTime?)null);
        A.CallTo(() => _backtestArchivePort.GetTopBacktestRunsAsync(1))
            .Returns(new List<BacktestRun>());

        // Act
        Result<BacktestArchiveResult> resultWrapper = await _useCase.ExecuteAsync(query);

        // Assert
        resultWrapper.IsSuccess.ShouldBeTrue();
        BacktestArchiveResult result = resultWrapper.Value;
        result.BacktestRuns.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
        result.MostRecentDate.ShouldBeNull();
        result.TopPerformer.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomLimit_RespectsLimit()
    {
        // Arrange
        int customLimit = 25;
        var query = new GetBacktestArchiveQuery(Limit: customLimit);
        List<BacktestRun> backtestRuns = CreateBacktestRuns(25);

        A.CallTo(() => _backtestArchivePort.GetBacktestRunsAsync(null, null, customLimit))
            .Returns(backtestRuns);
        A.CallTo(() => _backtestArchivePort.GetBacktestRunCountAsync()).Returns(100);
        A.CallTo(() => _backtestArchivePort.GetLastBacktestDateAsync()).Returns(DateTime.UtcNow);
        A.CallTo(() => _backtestArchivePort.GetTopBacktestRunsAsync(1))
            .Returns(new List<BacktestRun> { backtestRuns[0] });

        // Act
        Result<BacktestArchiveResult> resultWrapper = await _useCase.ExecuteAsync(query);

        // Assert
        resultWrapper.IsSuccess.ShouldBeTrue();
        BacktestArchiveResult result = resultWrapper.Value;
        result.BacktestRuns.Count.ShouldBe(25);
        result.TotalCount.ShouldBe(100);
        A.CallTo(() => _backtestArchivePort.GetBacktestRunsAsync(null, null, customLimit))
            .MustHaveHappenedOnceExactly();
    }

    // Helper methods
    private List<BacktestRun> CreateBacktestRuns(
        int count,
        string ticker = "AAPL",
        string strategyType = "rsi")
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateBacktestRun(
                id: i,
                ticker: ticker,
                strategyType: strategyType,
                executedAt: DateTime.UtcNow.AddDays(-i)))
            .ToList();
    }

    private BacktestRun CreateBacktestRun(
        int id,
        string ticker = "AAPL",
        string strategyType = "rsi",
        DateTime? executedAt = null,
        decimal? totalReturn = null,
        decimal? sharpeRatio = null,
        decimal? winRate = null)
    {
        var metrics = new PerformanceMetrics(
            InitialCapital: 10000m,
            FinalEquity: 10000m + (totalReturn ?? 20.0m) * 100,
            TotalReturn: (totalReturn ?? 20.0m) * 100,
            TotalReturnPercentage: totalReturn ?? 20.0m,
            AnnualizedReturn: (totalReturn ?? 20.0m) * 0.8m,
            TotalTrades: 50,
            WinningTrades: 30,
            LosingTrades: 20,
            WinRate: winRate ?? 60.0m,
            AverageWin: 150m,
            AverageLoss: 100m,
            LargestWin: 500m,
            LargestLoss: 300m,
            ProfitFactor: 1.5m,
            MaxConsecutiveWins: 5,
            MaxConsecutiveLosses: 3,
            MaxDrawdown: 1000m,
            MaxDrawdownPercentage: 10.0m,
            SharpeRatio: sharpeRatio ?? 1.5m,
            Volatility: 15m,
            TotalDays: 365,
            DaysInMarket: 200,
            MarketExposurePercentage: 54.8m
        );

        var backtestResult = new BacktestResult(
            StrategyName: $"RSI Strategy {id}",
            StrategyDescription: "Test strategy",
            StrategyParameters: new Dictionary<string, object>(),
            Ticker: ticker,
            StartDate: DateTime.Today.AddYears(-1),
            EndDate: DateTime.Today,
            InitialCapital: 10000m,
            CommissionPercentage: 0.001m,
            MinimumCommission: 1.0m,
            Trades: new List<Trade>(),
            EquityCurve: new List<EquityPoint>(),
            Metrics: metrics
        );

        return new BacktestRun
        {
            Id = id,
            Ticker = ticker,
            StrategyType = strategyType,
            StrategyName = $"{strategyType.ToUpper()} Strategy {id}",
            ExecutedAt = executedAt ?? DateTime.UtcNow,
            ExecutionTimeMs = 1000,
            Status = "Success",
            ResultsJson = JsonSerializer.Serialize(backtestResult),
            Tags = null
        };
    }
}
