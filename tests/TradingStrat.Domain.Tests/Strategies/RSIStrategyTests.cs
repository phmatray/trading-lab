using FluentAssertions;
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
        strategy.Should().NotBeNull();
        strategy.Name.Should().Be("RSI (14, 30/70)");
    }

    [Fact]
    public void Constructor_WithInvalidThresholds_ShouldThrow()
    {
        // Arrange & Act
        var act = () => new RSIStrategy(_indicatorCalculator, period: 14, oversoldThreshold: 80, overboughtThreshold: 70);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Oversold threshold must be less than overbought threshold*");
    }

    [Fact]
    public void GenerateSignal_WhenRSIOversold_ReturnsBuySignal()
    {
        // Arrange
        // Create declining prices to get low RSI (oversold)
        var prices = HistoricalPriceBuilder.Create()
            .WithPrices(
                150m, 148m, 145m, 143m, 140m,
                138m, 135m, 133m, 130m, 128m,
                125m, 123m, 120m, 118m, 115m,
                113m, 110m, 108m, 105m, 103m)
            .Build();

        var strategy = new RSIStrategy(_indicatorCalculator, period: 14, oversoldThreshold: 30, overboughtThreshold: 70);
        strategy.Initialize(prices.AsReadOnly());

        // Act
        var signal = strategy.GenerateSignal(19, 10000m, 0);

        // Assert
        signal.Type.Should().Be(SignalType.Buy);
        signal.Quantity.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GenerateSignal_WhenRSIOverbought_ReturnsSellSignal()
    {
        // Arrange
        // Create rising prices to get high RSI (overbought)
        var prices = HistoricalPriceBuilder.Create()
            .WithPrices(
                100m, 102m, 105m, 107m, 110m,
                112m, 115m, 117m, 120m, 122m,
                125m, 127m, 130m, 132m, 135m,
                137m, 140m, 142m, 145m, 147m)
            .Build();

        var strategy = new RSIStrategy(_indicatorCalculator, period: 14, oversoldThreshold: 30, overboughtThreshold: 70);
        strategy.Initialize(prices.AsReadOnly());

        // Act - assuming we have a position
        var signal = strategy.GenerateSignal(19, 0m, 100);

        // Assert
        signal.Type.Should().Be(SignalType.Sell);
        signal.Quantity.Should().Be(100);
    }

    [Fact]
    public void GenerateSignal_WhenRSINeutral_ReturnsHoldSignal()
    {
        // Arrange
        // Create sideways movement to get neutral RSI
        var prices = HistoricalPriceBuilder.Create()
            .WithPrices(
                100m, 101m, 100m, 101m, 100m,
                101m, 100m, 101m, 100m, 101m,
                100m, 101m, 100m, 101m, 100m,
                101m, 100m, 101m, 100m, 101m)
            .Build();

        var strategy = new RSIStrategy(_indicatorCalculator, period: 14, oversoldThreshold: 30, overboughtThreshold: 70);
        strategy.Initialize(prices.AsReadOnly());

        // Act
        var signal = strategy.GenerateSignal(19, 10000m, 0);

        // Assert
        signal.Type.Should().Be(SignalType.Hold);
    }

    [Fact]
    public void GenerateSignal_WhenNotEnoughData_ReturnsHoldSignal()
    {
        // Arrange
        var prices = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 101m, 102m)  // Only 3 data points
            .Build();

        var strategy = new RSIStrategy(_indicatorCalculator, period: 14, oversoldThreshold: 30, overboughtThreshold: 70);
        strategy.Initialize(prices.AsReadOnly());

        // Act
        var signal = strategy.GenerateSignal(2, 10000m, 0);

        // Assert
        signal.Type.Should().Be(SignalType.Hold);
        signal.Reason.Should().Contain("Insufficient data");
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
        strategy.Should().NotBeNull();
        strategy.Name.Should().Be($"RSI ({period}, {oversold}/{overbought})");
    }
}
