using FakeItEasy;
using Shouldly;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.UseCases;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.Tests.UseCases;

public class GetDashboardStatsUseCaseTests
{
    private readonly ICustomStrategyPort _customStrategyPort;
    private readonly IBacktestArchivePort _backtestArchivePort;
    private readonly IPortfolioPort _portfolioPort;
    private readonly IHistoricalDataPort _historicalDataPort;
    private readonly GetDashboardStatsUseCase _useCase;

    public GetDashboardStatsUseCaseTests()
    {
        _customStrategyPort = A.Fake<ICustomStrategyPort>();
        _backtestArchivePort = A.Fake<IBacktestArchivePort>();
        _portfolioPort = A.Fake<IPortfolioPort>();
        _historicalDataPort = A.Fake<IHistoricalDataPort>();

        _useCase = new GetDashboardStatsUseCase(
            _customStrategyPort,
            _backtestArchivePort,
            _portfolioPort,
            _historicalDataPort);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoData_ReturnsOnlyBuiltInStrategies()
    {
        // Arrange
        A.CallTo(() => _customStrategyPort.GetAllAsync()).Returns(new List<CustomStrategy>());
        A.CallTo(() => _backtestArchivePort.GetBacktestRunCountAsync()).Returns(0);
        A.CallTo(() => _portfolioPort.GetAllPortfoliosAsync()).Returns(new List<Portfolio>());
        A.CallTo(() => _backtestArchivePort.GetLastBacktestDateAsync()).Returns((DateTime?)null);
        SetupHistoricalDataPort(new List<string>(), new List<TickerSummary>(), null);

        // Act
        var resultWrapper = await _useCase.ExecuteAsync();

        // Assert
        resultWrapper.IsSuccess.ShouldBeTrue();
        var result = resultWrapper.Value;
        result.ShouldNotBeNull();
        result.TotalStrategies.ShouldBe(4); // 4 built-in strategies
        result.TotalBacktests.ShouldBe(0);
        result.TotalPortfolios.ShouldBe(0);
        result.LastBacktestDate.ShouldBeNull();
        result.TotalSecurities.ShouldBe(0);
        result.DataCoveragePercentage.ShouldBe(0m);
        result.LastDataUpdate.ShouldBeNull();
    }

    private void SetupHistoricalDataPort(
        List<string> tickers,
        List<TickerSummary> summaries,
        DateTime? lastUpdate)
    {
        A.CallTo(() => _historicalDataPort.GetAllTickersAsync()).Returns(tickers);
        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(A<Domain.ValueObjects.TimeFrame>._)).Returns(summaries);
        A.CallTo(() => _historicalDataPort.GetDatabaseLastModifiedAsync()).Returns(lastUpdate);
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomStrategies_IncludesThemInCount()
    {
        // Arrange
        var customStrategies = new List<CustomStrategy>
        {
            new() { Id = 1, Name = "Custom RSI", Category = "Momentum" },
            new() { Id = 2, Name = "Custom MACD", Category = "Trend" },
            new() { Id = 3, Name = "Custom Bollinger", Category = "Volatility" }
        };

        A.CallTo(() => _customStrategyPort.GetAllAsync()).Returns(customStrategies);
        A.CallTo(() => _backtestArchivePort.GetBacktestRunCountAsync()).Returns(0);
        A.CallTo(() => _portfolioPort.GetAllPortfoliosAsync()).Returns(new List<Portfolio>());
        A.CallTo(() => _backtestArchivePort.GetLastBacktestDateAsync()).Returns((DateTime?)null);
        SetupHistoricalDataPort(new List<string>(), new List<TickerSummary>(), null);

        // Act
        var resultWrapper = await _useCase.ExecuteAsync();

        // Assert
        resultWrapper.IsSuccess.ShouldBeTrue();
        resultWrapper.Value.TotalStrategies.ShouldBe(7); // 4 built-in + 3 custom
    }

    [Fact]
    public async Task ExecuteAsync_WithBacktests_ReturnsCorrectCount()
    {
        // Arrange
        A.CallTo(() => _customStrategyPort.GetAllAsync()).Returns(new List<CustomStrategy>());
        A.CallTo(() => _backtestArchivePort.GetBacktestRunCountAsync()).Returns(42);
        A.CallTo(() => _portfolioPort.GetAllPortfoliosAsync()).Returns(new List<Portfolio>());
        A.CallTo(() => _backtestArchivePort.GetLastBacktestDateAsync()).Returns((DateTime?)null);
        SetupHistoricalDataPort(new List<string>(), new List<TickerSummary>(), null);

        // Act
        var resultWrapper = await _useCase.ExecuteAsync();

        // Assert
        resultWrapper.IsSuccess.ShouldBeTrue();
        resultWrapper.Value.TotalBacktests.ShouldBe(42);
    }

    [Fact]
    public async Task ExecuteAsync_WithPortfolios_ReturnsCorrectCount()
    {
        // Arrange
        var portfolios = new List<Portfolio>
        {
            new() { Id = 1, Name = "Growth Portfolio", Cash = 10000m },
            new() { Id = 2, Name = "Income Portfolio", Cash = 5000m },
            new() { Id = 3, Name = "Balanced Portfolio", Cash = 7500m }
        };

        A.CallTo(() => _customStrategyPort.GetAllAsync()).Returns(new List<CustomStrategy>());
        A.CallTo(() => _backtestArchivePort.GetBacktestRunCountAsync()).Returns(0);
        A.CallTo(() => _portfolioPort.GetAllPortfoliosAsync()).Returns(portfolios);
        A.CallTo(() => _backtestArchivePort.GetLastBacktestDateAsync()).Returns((DateTime?)null);
        SetupHistoricalDataPort(new List<string>(), new List<TickerSummary>(), null);

        // Act
        var resultWrapper = await _useCase.ExecuteAsync();

        // Assert
        resultWrapper.IsSuccess.ShouldBeTrue();
        resultWrapper.Value.TotalPortfolios.ShouldBe(3);
    }

    [Fact]
    public async Task ExecuteAsync_WithLastBacktestDate_ReturnsCorrectDate()
    {
        // Arrange
        DateTime lastBacktestDate = new(2024, 12, 25, 10, 30, 0);

        A.CallTo(() => _customStrategyPort.GetAllAsync()).Returns(new List<CustomStrategy>());
        A.CallTo(() => _backtestArchivePort.GetBacktestRunCountAsync()).Returns(10);
        A.CallTo(() => _portfolioPort.GetAllPortfoliosAsync()).Returns(new List<Portfolio>());
        A.CallTo(() => _backtestArchivePort.GetLastBacktestDateAsync()).Returns(lastBacktestDate);
        SetupHistoricalDataPort(new List<string>(), new List<TickerSummary>(), null);

        // Act
        var resultWrapper = await _useCase.ExecuteAsync();

        // Assert
        resultWrapper.IsSuccess.ShouldBeTrue();
        var result = resultWrapper.Value;
        result.LastBacktestDate.ShouldNotBeNull();
        result.LastBacktestDate.ShouldBe(lastBacktestDate);
    }

    [Fact]
    public async Task ExecuteAsync_WithAllData_ReturnsCompleteStats()
    {
        // Arrange
        var customStrategies = new List<CustomStrategy>
        {
            new() { Id = 1, Name = "Custom Strategy 1", Category = "Momentum" },
            new() { Id = 2, Name = "Custom Strategy 2", Category = "Trend" }
        };

        var portfolios = new List<Portfolio>
        {
            new() { Id = 1, Name = "Portfolio 1", Cash = 10000m },
            new() { Id = 2, Name = "Portfolio 2", Cash = 5000m }
        };

        DateTime lastBacktestDate = new(2024, 12, 20, 15, 45, 0);
        DateTime lastDataUpdate = new(2024, 12, 26, 10, 0, 0);
        var tickers = new List<string> { "AAPL", "MSFT", "GOOGL" };
        var summaries = new List<TickerSummary>
        {
            new("AAPL", null, 100, DateTime.Today.AddDays(-30), DateTime.Today.AddDays(-1)),
            new("MSFT", null, 100, DateTime.Today.AddDays(-30), DateTime.Today.AddDays(-2)),
            new("GOOGL", null, 100, DateTime.Today.AddDays(-30), DateTime.Today.AddDays(-10))
        };

        A.CallTo(() => _customStrategyPort.GetAllAsync()).Returns(customStrategies);
        A.CallTo(() => _backtestArchivePort.GetBacktestRunCountAsync()).Returns(25);
        A.CallTo(() => _portfolioPort.GetAllPortfoliosAsync()).Returns(portfolios);
        A.CallTo(() => _backtestArchivePort.GetLastBacktestDateAsync()).Returns(lastBacktestDate);
        SetupHistoricalDataPort(tickers, summaries, lastDataUpdate);

        // Act
        var resultWrapper = await _useCase.ExecuteAsync();

        // Assert
        resultWrapper.IsSuccess.ShouldBeTrue();
        var result = resultWrapper.Value;
        result.ShouldNotBeNull();
        result.TotalStrategies.ShouldBe(6); // 4 built-in + 2 custom
        result.TotalBacktests.ShouldBe(25);
        result.TotalPortfolios.ShouldBe(2);
        result.LastBacktestDate.ShouldBe(lastBacktestDate);
        result.TotalSecurities.ShouldBe(3);
        result.DataCoveragePercentage.ShouldBeGreaterThan(0m); // 2 out of 3 have recent data
        result.LastDataUpdate.ShouldBe(lastDataUpdate);
    }

    [Fact]
    public async Task ExecuteAsync_CallsAllPortsInParallel()
    {
        // Arrange
        A.CallTo(() => _customStrategyPort.GetAllAsync()).Returns(new List<CustomStrategy>());
        A.CallTo(() => _backtestArchivePort.GetBacktestRunCountAsync()).Returns(0);
        A.CallTo(() => _portfolioPort.GetAllPortfoliosAsync()).Returns(new List<Portfolio>());
        A.CallTo(() => _backtestArchivePort.GetLastBacktestDateAsync()).Returns((DateTime?)null);
        SetupHistoricalDataPort(new List<string>(), new List<TickerSummary>(), null);

        // Act
        var resultWrapper = await _useCase.ExecuteAsync();

        // Assert
        resultWrapper.IsSuccess.ShouldBeTrue();

        // Verify all ports were called exactly once
        A.CallTo(() => _customStrategyPort.GetAllAsync()).MustHaveHappenedOnceExactly();
        A.CallTo(() => _backtestArchivePort.GetBacktestRunCountAsync()).MustHaveHappenedOnceExactly();
        A.CallTo(() => _portfolioPort.GetAllPortfoliosAsync()).MustHaveHappenedOnceExactly();
        A.CallTo(() => _backtestArchivePort.GetLastBacktestDateAsync()).MustHaveHappenedOnceExactly();
        A.CallTo(() => _historicalDataPort.GetAllTickersAsync()).MustHaveHappenedOnceExactly();
        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(A<Domain.ValueObjects.TimeFrame>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _historicalDataPort.GetDatabaseLastModifiedAsync()).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteAsync_WithSecuritiesData_ReturnsCorrectCounts()
    {
        // Arrange
        var tickers = new List<string> { "AAPL", "MSFT", "GOOGL", "TSLA", "AMZN" };
        var summaries = new List<TickerSummary>
        {
            new("AAPL", null, 100, DateTime.Today.AddDays(-30), DateTime.Today.AddDays(-1)), // Recent
            new("MSFT", null, 100, DateTime.Today.AddDays(-30), DateTime.Today.AddDays(-2)), // Recent
            new("GOOGL", null, 100, DateTime.Today.AddDays(-30), DateTime.Today.AddDays(-10)), // Not recent
            new("TSLA", null, 100, DateTime.Today.AddDays(-30), DateTime.Today.AddDays(-1)), // Recent
            new("AMZN", null, 100, DateTime.Today.AddDays(-30), DateTime.Today.AddDays(-15)) // Not recent
        };
        DateTime lastUpdate = DateTime.UtcNow.AddHours(-2);

        A.CallTo(() => _customStrategyPort.GetAllAsync()).Returns(new List<CustomStrategy>());
        A.CallTo(() => _backtestArchivePort.GetBacktestRunCountAsync()).Returns(0);
        A.CallTo(() => _portfolioPort.GetAllPortfoliosAsync()).Returns(new List<Portfolio>());
        A.CallTo(() => _backtestArchivePort.GetLastBacktestDateAsync()).Returns((DateTime?)null);
        SetupHistoricalDataPort(tickers, summaries, lastUpdate);

        // Act
        var resultWrapper = await _useCase.ExecuteAsync();

        // Assert
        resultWrapper.IsSuccess.ShouldBeTrue();
        var result = resultWrapper.Value;
        result.TotalSecurities.ShouldBe(5);
        result.DataCoveragePercentage.ShouldBe(60m); // 3 out of 5 have recent data (within 7 days)
        result.LastDataUpdate.ShouldBe(lastUpdate);
    }
}
