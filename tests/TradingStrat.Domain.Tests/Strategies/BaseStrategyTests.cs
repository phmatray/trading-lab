using FakeItEasy;
using Shouldly;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.Strategies;
using TradingStrat.Domain.Tests.Builders;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Tests.Strategies;

public class BaseStrategyTests
{
    private readonly IIndicatorCalculator _fakeIndicatorCalculator;

    public BaseStrategyTests()
    {
        _fakeIndicatorCalculator = A.Fake<IIndicatorCalculator>();
    }

    #region Initialize Tests

    [Fact]
    public void Initialize_WithValidData_SetsHistoricalData()
    {
        // Arrange
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 101m, 102m, 103m, 104m)
            .Build();

        TestStrategy strategy = new(_fakeIndicatorCalculator);

        // Act
        strategy.Initialize(prices.AsReadOnly());

        // Assert
        strategy.GetHistoricalData().ShouldNotBeNull();
        strategy.GetHistoricalData().Count.ShouldBe(5);
        strategy.GetHistoricalData()[0].Close.ShouldBe(100m);
        strategy.GetHistoricalData()[4].Close.ShouldBe(104m);
    }

    [Fact]
    public void Initialize_WithValidData_ExtractsClosePrices()
    {
        // Arrange
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 101m, 102m, 103m, 104m)
            .Build();

        TestStrategy strategy = new(_fakeIndicatorCalculator);

        // Act
        strategy.Initialize(prices.AsReadOnly());

        // Assert
        decimal[] closePrices = strategy.GetClosePrices();
        closePrices.ShouldNotBeNull();
        closePrices.Length.ShouldBe(5);
        closePrices[0].ShouldBe(100m);
        closePrices[1].ShouldBe(101m);
        closePrices[2].ShouldBe(102m);
        closePrices[3].ShouldBe(103m);
        closePrices[4].ShouldBe(104m);
    }

    [Fact]
    public void Initialize_WithNullClosePrices_UsesZero()
    {
        // Arrange
        List<HistoricalPrice> prices =
        [
            new HistoricalPrice { Ticker = "TEST", DateTime = DateTime.Today, Close = null },
            new HistoricalPrice { Ticker = "TEST", DateTime = DateTime.Today.AddDays(1), Close = 100m },
            new HistoricalPrice { Ticker = "TEST", DateTime = DateTime.Today.AddDays(2), Close = null }
        ];

        TestStrategy strategy = new(_fakeIndicatorCalculator);

        // Act
        strategy.Initialize(prices.AsReadOnly());

        // Assert
        decimal[] closePrices = strategy.GetClosePrices();
        closePrices[0].ShouldBe(0m);
        closePrices[1].ShouldBe(100m);
        closePrices[2].ShouldBe(0m);
    }

    #endregion

    #region CalculateQuantity Tests

    [Fact]
    public void CalculateQuantity_WithSufficientCash_ReturnsCorrectQuantity()
    {
        // Arrange
        TestStrategy strategy = new(_fakeIndicatorCalculator);
        decimal cash = 10000m;
        decimal price = 100m;
        int currentPosition = 0;

        // Act
        int quantity = strategy.TestCalculateQuantity(cash, price, currentPosition);

        // Assert
        quantity.ShouldBe(100);
    }

    [Fact]
    public void CalculateQuantity_WithExistingPosition_ReturnsZero()
    {
        // Arrange
        TestStrategy strategy = new(_fakeIndicatorCalculator);
        decimal cash = 10000m;
        decimal price = 100m;
        int currentPosition = 50;

        // Act
        int quantity = strategy.TestCalculateQuantity(cash, price, currentPosition);

        // Assert
        quantity.ShouldBe(0);
    }

    [Fact]
    public void CalculateQuantity_WithInsufficientCash_ReturnsZero()
    {
        // Arrange
        TestStrategy strategy = new(_fakeIndicatorCalculator);
        decimal cash = 50m;
        decimal price = 100m;
        int currentPosition = 0;

        // Act
        int quantity = strategy.TestCalculateQuantity(cash, price, currentPosition);

        // Assert
        quantity.ShouldBe(0);
    }

    [Fact]
    public void CalculateQuantity_WithExactCash_ReturnsOneShare()
    {
        // Arrange
        TestStrategy strategy = new(_fakeIndicatorCalculator);
        decimal cash = 100m;
        decimal price = 100m;
        int currentPosition = 0;

        // Act
        int quantity = strategy.TestCalculateQuantity(cash, price, currentPosition);

        // Assert
        quantity.ShouldBe(1);
    }

    [Fact]
    public void CalculateQuantity_WithRemainder_TruncatesToWholeShares()
    {
        // Arrange
        TestStrategy strategy = new(_fakeIndicatorCalculator);
        decimal cash = 1050m;
        decimal price = 100m;
        int currentPosition = 0;

        // Act
        int quantity = strategy.TestCalculateQuantity(cash, price, currentPosition);

        // Assert
        quantity.ShouldBe(10); // 1050 / 100 = 10.5, truncated to 10
    }

    [Fact]
    public void CalculateQuantity_WithZeroCash_ReturnsZero()
    {
        // Arrange
        TestStrategy strategy = new(_fakeIndicatorCalculator);
        decimal cash = 0m;
        decimal price = 100m;
        int currentPosition = 0;

        // Act
        int quantity = strategy.TestCalculateQuantity(cash, price, currentPosition);

        // Assert
        quantity.ShouldBe(0);
    }

    #endregion

    #region Delegation Tests

    [Fact]
    public void CalculateSMA_DelegatesToIndicatorCalculator()
    {
        // Arrange
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 101m, 102m, 103m, 104m)
            .Build();

        decimal[] expectedSMA = [0m, 0m, 101m, 102m, 103m];

        A.CallTo(() => _fakeIndicatorCalculator.CalculateSMA(
            A<decimal[]>.That.IsSameSequenceAs(new[] { 100m, 101m, 102m, 103m, 104m }),
            5))
            .Returns(expectedSMA);

        TestStrategy strategy = new(_fakeIndicatorCalculator);
        strategy.Initialize(prices.AsReadOnly());

        // Act
        decimal[] result = strategy.TestCalculateSMA(5);

        // Assert
        result.ShouldBe(expectedSMA);
        A.CallTo(() => _fakeIndicatorCalculator.CalculateSMA(
            A<decimal[]>.That.IsSameSequenceAs(new[] { 100m, 101m, 102m, 103m, 104m }),
            5))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void CalculateEMA_DelegatesToIndicatorCalculator()
    {
        // Arrange
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 101m, 102m, 103m, 104m)
            .Build();

        decimal[] expectedEMA = [0m, 0m, 101m, 102.5m, 103.75m];

        A.CallTo(() => _fakeIndicatorCalculator.CalculateEMA(
            A<decimal[]>.That.IsSameSequenceAs(new[] { 100m, 101m, 102m, 103m, 104m }),
            3))
            .Returns(expectedEMA);

        TestStrategy strategy = new(_fakeIndicatorCalculator);
        strategy.Initialize(prices.AsReadOnly());

        // Act
        decimal[] result = strategy.TestCalculateEMA(3);

        // Assert
        result.ShouldBe(expectedEMA);
        A.CallTo(() => _fakeIndicatorCalculator.CalculateEMA(
            A<decimal[]>.That.IsSameSequenceAs(new[] { 100m, 101m, 102m, 103m, 104m }),
            3))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void CalculateRSI_DelegatesToIndicatorCalculator()
    {
        // Arrange
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 101m, 102m, 103m, 104m)
            .Build();

        decimal[] expectedRSI = [50m, 50m, 100m, 100m, 100m];

        A.CallTo(() => _fakeIndicatorCalculator.CalculateRSI(
            A<decimal[]>.That.IsSameSequenceAs(new[] { 100m, 101m, 102m, 103m, 104m }),
            14))
            .Returns(expectedRSI);

        TestStrategy strategy = new(_fakeIndicatorCalculator);
        strategy.Initialize(prices.AsReadOnly());

        // Act
        decimal[] result = strategy.TestCalculateRSI(14);

        // Assert
        result.ShouldBe(expectedRSI);
        A.CallTo(() => _fakeIndicatorCalculator.CalculateRSI(
            A<decimal[]>.That.IsSameSequenceAs(new[] { 100m, 101m, 102m, 103m, 104m }),
            14))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void CalculateMACD_DelegatesToIndicatorCalculator()
    {
        // Arrange
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 101m, 102m, 103m, 104m)
            .Build();

        decimal[] expectedMACD = [0m, 0m, 1m, 2m, 3m];
        decimal[] expectedSignal = [0m, 0m, 0m, 1m, 2m];
        decimal[] expectedHistogram = [0m, 0m, 1m, 1m, 1m];

        A.CallTo(() => _fakeIndicatorCalculator.CalculateMACD(
            A<decimal[]>.That.IsSameSequenceAs(new[] { 100m, 101m, 102m, 103m, 104m }),
            12, 26, 9))
            .Returns((expectedMACD, expectedSignal, expectedHistogram));

        TestStrategy strategy = new(_fakeIndicatorCalculator);
        strategy.Initialize(prices.AsReadOnly());

        // Act
        (decimal[] macd, decimal[] signal, decimal[] histogram) = strategy.TestCalculateMACD();

        // Assert
        macd.ShouldBe(expectedMACD);
        signal.ShouldBe(expectedSignal);
        histogram.ShouldBe(expectedHistogram);
        A.CallTo(() => _fakeIndicatorCalculator.CalculateMACD(
            A<decimal[]>.That.IsSameSequenceAs(new[] { 100m, 101m, 102m, 103m, 104m }),
            12, 26, 9))
            .MustHaveHappenedOnceExactly();
    }

    #endregion

    #region Test Helper Strategy

    private class TestStrategy(IIndicatorCalculator indicatorCalculator) : BaseStrategy(indicatorCalculator)
    {
        public override string Name => "Test Strategy";
        public override string Description => "Test strategy for unit testing BaseStrategy";

        public override TradeSignal GenerateSignal(int currentIndex, decimal currentCash, int currentPosition)
        {
            return new TradeSignal(SignalType.Hold, 0m, 0, "Test signal");
        }

        public override Dictionary<string, object> GetParameters()
        {
            return new Dictionary<string, object>
            {
                { "Test", "Value" }
            };
        }

        // Expose protected members for testing
        public IReadOnlyList<HistoricalPrice> GetHistoricalData() => HistoricalData;
        public decimal[] GetClosePrices() => ClosePrices;
        public int TestCalculateQuantity(decimal cash, decimal price, int currentPosition)
            => CalculateQuantity(cash, price, currentPosition);
        public decimal[] TestCalculateSMA(int period) => CalculateSMA(period);
        public decimal[] TestCalculateEMA(int period) => CalculateEMA(period);
        public decimal[] TestCalculateRSI(int period) => CalculateRSI(period);
        public (decimal[] macd, decimal[] signal, decimal[] histogram) TestCalculateMACD(
            int fastPeriod = 12,
            int slowPeriod = 26,
            int signalPeriod = 9)
            => CalculateMACD(fastPeriod, slowPeriod, signalPeriod);
    }

    #endregion
}
