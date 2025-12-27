using FakeItEasy;
using Shouldly;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.UseCases;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Strategies;

namespace TradingStrat.Application.Tests.UseCases;

public class MultiStrategyComparisonUseCaseTests
{
    private readonly IBacktestUseCase _backtestUseCase;
    private readonly MultiStrategyComparisonUseCase _useCase;

    public MultiStrategyComparisonUseCaseTests()
    {
        _backtestUseCase = A.Fake<IBacktestUseCase>();
        _useCase = new MultiStrategyComparisonUseCase(_backtestUseCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleStrategies_ReturnsComparisonResult()
    {
        // Arrange
        var strategies = new List<StrategyConfiguration>
        {
            new("rsi", new Dictionary<string, object> { ["Period"] = 14 }),
            new("macd", new Dictionary<string, object> { ["FastPeriod"] = 12, ["SlowPeriod"] = 26 }),
            new("ma", new Dictionary<string, object> { ["FastPeriod"] = 10, ["SlowPeriod"] = 20 })
        };

        var command = new MultiStrategyComparisonCommand(
            Ticker: "AAPL",
            Strategies: strategies,
            StartDate: DateTime.Today.AddYears(-1),
            EndDate: DateTime.Today,
            InitialCapital: 10000m,
            CommissionPercentage: 0.001m,
            MinimumCommission: 1.0m
        );

        // Setup fake responses
        A.CallTo(() => _backtestUseCase.ExecuteAsync(A<BacktestCommand>._, A<IProgress<BacktestProgress>?>._))
            .ReturnsNextFromSequence(
                CreateBacktestResult("RSI Strategy", 25.0m, 1.8m, 500m),
                CreateBacktestResult("MACD Strategy", 30.0m, 2.0m, 600m),
                CreateBacktestResult("MA Strategy", 20.0m, 1.5m, 400m)
            );

        // Act
        MultiStrategyComparisonResult result = await _useCase.ExecuteAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.Ticker.ShouldBe("AAPL");
        result.Strategies.Count.ShouldBe(3);
        result.BestByReturn.ShouldNotBeNull();
        result.BestBySharpe.ShouldNotBeNull();
        result.BestByDrawdown.ShouldNotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_IdentifiesBestByReturn()
    {
        // Arrange
        var strategies = new List<StrategyConfiguration>
        {
            new("rsi", new Dictionary<string, object>()),
            new("macd", new Dictionary<string, object>())
        };

        var command = new MultiStrategyComparisonCommand(
            "AAPL", strategies, DateTime.Today.AddYears(-1), DateTime.Today, 10000m, 0.001m, 1.0m
        );

        A.CallTo(() => _backtestUseCase.ExecuteAsync(A<BacktestCommand>._, A<IProgress<BacktestProgress>?>._))
            .ReturnsNextFromSequence(
                CreateBacktestResult("RSI Strategy", 25.0m, 1.5m, 500m),
                CreateBacktestResult("MACD Strategy", 35.0m, 1.8m, 600m)  // Higher return
            );

        // Act
        MultiStrategyComparisonResult result = await _useCase.ExecuteAsync(command);

        // Assert
        result.BestByReturn.ShouldNotBeNull();
        result.BestByReturn.StrategyName.ShouldBe("MACD Strategy");
        result.BestByReturn.Metrics.TotalReturnPercentage.ShouldBe(35.0m);
    }

    [Fact]
    public async Task ExecuteAsync_IdentifiesBestBySharpe()
    {
        // Arrange
        var strategies = new List<StrategyConfiguration>
        {
            new("rsi", new Dictionary<string, object>()),
            new("macd", new Dictionary<string, object>())
        };

        var command = new MultiStrategyComparisonCommand(
            "AAPL", strategies, DateTime.Today.AddYears(-1), DateTime.Today, 10000m, 0.001m, 1.0m
        );

        A.CallTo(() => _backtestUseCase.ExecuteAsync(A<BacktestCommand>._, A<IProgress<BacktestProgress>?>._))
            .ReturnsNextFromSequence(
                CreateBacktestResult("RSI Strategy", 25.0m, 2.5m, 500m),  // Higher Sharpe
                CreateBacktestResult("MACD Strategy", 30.0m, 1.8m, 600m)
            );

        // Act
        MultiStrategyComparisonResult result = await _useCase.ExecuteAsync(command);

        // Assert
        result.BestBySharpe.ShouldNotBeNull();
        result.BestBySharpe.StrategyName.ShouldBe("RSI Strategy");
        result.BestBySharpe.Metrics.SharpeRatio.ShouldBe(2.5m);
    }

    [Fact]
    public async Task ExecuteAsync_IdentifiesBestByDrawdown()
    {
        // Arrange
        var strategies = new List<StrategyConfiguration>
        {
            new("rsi", new Dictionary<string, object>()),
            new("macd", new Dictionary<string, object>())
        };

        var command = new MultiStrategyComparisonCommand(
            "AAPL", strategies, DateTime.Today.AddYears(-1), DateTime.Today, 10000m, 0.001m, 1.0m
        );

        A.CallTo(() => _backtestUseCase.ExecuteAsync(A<BacktestCommand>._, A<IProgress<BacktestProgress>?>._))
            .ReturnsNextFromSequence(
                CreateBacktestResult("RSI Strategy", 25.0m, 1.5m, 800m),
                CreateBacktestResult("MACD Strategy", 30.0m, 1.8m, 400m)  // Lower drawdown (better)
            );

        // Act
        MultiStrategyComparisonResult result = await _useCase.ExecuteAsync(command);

        // Assert
        result.BestByDrawdown.ShouldNotBeNull();
        result.BestByDrawdown.StrategyName.ShouldBe("MACD Strategy");
        result.BestByDrawdown.Metrics.MaxDrawdown.ShouldBe(400m);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoStrategies_ThrowsArgumentException()
    {
        // Arrange
        var command = new MultiStrategyComparisonCommand(
            "AAPL",
            new List<StrategyConfiguration>(),
            DateTime.Today.AddYears(-1),
            DateTime.Today,
            10000m,
            0.001m,
            1.0m
        );

        // Act & Assert
        ArgumentException ex = await Should.ThrowAsync<ArgumentException>(
            async () => await _useCase.ExecuteAsync(command));
        ex.Message.ShouldContain("At least one strategy must be provided");
    }

    [Fact]
    public async Task ExecuteAsync_WithMoreThan10Strategies_ThrowsArgumentException()
    {
        // Arrange
        var strategies = Enumerable.Range(1, 11)
            .Select(i => new StrategyConfiguration("rsi", new Dictionary<string, object>()))
            .ToList();

        var command = new MultiStrategyComparisonCommand(
            "AAPL", strategies, DateTime.Today.AddYears(-1), DateTime.Today, 10000m, 0.001m, 1.0m
        );

        // Act & Assert
        ArgumentException ex = await Should.ThrowAsync<ArgumentException>(
            async () => await _useCase.ExecuteAsync(command));
        ex.Message.ShouldContain("Maximum 10 strategies");
    }

    [Fact]
    public async Task ExecuteAsync_CountsWinningAndLosingTrades()
    {
        // Arrange
        var strategies = new List<StrategyConfiguration>
        {
            new("rsi", new Dictionary<string, object>())
        };

        var command = new MultiStrategyComparisonCommand(
            "AAPL", strategies, DateTime.Today.AddYears(-1), DateTime.Today, 10000m, 0.001m, 1.0m
        );

        var trades = new List<Trade>
        {
            new() { ProfitLoss = 100m },   // Win
            new() { ProfitLoss = -50m },   // Loss
            new() { ProfitLoss = 200m },   // Win
            new() { ProfitLoss = -75m },   // Loss
            new() { ProfitLoss = 150m }    // Win
        };

        var backtestResult = CreateBacktestResult("RSI Strategy", 25.0m, 1.5m, 500m);
        backtestResult = backtestResult with { Trades = trades };

        A.CallTo(() => _backtestUseCase.ExecuteAsync(A<BacktestCommand>._, A<IProgress<BacktestProgress>?>._))
            .Returns(backtestResult);

        // Act
        MultiStrategyComparisonResult result = await _useCase.ExecuteAsync(command);

        // Assert
        result.Strategies.Count.ShouldBe(1);
        result.Strategies[0].TotalTrades.ShouldBe(5);
        result.Strategies[0].WinningTrades.ShouldBe(3);
        result.Strategies[0].LosingTrades.ShouldBe(2);
    }

    [Fact]
    public async Task ExecuteAsync_ReportsProgressForEachStrategy()
    {
        // Arrange
        var strategies = new List<StrategyConfiguration>
        {
            new("rsi", new Dictionary<string, object>()),
            new("macd", new Dictionary<string, object>())
        };

        var command = new MultiStrategyComparisonCommand(
            "AAPL", strategies, DateTime.Today.AddYears(-1), DateTime.Today, 10000m, 0.001m, 1.0m
        );

        A.CallTo(() => _backtestUseCase.ExecuteAsync(A<BacktestCommand>._, A<IProgress<BacktestProgress>?>._))
            .ReturnsNextFromSequence(
                CreateBacktestResult("RSI Strategy", 25.0m, 1.5m, 500m),
                CreateBacktestResult("MACD Strategy", 30.0m, 1.8m, 600m)
            );

        var progressReports = new List<string>();
        var progress = new Progress<string>(msg => progressReports.Add(msg));

        // Act
        await _useCase.ExecuteAsync(command, progress);

        // Assert
        progressReports.ShouldNotBeEmpty();
        progressReports.ShouldContain(msg => msg.Contains("Running backtest 1/2"));
        progressReports.ShouldContain(msg => msg.Contains("Running backtest 2/2"));
        progressReports.ShouldContain(msg => msg.Contains("Comparison complete"));
    }

    [Fact]
    public async Task ExecuteAsync_PassesCorrectParametersToBacktest()
    {
        // Arrange
        var strategyParams = new Dictionary<string, object> { ["Period"] = 14, ["Oversold"] = 30 };
        var strategies = new List<StrategyConfiguration>
        {
            new("rsi", strategyParams)
        };

        var command = new MultiStrategyComparisonCommand(
            Ticker: "AAPL",
            Strategies: strategies,
            StartDate: new DateTime(2023, 1, 1),
            EndDate: new DateTime(2024, 1, 1),
            InitialCapital: 15000m,
            CommissionPercentage: 0.002m,
            MinimumCommission: 2.0m
        );

        A.CallTo(() => _backtestUseCase.ExecuteAsync(A<BacktestCommand>._, A<IProgress<BacktestProgress>?>._))
            .Returns(CreateBacktestResult("RSI Strategy", 25.0m, 1.5m, 500m));

        // Act
        await _useCase.ExecuteAsync(command);

        // Assert
        A.CallTo(() => _backtestUseCase.ExecuteAsync(
            A<BacktestCommand>.That.Matches(cmd =>
                cmd.Ticker == "AAPL" &&
                cmd.StrategyType == StrategyType.RSI &&
                cmd.StrategyParameters == strategyParams &&
                cmd.InitialCapital == 15000m &&
                cmd.CommissionPercentage == 0.002m &&
                cmd.MinimumCommission == 2.0m &&
                cmd.StartDate == new DateTime(2023, 1, 1) &&
                cmd.EndDate == new DateTime(2024, 1, 1)
            ),
            A<IProgress<BacktestProgress>?>._
        )).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomStrategy_PassesCustomStrategyId()
    {
        // Arrange
        var strategies = new List<StrategyConfiguration>
        {
            new("custom", new Dictionary<string, object>(), CustomStrategyId: 42)
        };

        var command = new MultiStrategyComparisonCommand(
            "AAPL", strategies, DateTime.Today.AddYears(-1), DateTime.Today, 10000m, 0.001m, 1.0m
        );

        A.CallTo(() => _backtestUseCase.ExecuteAsync(A<BacktestCommand>._, A<IProgress<BacktestProgress>?>._))
            .Returns(CreateBacktestResult("Custom Strategy", 25.0m, 1.5m, 500m));

        // Act
        await _useCase.ExecuteAsync(command);

        // Assert
        A.CallTo(() => _backtestUseCase.ExecuteAsync(
            A<BacktestCommand>.That.Matches(cmd => cmd.CustomStrategyId == 42),
            A<IProgress<BacktestProgress>?>._
        )).MustHaveHappenedOnceExactly();
    }

    // Helper method
    private BacktestResult CreateBacktestResult(
        string strategyName,
        decimal totalReturnPercentage,
        decimal sharpeRatio,
        decimal maxDrawdown)
    {
        var metrics = new PerformanceMetrics(
            InitialCapital: 10000m,
            FinalEquity: 10000m + (10000m * totalReturnPercentage / 100),
            TotalReturn: 10000m * totalReturnPercentage / 100,
            TotalReturnPercentage: totalReturnPercentage,
            AnnualizedReturn: totalReturnPercentage * 0.8m,
            TotalTrades: 50,
            WinningTrades: 30,
            LosingTrades: 20,
            WinRate: 60m,
            AverageWin: 150m,
            AverageLoss: 100m,
            LargestWin: 500m,
            LargestLoss: 300m,
            ProfitFactor: 1.5m,
            MaxConsecutiveWins: 5,
            MaxConsecutiveLosses: 3,
            MaxDrawdown: maxDrawdown,
            MaxDrawdownPercentage: (maxDrawdown / 10000m) * 100,
            SharpeRatio: sharpeRatio,
            Volatility: 15m,
            TotalDays: 365,
            DaysInMarket: 200,
            MarketExposurePercentage: 54.8m
        );

        return new BacktestResult(
            StrategyName: strategyName,
            StrategyDescription: "Test strategy",
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
    }
}
