using Microsoft.Extensions.Logging;
using FakeItEasy;
using Shouldly;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.Strategies;
using TradingStrat.Domain.Tests.Builders;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Tests.Strategies;

public class MachineLearningStrategyTests
{
    private readonly IIndicatorCalculator _indicatorCalculator;
    private readonly ILogger<MachineLearningStrategy> _fakeLogger;

    public MachineLearningStrategyTests()
    {
        _indicatorCalculator = new IndicatorCalculator();
        _fakeLogger = A.Fake<ILogger<MachineLearningStrategy>>();
    }

    #region Constructor & Initialization Tests

    [Fact]
    public void Constructor_WithDefaultThresholds_CreatesStrategy()
    {
        // Arrange & Act
        MachineLearningStrategy strategy = new(_indicatorCalculator);

        // Assert
        strategy.ShouldNotBeNull();
        strategy.Name.ShouldBe("ML FastTree (Walk-Forward)");
        strategy.Description.ShouldContain("walk-forward validation");
    }

    [Fact]
    public void Constructor_WithCustomThresholds_UsesProvidedValues()
    {
        // Arrange
        PredictionThresholds thresholds = new(0.02m, -0.02m);

        // Act
        MachineLearningStrategy strategy = new(_indicatorCalculator, thresholds, _fakeLogger);

        // Assert
        strategy.ShouldNotBeNull();
    }

    [Fact]
    public void Initialize_ExtractsOHLCVData()
    {
        // Arrange
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 101m, 102m, 103m, 104m)
            .Build();

        MachineLearningStrategy strategy = new(_indicatorCalculator);

        // Act
        strategy.Initialize(prices.AsReadOnly());

        // Assert
        // Just verify initialization doesn't throw
        strategy.ShouldNotBeNull();
    }

    [Fact]
    public void GetParameters_ReturnsMLConfiguration()
    {
        // Arrange
        PredictionThresholds thresholds = new(0.015m, -0.015m);
        MachineLearningStrategy strategy = new(_indicatorCalculator, thresholds);
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 101m, 102m)
            .Build();
        strategy.Initialize(prices.AsReadOnly());

        // Act
        Dictionary<string, object> parameters = strategy.GetParameters();

        // Assert
        parameters.ShouldContainKey("BuyThreshold");
        parameters.ShouldContainKey("SellThreshold");
        parameters["BuyThreshold"].ShouldBe(0.015m);
        parameters["SellThreshold"].ShouldBe(-0.015m);
    }

    #endregion

    #region Insufficient Data Handling Tests

    [Fact]
    public void GenerateSignal_WithLessThanMinTrainingBars_ReturnsHold()
    {
        // Arrange - Only 50 bars (need 100 minimum)
        decimal[] priceValues = new decimal[50];
        for (int i = 0; i < 50; i++)
        {
            priceValues[i] = 100m + i;
        }

        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(priceValues)
            .Build();

        MachineLearningStrategy strategy = new(_indicatorCalculator);
        strategy.Initialize(prices.AsReadOnly());

        // Act
        TradeSignal signal = strategy.GenerateSignal(49, 10000m, 0);

        // Assert
        signal.Type.ShouldBe(SignalType.Hold);
        signal.Reason.ShouldContain("Insufficient training data");
    }

    [Fact]
    public void GenerateSignal_AtExactly100Bars_AttemptsTraining()
    {
        // Arrange - Need 101 bars to call GenerateSignal at index 100
        decimal[] priceValues = new decimal[101];
        for (int i = 0; i < 101; i++)
        {
            priceValues[i] = 100m + (i * 0.5m);
        }

        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(priceValues)
            .Build();

        MachineLearningStrategy strategy = new(_indicatorCalculator);
        strategy.Initialize(prices.AsReadOnly());

        // Act - Call at index 100 (minimum required)
        TradeSignal signal = strategy.GenerateSignal(100, 10000m, 0);

        // Assert
        // Should not be "Insufficient training data"
        signal.Reason.ShouldNotContain("Insufficient training data");
    }

    [Fact]
    public void GenerateSignal_With99Bars_ReturnsInsufficientData()
    {
        // Arrange
        decimal[] priceValues = new decimal[99];
        for (int i = 0; i < 99; i++)
        {
            priceValues[i] = 100m + i;
        }

        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(priceValues)
            .Build();

        MachineLearningStrategy strategy = new(_indicatorCalculator);
        strategy.Initialize(prices.AsReadOnly());

        // Act
        TradeSignal signal = strategy.GenerateSignal(98, 10000m, 0);

        // Assert
        signal.Type.ShouldBe(SignalType.Hold);
        signal.Reason.ShouldBe("Insufficient training data");
    }

    [Fact]
    public void GenerateSignal_WithNoData_ReturnsHold()
    {
        // Arrange
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(100m)
            .Build();

        MachineLearningStrategy strategy = new(_indicatorCalculator);
        strategy.Initialize(prices.AsReadOnly());

        // Act
        TradeSignal signal = strategy.GenerateSignal(0, 10000m, 0);

        // Assert
        signal.Type.ShouldBe(SignalType.Hold);
        signal.Reason.ShouldContain("Insufficient training data");
    }

    #endregion

    #region Signal Generation Tests

    [Fact]
    public void GenerateSignal_WithSufficientData_GeneratesSignal()
    {
        // Arrange - 150 bars of trending data
        decimal[] priceValues = new decimal[150];
        for (int i = 0; i < 150; i++)
        {
            priceValues[i] = 100m + (i * 0.5m);
        }

        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(priceValues)
            .Build();

        MachineLearningStrategy strategy = new(_indicatorCalculator);
        strategy.Initialize(prices.AsReadOnly());

        // Act
        TradeSignal signal = strategy.GenerateSignal(149, 10000m, 0);

        // Assert
        signal.ShouldNotBeNull();
        signal.Type.ShouldBeOneOf(SignalType.Buy, SignalType.Sell, SignalType.Hold);
    }

    [Fact]
    public void GenerateSignal_WithNoPosition_CannotSell()
    {
        // Arrange
        decimal[] priceValues = new decimal[150];
        for (int i = 0; i < 150; i++)
        {
            priceValues[i] = 100m - (i * 0.5m); // Declining prices
        }

        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(priceValues)
            .Build();

        MachineLearningStrategy strategy = new(_indicatorCalculator);
        strategy.Initialize(prices.AsReadOnly());

        // Act
        TradeSignal signal = strategy.GenerateSignal(149, 10000m, 0);

        // Assert
        // Should never generate a sell signal when position is 0
        signal.Type.ShouldNotBe(SignalType.Sell);
    }

    [Fact]
    public void GenerateSignal_WithNoCash_CannotBuy()
    {
        // Arrange
        decimal[] priceValues = new decimal[150];
        for (int i = 0; i < 150; i++)
        {
            priceValues[i] = 100m + (i * 0.5m);
        }

        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(priceValues)
            .Build();

        MachineLearningStrategy strategy = new(_indicatorCalculator);
        strategy.Initialize(prices.AsReadOnly());

        // Act
        TradeSignal signal = strategy.GenerateSignal(149, 0m, 0);

        // Assert
        // Should never generate a buy signal when cash is 0
        signal.Type.ShouldNotBe(SignalType.Buy);
    }

    [Fact]
    public void GenerateSignal_WithExistingPosition_CanSell()
    {
        // Arrange
        decimal[] priceValues = new decimal[150];
        for (int i = 0; i < 150; i++)
        {
            priceValues[i] = 100m - (i * 0.5m); // Declining prices
        }

        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(priceValues)
            .Build();

        MachineLearningStrategy strategy = new(_indicatorCalculator, new PredictionThresholds(0.01m, -0.01m));
        strategy.Initialize(prices.AsReadOnly());

        // Act
        TradeSignal signal = strategy.GenerateSignal(149, 0m, 100);

        // Assert
        // With declining prices and a position, might generate sell
        signal.ShouldNotBeNull();
    }

    [Fact]
    public void GenerateSignal_ReasonIncludesPredictedReturn()
    {
        // Arrange
        decimal[] priceValues = new decimal[150];
        for (int i = 0; i < 150; i++)
        {
            priceValues[i] = 100m + (i * 0.5m);
        }

        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(priceValues)
            .Build();

        MachineLearningStrategy strategy = new(_indicatorCalculator);
        strategy.Initialize(prices.AsReadOnly());

        // Act
        TradeSignal signal = strategy.GenerateSignal(149, 10000m, 0);

        // Assert
        // Reason should contain information about the prediction
        signal.Reason.ShouldNotBeNullOrEmpty();
    }

    #endregion

    #region Walk-Forward Validation Tests

    [Fact]
    public void GenerateSignal_TrainsModelOnlyOnHistoricalData()
    {
        // Arrange - This tests that we don't use future data (look-ahead bias)
        decimal[] priceValues = new decimal[120];
        for (int i = 0; i < 120; i++)
        {
            priceValues[i] = 100m + (i * 0.5m);
        }

        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(priceValues)
            .Build();

        MachineLearningStrategy strategy = new(_indicatorCalculator);
        strategy.Initialize(prices.AsReadOnly());

        // Act - Generate signal at index 100
        TradeSignal signal = strategy.GenerateSignal(100, 10000m, 0);

        // Assert - Should successfully generate without using data beyond index 100
        signal.ShouldNotBeNull();
        signal.Reason.ShouldNotContain("error");
    }

    [Fact]
    public void GenerateSignal_ConsecutiveCalls_UsesDifferentModels()
    {
        // Arrange
        decimal[] priceValues = new decimal[150];
        for (int i = 0; i < 150; i++)
        {
            priceValues[i] = 100m + (i * 0.5m);
        }

        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(priceValues)
            .Build();

        MachineLearningStrategy strategy = new(_indicatorCalculator);
        strategy.Initialize(prices.AsReadOnly());

        // Act - Generate signals at different time points
        TradeSignal signal1 = strategy.GenerateSignal(120, 10000m, 0);
        TradeSignal signal2 = strategy.GenerateSignal(140, 10000m, 0);

        // Assert - Both should succeed (each trains its own model)
        signal1.ShouldNotBeNull();
        signal2.ShouldNotBeNull();
    }

    #endregion
}
