using Shouldly;
using TradingStrat.Application.Services;
using TradingStrat.Application.Tests.TestDoubles;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.Strategies;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Tests.Services;

public class BacktestEngineTests
{
    private readonly BacktestEngine _engine;
    private readonly InMemoryHistoricalDataRepository _repository;

    public BacktestEngineTests()
    {
        _repository = new InMemoryHistoricalDataRepository();
        _engine = new BacktestEngine(_repository, new PerformanceCalculator());
    }

    [Fact]
    public async Task RunBacktestAsync_WithValidData_ReturnsResult()
    {
        // Arrange
        await SaveTestData();
        BacktestConfiguration config = CreateConfig();
        IStrategy strategy = new SimpleHoldStrategy();

        // Act
        BacktestResult result = await _engine.RunBacktestAsync(strategy, config);

        // Assert
        result.ShouldNotBeNull();
        result.Ticker.ShouldBe("TEST");
    }

    [Fact]
    public async Task RunBacktestAsync_GeneratesEquityCurve()
    {
        // Arrange
        await SaveTestData();
        BacktestConfiguration config = CreateConfig();
        IStrategy strategy = new SimpleHoldStrategy();

        // Act
        BacktestResult result = await _engine.RunBacktestAsync(strategy, config);

        // Assert
        result.EquityCurve.ShouldNotBeEmpty();
        result.EquityCurve.Count.ShouldBe(10);
    }

    [Fact]
    public async Task RunBacktestAsync_WithNoData_ThrowsException()
    {
        // Arrange
        BacktestConfiguration config = CreateConfig();
        IStrategy strategy = new SimpleHoldStrategy();

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            async () => await _engine.RunBacktestAsync(strategy, config));
    }

    [Fact]
    public async Task RunBacktestAsync_ProcessesBuySignal()
    {
        // Arrange
        await SaveTestData();
        BacktestConfiguration config = CreateConfig();
        IStrategy strategy = new BuyOnceStrategy();

        // Act
        BacktestResult result = await _engine.RunBacktestAsync(strategy, config);

        // Assert
        result.Trades.ShouldContain(t => t.Type == TradeType.Buy);
    }

    [Fact]
    public async Task RunBacktestAsync_ClosesFinalPosition()
    {
        // Arrange
        await SaveTestData();
        BacktestConfiguration config = CreateConfig();
        IStrategy strategy = new BuyAndHoldStrategy();

        // Act
        BacktestResult result = await _engine.RunBacktestAsync(strategy, config);

        // Assert
        result.Trades.Last().Reason.ShouldBe("Close final position");
    }

    [Fact]
    public async Task RunBacktestAsync_CalculatesCommissions()
    {
        // Arrange
        await SaveTestData();
        BacktestConfiguration config = CreateConfig();
        IStrategy strategy = new BuyOnceStrategy();

        // Act
        BacktestResult result = await _engine.RunBacktestAsync(strategy, config);

        // Assert
        result.Trades.ShouldAllBe(t => t.Commission > 0);
    }

    [Fact]
    public async Task RunBacktestAsync_IncludesPerformanceMetrics()
    {
        // Arrange
        await SaveTestData();
        BacktestConfiguration config = CreateConfig();
        IStrategy strategy = new SimpleHoldStrategy();

        // Act
        BacktestResult result = await _engine.RunBacktestAsync(strategy, config);

        // Assert
        result.Metrics.ShouldNotBeNull();
    }

    [Fact]
    public async Task RunBacktestAsync_ReportsProgress()
    {
        // Arrange
        await SaveTestData();
        BacktestConfiguration config = CreateConfig();
        IStrategy strategy = new SimpleHoldStrategy();
        List<(int, int, int)> reports = new();

        // Act
        await _engine.RunBacktestAsync(strategy, config, new Progress<(int, int, int)>(reports.Add));

        // Assert
        reports.ShouldNotBeEmpty();
        reports.Last().Item1.ShouldBe(10);
    }

    private async Task SaveTestData()
    {
        List<HistoricalPrice> prices = new();
        for (int i = 0; i < 10; i++)
        {
            prices.Add(new HistoricalPrice
            {
                Ticker = "TEST",
                DateTime = DateTime.Today.AddDays(-9 + i),
                Open = 100m,
                High = 105m,
                Low = 95m,
                Close = 100m + i,
                Volume = 1000000
            });
        }
        await _repository.SaveHistoricalDataAsync("TEST", null, TimeFrame.D1, prices);
    }

    private BacktestConfiguration CreateConfig() =>
        new("TEST", DateTime.Today.AddDays(-9), DateTime.Today, 10000m, 0.001m, 1m, TimeFrame.D1);

    private class SimpleHoldStrategy : IStrategy
    {
        public string Name => "Hold";
        public string Description => "Always holds";
        public void Initialize(IReadOnlyList<HistoricalPrice> data) { }
        public TradeSignal GenerateSignal(int i, decimal cash, int pos) =>
            new(SignalType.Hold, 0, 0, "Hold");
        public Dictionary<string, object> GetParameters() => new();
    }

    private class BuyOnceStrategy : IStrategy
    {
        private bool _bought;
        private IReadOnlyList<HistoricalPrice> _data = null!;
        public string Name => "Buy Once";
        public string Description => "Buys once";
        public void Initialize(IReadOnlyList<HistoricalPrice> data) { _data = data; }
        public TradeSignal GenerateSignal(int i, decimal cash, int pos)
        {
            if (!_bought && i == 0 && pos == 0)
            {
                _bought = true;
                decimal price = _data[i].Close ?? 100m;
                // Account for commission: quantity = cash / (price * (1 + commissionRate))
                // With 0.1% commission: quantity = cash / (price * 1.001)
                int quantity = (int)(cash / (price * 1.001m));
                return new(SignalType.Buy, price, quantity, "Buy");
            }
            return new(SignalType.Hold, 0, 0, "Hold");
        }
        public Dictionary<string, object> GetParameters() => new();
    }

    private class BuyAndHoldStrategy : IStrategy
    {
        private bool _bought;
        private IReadOnlyList<HistoricalPrice> _data = null!;
        public string Name => "Buy and Hold";
        public string Description => "Buys and holds";
        public void Initialize(IReadOnlyList<HistoricalPrice> data) { _data = data; }
        public TradeSignal GenerateSignal(int i, decimal cash, int pos)
        {
            if (!_bought && i == 0 && pos == 0)
            {
                _bought = true;
                decimal price = _data[i].Close ?? 100m;
                // Account for commission: quantity = cash / (price * (1 + commissionRate))
                // With 0.1% commission: quantity = cash / (price * 1.001)
                int quantity = (int)(cash / (price * 1.001m));
                return new(SignalType.Buy, price, quantity, "Buy");
            }
            return new(SignalType.Hold, 0, 0, "Hold");
        }
        public Dictionary<string, object> GetParameters() => new();
    }
}
