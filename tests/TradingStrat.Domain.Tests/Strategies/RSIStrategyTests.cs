using Shouldly;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.Strategies;
using TradingStrat.Domain.Tests.Builders;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Tests.Strategies;

public class RSIStrategyTests
{
    private readonly IIndicatorCalculator _indicatorCalculator;

    public RSIStrategyTests()
    {
        _indicatorCalculator = new IndicatorCalculator();
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateStrategy()
    {
        // Arrange & Act
        var strategy = new RSIStrategy(_indicatorCalculator, period: 14, oversoldThreshold: 30, overboughtThreshold: 70);

        // Assert
        strategy.ShouldNotBeNull();
        strategy.Name.ShouldBe("RSI (14, 30/70)");
    }

    [Fact]
    public void Constructor_WithInvalidThresholds_ShouldThrow()
    {
        // Arrange & Act
        Func<RSIStrategy> act = () => new RSIStrategy(_indicatorCalculator, period: 14, oversoldThreshold: 80, overboughtThreshold: 70);

        // Assert
        ArgumentException ex = Should.Throw<ArgumentException>(act);
        ex.Message.ShouldContain("Oversold threshold must be less than overbought threshold");
    }

    [Fact]
    public void GenerateSignal_WhenRSIOversold_ReturnsBuySignal()
    {
        // Arrange
        // Create declining prices to get low RSI (oversold), then recovery to cross above threshold
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(
                150m, 145m, 140m, 135m, 130m,
                125m, 120m, 115m, 110m, 105m,
                100m, 98m, 96m, 95m, 94m,      // Deep oversold
                96m, 98m, 101m, 104m, 108m)    // Recovery crosses above 30
            .Build();

        var strategy = new RSIStrategy(_indicatorCalculator, period: 14, oversoldThreshold: 30, overboughtThreshold: 70);
        strategy.Initialize(prices.AsReadOnly());

        // Act
        TradeSignal signal = strategy.GenerateSignal(19, 10000m, 0);

        // Assert
        signal.Type.ShouldBe(SignalType.Buy);
        signal.Quantity.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void GenerateSignal_WhenRSIOverbought_ReturnsSellSignal()
    {
        // Arrange
        // Create rising prices to get high RSI (overbought), then decline to cross below threshold
        // Mirror the buy test - invert the price pattern
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(
                50m, 55m, 60m, 65m, 70m,
                75m, 80m, 85m, 90m, 95m,
                100m, 102m, 104m, 105m, 106m,  // Peak - deep overbought
                104m, 102m, 99m, 96m, 92m)     // Decline - crosses below 70
            .Build();

        var strategy = new RSIStrategy(_indicatorCalculator, period: 14, oversoldThreshold: 30, overboughtThreshold: 70);
        strategy.Initialize(prices.AsReadOnly());

        // Act - assuming we have a position
        TradeSignal signal = strategy.GenerateSignal(19, 0m, 100);

        // Assert
        signal.Type.ShouldBe(SignalType.Sell);
        signal.Quantity.ShouldBe(100);
    }

    [Fact]
    public void GenerateSignal_WhenRSINeutral_ReturnsHoldSignal()
    {
        // Arrange
        // Create sideways movement to get neutral RSI
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(
                100m, 101m, 100m, 101m, 100m,
                101m, 100m, 101m, 100m, 101m,
                100m, 101m, 100m, 101m, 100m,
                101m, 100m, 101m, 100m, 101m)
            .Build();

        var strategy = new RSIStrategy(_indicatorCalculator, period: 14, oversoldThreshold: 30, overboughtThreshold: 70);
        strategy.Initialize(prices.AsReadOnly());

        // Act
        TradeSignal signal = strategy.GenerateSignal(19, 10000m, 0);

        // Assert
        signal.Type.ShouldBe(SignalType.Hold);
    }

    [Fact]
    public void GenerateSignal_WhenNotEnoughData_ReturnsHoldSignal()
    {
        // Arrange
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 101m, 102m)  // Only 3 data points
            .Build();

        var strategy = new RSIStrategy(_indicatorCalculator, period: 14, oversoldThreshold: 30, overboughtThreshold: 70);
        strategy.Initialize(prices.AsReadOnly());

        // Act
        TradeSignal signal = strategy.GenerateSignal(2, 10000m, 0);

        // Assert
        signal.Type.ShouldBe(SignalType.Hold);
        signal.Reason.ShouldContain("Insufficient data");
    }

    [Theory]
    [InlineData(14, 30, 70)]
    [InlineData(9, 20, 80)]
    [InlineData(21, 25, 75)]
    public void Constructor_WithDifferentParameters_ShouldCreateStrategy(int period, int oversold, int overbought)
    {
        // Arrange & Act
        var strategy = new RSIStrategy(_indicatorCalculator, period, oversold, overbought);

        // Assert
        strategy.ShouldNotBeNull();
        strategy.Name.ShouldBe($"RSI ({period}, {oversold}/{overbought})");
    }
}
