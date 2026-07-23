using Shouldly;
using TradingStrat.Domain.Entities;
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
        strategy.ShouldNotBeNull();
        strategy.Name.ShouldBe("MA Crossover (5/10)");
    }

    [Fact]
    public void Constructor_WithFastPeriodGreaterThanSlowPeriod_ShouldThrow()
    {
        // Arrange & Act
        Func<MovingAverageCrossoverStrategy> act = () => new MovingAverageCrossoverStrategy(_indicatorCalculator, fastPeriod: 20, slowPeriod: 10);

        // Assert
        ArgumentException ex = Should.Throw<ArgumentException>(act);
        ex.Message.ShouldContain("Fast period must be less than slow period");
    }

    [Fact]
    public void GenerateSignal_WhenFastCrossesAboveSlow_ReturnsBuySignal()
    {
        // Arrange
        // Create price data where fast MA crosses above slow MA at index 14
        // Use declining then sharp rally pattern
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(
                110m, 108m, 106m, 104m, 102m,  // Decline - slow moves down gradually
                100m, 98m, 96m, 94m, 92m,      // Continued decline
                90m, 92m, 95m, 100m, 110m)     // Sharp rally - fast crosses above at index 14
            .Build();

        var strategy = new MovingAverageCrossoverStrategy(_indicatorCalculator, fastPeriod: 5, slowPeriod: 10);
        strategy.Initialize(prices.AsReadOnly());

        // Act
        TradeSignal signal = strategy.GenerateSignal(14, 10000m, 0);

        // Assert
        signal.Type.ShouldBe(SignalType.Buy);
        signal.Quantity.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void GenerateSignal_WhenFastCrossesBelowSlow_ReturnsSellSignal()
    {
        // Arrange
        // Create price data where fast MA crosses below slow MA at index 14
        // Use rising then sharp decline pattern
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(
                90m, 92m, 94m, 96m, 98m,       // Rise - slow moves up gradually
                100m, 102m, 104m, 106m, 108m,  // Continued rise
                110m, 108m, 105m, 100m, 90m)   // Sharp decline - fast crosses below at index 14
            .Build();

        var strategy = new MovingAverageCrossoverStrategy(_indicatorCalculator, fastPeriod: 5, slowPeriod: 10);
        strategy.Initialize(prices.AsReadOnly());

        // Act - assuming we have a position
        TradeSignal signal = strategy.GenerateSignal(14, 0m, 100);

        // Assert
        signal.Type.ShouldBe(SignalType.Sell);
        signal.Quantity.ShouldBe(100);
    }

    [Fact]
    public void GenerateSignal_WhenNotEnoughData_ReturnsHoldSignal()
    {
        // Arrange
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 101m, 102m)  // Only 3 data points
            .Build();

        var strategy = new MovingAverageCrossoverStrategy(_indicatorCalculator, fastPeriod: 5, slowPeriod: 10);
        strategy.Initialize(prices.AsReadOnly());

        // Act
        TradeSignal signal = strategy.GenerateSignal(2, 10000m, 0);

        // Assert
        signal.Type.ShouldBe(SignalType.Hold);
        signal.Reason.ShouldContain("Insufficient data");
    }

    [Fact]
    public void GenerateSignal_WhenNoPositionAndNoCash_ReturnsHoldSignal()
    {
        // Arrange
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithTrendingPrices(count: 20, startPrice: 100m, increment: 1m)
            .Build();

        var strategy = new MovingAverageCrossoverStrategy(_indicatorCalculator, fastPeriod: 5, slowPeriod: 10);
        strategy.Initialize(prices.AsReadOnly());

        // Act
        TradeSignal signal = strategy.GenerateSignal(19, 0m, 0);

        // Assert
        signal.Type.ShouldBe(SignalType.Hold);
    }

    [Fact]
    public void Initialize_WithValidData_ShouldNotThrow()
    {
        // Arrange
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithTrendingPrices(count: 50, startPrice: 100m, increment: 0.5m)
            .Build();

        var strategy = new MovingAverageCrossoverStrategy(_indicatorCalculator, fastPeriod: 20, slowPeriod: 50);

        // Act
        Action act = () => strategy.Initialize(prices.AsReadOnly());

        // Assert
        Should.NotThrow(act);
    }

    [Fact]
    public void GenerateSignal_AfterMultipleCalls_ShouldProduceConsistentResults()
    {
        // Arrange
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithTrendingPrices(count: 30, startPrice: 100m, increment: 1m)
            .Build();

        var strategy = new MovingAverageCrossoverStrategy(_indicatorCalculator, fastPeriod: 5, slowPeriod: 10);
        strategy.Initialize(prices.AsReadOnly());

        // Act
        TradeSignal signal1 = strategy.GenerateSignal(20, 10000m, 0);
        TradeSignal signal2 = strategy.GenerateSignal(20, 10000m, 0);

        // Assert
        signal1.Type.ShouldBe(signal2.Type);
        signal1.Quantity.ShouldBe(signal2.Quantity);
    }
}
