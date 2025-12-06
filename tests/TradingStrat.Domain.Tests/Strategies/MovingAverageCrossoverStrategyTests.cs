using FluentAssertions;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.Strategies;
using TradingStrat.Domain.Tests.Builders;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Tests.Strategies;

public class MovingAverageCrossoverStrategyTests
{
    private readonly IIndicatorCalculator _indicatorCalculator;

    public MovingAverageCrossoverStrategyTests()
    {
        _indicatorCalculator = new IndicatorCalculator();
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateStrategy()
    {
        // Arrange & Act
        var strategy = new MovingAverageCrossoverStrategy(_indicatorCalculator, fastPeriod: 5, slowPeriod: 10);

        // Assert
        strategy.Should().NotBeNull();
        strategy.Name.Should().Be("MA Crossover (5/10)");
    }

    [Fact]
    public void Constructor_WithFastPeriodGreaterThanSlowPeriod_ShouldThrow()
    {
        // Arrange & Act
        var act = () => new MovingAverageCrossoverStrategy(_indicatorCalculator, fastPeriod: 20, slowPeriod: 10);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Fast period must be less than slow period*");
    }

    [Fact]
    public void GenerateSignal_WhenFastCrossesAboveSlow_ReturnsBuySignal()
    {
        // Arrange
        // Create price data where fast MA crosses above slow MA
        var prices = HistoricalPriceBuilder.Create()
            .WithPrices(
                100m, 101m, 102m, 103m, 104m,  // Uptrend - fast MA will cross above
                105m, 106m, 107m, 108m, 109m,
                110m, 111m, 112m, 113m, 114m)
            .Build();

        var strategy = new MovingAverageCrossoverStrategy(_indicatorCalculator, fastPeriod: 5, slowPeriod: 10);
        strategy.Initialize(prices.AsReadOnly());

        // Act
        var signal = strategy.GenerateSignal(14, 10000m, 0);

        // Assert
        signal.Type.Should().Be(SignalType.Buy);
        signal.Quantity.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GenerateSignal_WhenFastCrossesBelowSlow_ReturnsSellSignal()
    {
        // Arrange
        // Start high then downtrend - fast MA will cross below slow MA
        var prices = HistoricalPriceBuilder.Create()
            .WithPrices(
                120m, 119m, 118m, 117m, 116m,  // Downtrend - fast MA will cross below
                115m, 114m, 113m, 112m, 111m,
                110m, 109m, 108m, 107m, 106m)
            .Build();

        var strategy = new MovingAverageCrossoverStrategy(_indicatorCalculator, fastPeriod: 5, slowPeriod: 10);
        strategy.Initialize(prices.AsReadOnly());

        // Act - assuming we have a position
        var signal = strategy.GenerateSignal(14, 0m, 100);

        // Assert
        signal.Type.Should().Be(SignalType.Sell);
        signal.Quantity.Should().Be(100);
    }

    [Fact]
    public void GenerateSignal_WhenNotEnoughData_ReturnsHoldSignal()
    {
        // Arrange
        var prices = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 101m, 102m)  // Only 3 data points
            .Build();

        var strategy = new MovingAverageCrossoverStrategy(_indicatorCalculator, fastPeriod: 5, slowPeriod: 10);
        strategy.Initialize(prices.AsReadOnly());

        // Act
        var signal = strategy.GenerateSignal(2, 10000m, 0);

        // Assert
        signal.Type.Should().Be(SignalType.Hold);
        signal.Reason.Should().Contain("Insufficient data");
    }

    [Fact]
    public void GenerateSignal_WhenNoPositionAndNoCash_ReturnsHoldSignal()
    {
        // Arrange
        var prices = HistoricalPriceBuilder.Create()
            .WithTrendingPrices(count: 20, startPrice: 100m, increment: 1m)
            .Build();

        var strategy = new MovingAverageCrossoverStrategy(_indicatorCalculator, fastPeriod: 5, slowPeriod: 10);
        strategy.Initialize(prices.AsReadOnly());

        // Act
        var signal = strategy.GenerateSignal(19, 0m, 0);

        // Assert
        signal.Type.Should().Be(SignalType.Hold);
    }

    [Fact]
    public void Initialize_WithValidData_ShouldNotThrow()
    {
        // Arrange
        var prices = HistoricalPriceBuilder.Create()
            .WithTrendingPrices(count: 50, startPrice: 100m, increment: 0.5m)
            .Build();

        var strategy = new MovingAverageCrossoverStrategy(_indicatorCalculator, fastPeriod: 20, slowPeriod: 50);

        // Act
        var act = () => strategy.Initialize(prices.AsReadOnly());

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void GenerateSignal_AfterMultipleCalls_ShouldProduceConsistentResults()
    {
        // Arrange
        var prices = HistoricalPriceBuilder.Create()
            .WithTrendingPrices(count: 30, startPrice: 100m, increment: 1m)
            .Build();

        var strategy = new MovingAverageCrossoverStrategy(_indicatorCalculator, fastPeriod: 5, slowPeriod: 10);
        strategy.Initialize(prices.AsReadOnly());

        // Act
        var signal1 = strategy.GenerateSignal(20, 10000m, 0);
        var signal2 = strategy.GenerateSignal(20, 10000m, 0);

        // Assert
        signal1.Type.Should().Be(signal2.Type);
        signal1.Quantity.Should().Be(signal2.Quantity);
    }
}
