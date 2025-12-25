using FakeItEasy;
using Shouldly;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.Strategies;
using TradingStrat.Domain.Tests.Builders;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Tests.Strategies;

public class CustomRuleBasedStrategyTests
{
    private readonly IIndicatorCalculator _indicatorCalculator;

    public CustomRuleBasedStrategyTests()
    {
        _indicatorCalculator = new IndicatorCalculator();
    }

    [Fact]
    public void Constructor_WithValidDefinition_ShouldCreateStrategy()
    {
        // Arrange
        StrategyDefinition definition = CreateSimpleRSIDefinition();

        // Act
        var strategy = new CustomRuleBasedStrategy(
            _indicatorCalculator,
            definition,
            "Test Strategy",
            "Test description");

        // Assert
        strategy.ShouldNotBeNull();
        strategy.Name.ShouldBe("Test Strategy");
        strategy.Description.ShouldBe("Test description");
    }

    [Fact]
    public void GenerateSignal_WithSingleRSIRule_ReturnsBuyWhenConditionMet()
    {
        // Arrange - Strategy: Buy when RSI < 30
        StrategyDefinition definition = new(
            EntryRules: new List<StrategyRule>
            {
                new StrategyRule(
                    IndicatorName: "RSI",
                    IndicatorParameters: new Dictionary<string, object> { ["Period"] = 14 },
                    Operator: ComparisonOperator.LessThan,
                    ValueType: RuleValueType.Constant,
                    ConstantValue: 30,
                    SecondIndicatorName: null,
                    SecondIndicatorParameters: null,
                    LogicalOperator: LogicalOperator.None
                )
            },
            ExitRules: new List<StrategyRule>
            {
                new StrategyRule(
                    IndicatorName: "RSI",
                    IndicatorParameters: new Dictionary<string, object> { ["Period"] = 14 },
                    Operator: ComparisonOperator.GreaterThan,
                    ValueType: RuleValueType.Constant,
                    ConstantValue: 70,
                    SecondIndicatorName: null,
                    SecondIndicatorParameters: null,
                    LogicalOperator: LogicalOperator.None
                )
            },
            SizingMode: PositionSizingMode.FixedPercentage,
            SizingParameters: new Dictionary<string, decimal> { ["Percentage"] = 0.95m }
        );

        // Create declining prices to get low RSI (oversold)
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(
                150m, 145m, 140m, 135m, 130m,
                125m, 120m, 115m, 110m, 105m,
                100m, 98m, 96m, 95m, 94m,
                93m, 92m, 91m, 90m, 89m)
            .Build();

        var strategy = new CustomRuleBasedStrategy(_indicatorCalculator, definition, "RSI Strategy", "Buy when RSI < 30");
        strategy.Initialize(prices.AsReadOnly());

        // Act
        TradeSignal signal = strategy.GenerateSignal(19, 10000m, 0);

        // Assert
        signal.Type.ShouldBe(SignalType.Buy);
        signal.Quantity.ShouldBeGreaterThan(0);
        signal.Reason.ShouldContain("Entry triggered");
    }

    [Fact]
    public void GenerateSignal_WithMultipleANDRules_RequiresAllConditions()
    {
        // Arrange - Strategy: Buy when RSI < 30 AND Price > SMA(20)
        StrategyDefinition definition = new(
            EntryRules: new List<StrategyRule>
            {
                new StrategyRule(
                    "RSI",
                    new Dictionary<string, object> { ["Period"] = 14 },
                    ComparisonOperator.LessThan,
                    RuleValueType.Constant,
                    30,
                    null,
                    null,
                    LogicalOperator.And
                ),
                new StrategyRule(
                    "SMA",
                    new Dictionary<string, object> { ["Period"] = 5 },
                    ComparisonOperator.LessThan,
                    RuleValueType.Price,
                    null,
                    null,
                    null,
                    LogicalOperator.None
                )
            },
            ExitRules: new List<StrategyRule>
            {
                new StrategyRule(
                    "RSI",
                    new Dictionary<string, object> { ["Period"] = 14 },
                    ComparisonOperator.GreaterThan,
                    RuleValueType.Constant,
                    70,
                    null,
                    null,
                    LogicalOperator.None
                )
            },
            SizingMode: PositionSizingMode.FixedPercentage,
            SizingParameters: new Dictionary<string, decimal> { ["Percentage"] = 0.95m }
        );

        // Create prices that satisfy both conditions
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(
                100m, 95m, 90m, 85m, 80m,      // Declining (low RSI)
                75m, 70m, 68m, 66m, 65m,
                64m, 63m, 62m, 61m, 60m,
                60m, 61m, 62m, 64m, 66m)       // Recovery (price above recent SMA)
            .Build();

        var strategy = new CustomRuleBasedStrategy(_indicatorCalculator, definition, "Multi-Condition", "RSI + SMA");
        strategy.Initialize(prices.AsReadOnly());

        // Act
        TradeSignal signal = strategy.GenerateSignal(19, 10000m, 0);

        // Assert - Should buy because both conditions are met
        signal.Type.ShouldBe(SignalType.Buy);
        signal.Quantity.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void GenerateSignal_WithORRules_RequiresAnyCondition()
    {
        // Arrange - Strategy: Buy when RSI < 30 OR SMA(5) < SMA(20)
        StrategyDefinition definition = new(
            EntryRules: new List<StrategyRule>
            {
                new StrategyRule(
                    "RSI",
                    new Dictionary<string, object> { ["Period"] = 14 },
                    ComparisonOperator.LessThan,
                    RuleValueType.Constant,
                    30,
                    null,
                    null,
                    LogicalOperator.Or
                ),
                new StrategyRule(
                    "SMA",
                    new Dictionary<string, object> { ["Period"] = 5 },
                    ComparisonOperator.LessThan,
                    RuleValueType.Indicator,
                    null,
                    "SMA",
                    new Dictionary<string, object> { ["Period"] = 20 },
                    LogicalOperator.None
                )
            },
            ExitRules: new List<StrategyRule>
            {
                new StrategyRule(
                    "RSI",
                    new Dictionary<string, object> { ["Period"] = 14 },
                    ComparisonOperator.GreaterThan,
                    RuleValueType.Constant,
                    70,
                    null,
                    null,
                    LogicalOperator.None
                )
            },
            SizingMode: PositionSizingMode.FixedPercentage,
            SizingParameters: new Dictionary<string, decimal> { ["Percentage"] = 0.95m }
        );

        // Create prices where SMA(5) < SMA(20) (downtrend) even if RSI is not oversold
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(
                100m, 99m, 98m, 97m, 96m,
                95m, 94m, 93m, 92m, 91m,
                90m, 89m, 88m, 87m, 86m,
                85m, 84m, 83m, 82m, 81m,
                80m, 79m, 78m, 77m, 76m)
            .Build();

        var strategy = new CustomRuleBasedStrategy(_indicatorCalculator, definition, "OR Strategy", "RSI OR MA");
        strategy.Initialize(prices.AsReadOnly());

        // Act
        TradeSignal signal = strategy.GenerateSignal(24, 10000m, 0);

        // Assert - Should buy because at least one condition is met (SMA crossover)
        signal.Type.ShouldBe(SignalType.Buy);
    }

    [Fact]
    public void GenerateSignal_WithCrossesAboveOperator_DetectsCrossover()
    {
        // Arrange - Strategy: Buy when RSI crosses above 30
        StrategyDefinition definition = new(
            EntryRules: new List<StrategyRule>
            {
                new StrategyRule(
                    "RSI",
                    new Dictionary<string, object> { ["Period"] = 14 },
                    ComparisonOperator.CrossesAbove,
                    RuleValueType.Constant,
                    30,
                    null,
                    null,
                    LogicalOperator.None
                )
            },
            ExitRules: new List<StrategyRule>
            {
                new StrategyRule(
                    "RSI",
                    new Dictionary<string, object> { ["Period"] = 14 },
                    ComparisonOperator.GreaterThan,
                    RuleValueType.Constant,
                    70,
                    null,
                    null,
                    LogicalOperator.None
                )
            },
            SizingMode: PositionSizingMode.FixedPercentage,
            SizingParameters: new Dictionary<string, decimal> { ["Percentage"] = 0.95m }
        );

        // Create prices with RSI crossing above 30
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(
                150m, 145m, 140m, 135m, 130m,
                125m, 120m, 115m, 110m, 105m,
                100m, 98m, 96m, 95m, 94m,      // Deep oversold
                96m, 98m, 101m, 104m, 108m)    // Recovery - RSI crosses above 30
            .Build();

        var strategy = new CustomRuleBasedStrategy(_indicatorCalculator, definition, "Crossover", "RSI Crossover");
        strategy.Initialize(prices.AsReadOnly());

        // Act
        TradeSignal signal = strategy.GenerateSignal(19, 10000m, 0);

        // Assert - Should detect crossover
        signal.Type.ShouldBe(SignalType.Buy);
    }

    [Fact]
    public void GenerateSignal_WithCrossesAboveOperator_IgnoresNonCrossover()
    {
        // Arrange - Same strategy as above
        StrategyDefinition definition = new(
            EntryRules: new List<StrategyRule>
            {
                new StrategyRule(
                    "RSI",
                    new Dictionary<string, object> { ["Period"] = 14 },
                    ComparisonOperator.CrossesAbove,
                    RuleValueType.Constant,
                    30,
                    null,
                    null,
                    LogicalOperator.None
                )
            },
            ExitRules: new List<StrategyRule>
            {
                new StrategyRule(
                    "RSI",
                    new Dictionary<string, object> { ["Period"] = 14 },
                    ComparisonOperator.GreaterThan,
                    RuleValueType.Constant,
                    70,
                    null,
                    null,
                    LogicalOperator.None
                )
            },
            SizingMode: PositionSizingMode.FixedPercentage,
            SizingParameters: new Dictionary<string, decimal> { ["Percentage"] = 0.95m }
        );

        // Create prices where RSI stays above 30 (no crossover)
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(
                100m, 101m, 102m, 103m, 104m,
                105m, 106m, 107m, 108m, 109m,
                110m, 111m, 112m, 113m, 114m,
                115m, 116m, 117m, 118m, 119m)
            .Build();

        var strategy = new CustomRuleBasedStrategy(_indicatorCalculator, definition, "No Cross", "No Crossover");
        strategy.Initialize(prices.AsReadOnly());

        // Act
        TradeSignal signal = strategy.GenerateSignal(19, 10000m, 0);

        // Assert - Should NOT generate buy signal (no crossover)
        signal.Type.ShouldBe(SignalType.Hold);
    }

    [Fact]
    public void GenerateSignal_ComparingTwoIndicators_EvaluatesCorrectly()
    {
        // Arrange - Strategy: Buy when SMA(5) > SMA(20) (simplified - not crossover)
        StrategyDefinition definition = new(
            EntryRules: new List<StrategyRule>
            {
                new StrategyRule(
                    "SMA",
                    new Dictionary<string, object> { ["Period"] = 5 },
                    ComparisonOperator.GreaterThan,
                    RuleValueType.Indicator,
                    null,
                    "SMA",
                    new Dictionary<string, object> { ["Period"] = 20 },
                    LogicalOperator.None
                )
            },
            ExitRules: new List<StrategyRule>
            {
                new StrategyRule(
                    "SMA",
                    new Dictionary<string, object> { ["Period"] = 5 },
                    ComparisonOperator.LessThan,
                    RuleValueType.Indicator,
                    null,
                    "SMA",
                    new Dictionary<string, object> { ["Period"] = 20 },
                    LogicalOperator.None
                )
            },
            SizingMode: PositionSizingMode.FixedPercentage,
            SizingParameters: new Dictionary<string, decimal> { ["Percentage"] = 0.95m }
        );

        // Create prices in uptrend (fast SMA will be above slow SMA)
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(
                80m, 82m, 84m, 86m, 88m,       // Uptrend begins
                90m, 92m, 94m, 96m, 98m,
                100m, 102m, 104m, 106m, 108m,
                110m, 112m, 114m, 116m, 118m,
                120m, 122m, 124m, 126m, 128m)
            .Build();

        var strategy = new CustomRuleBasedStrategy(_indicatorCalculator, definition, "MA Cross", "SMA Crossover");
        strategy.Initialize(prices.AsReadOnly());

        // Act - Check near the end where fast SMA should be well above slow SMA
        TradeSignal signal = strategy.GenerateSignal(24, 10000m, 0);

        // Assert
        signal.Type.ShouldBe(SignalType.Buy);
    }

    [Fact]
    public void GenerateSignal_WithPriceComparison_UsesCurrentPrice()
    {
        // Arrange - Strategy: Buy when Price > SMA(20)
        StrategyDefinition definition = new(
            EntryRules: new List<StrategyRule>
            {
                new StrategyRule(
                    "SMA",
                    new Dictionary<string, object> { ["Period"] = 20 },
                    ComparisonOperator.LessThan,
                    RuleValueType.Price,
                    null,
                    null,
                    null,
                    LogicalOperator.None
                )
            },
            ExitRules: new List<StrategyRule>
            {
                new StrategyRule(
                    "SMA",
                    new Dictionary<string, object> { ["Period"] = 20 },
                    ComparisonOperator.GreaterThan,
                    RuleValueType.Price,
                    null,
                    null,
                    null,
                    LogicalOperator.None
                )
            },
            SizingMode: PositionSizingMode.FixedPercentage,
            SizingParameters: new Dictionary<string, decimal> { ["Percentage"] = 0.95m }
        );

        // Create prices in uptrend (price above MA)
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(
                90m, 92m, 94m, 96m, 98m,
                100m, 102m, 104m, 106m, 108m,
                110m, 112m, 114m, 116m, 118m,
                120m, 122m, 124m, 126m, 128m,
                130m, 132m, 134m, 136m, 138m)
            .Build();

        var strategy = new CustomRuleBasedStrategy(_indicatorCalculator, definition, "Price > MA", "Trend Following");
        strategy.Initialize(prices.AsReadOnly());

        // Act
        TradeSignal signal = strategy.GenerateSignal(24, 10000m, 0);

        // Assert
        signal.Type.ShouldBe(SignalType.Buy);
    }

    [Fact]
    public void Initialize_PreCalculatesAllIndicators_CachesResults()
    {
        // Arrange
        StrategyDefinition definition = CreateSimpleRSIDefinition();
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 101m, 102m, 103m, 104m, 105m, 106m, 107m, 108m, 109m,
                       110m, 111m, 112m, 113m, 114m, 115m, 116m, 117m, 118m, 119m)
            .Build();

        var strategy = new CustomRuleBasedStrategy(_indicatorCalculator, definition, "Cache Test", "Test caching");

        // Act
        strategy.Initialize(prices.AsReadOnly());

        // Assert - No exception thrown, indicators cached
        // Verify by generating multiple signals (should use cached values)
        TradeSignal signal1 = strategy.GenerateSignal(15, 10000m, 0);
        TradeSignal signal2 = strategy.GenerateSignal(16, 10000m, 0);
        TradeSignal signal3 = strategy.GenerateSignal(17, 10000m, 0);

        // Should complete without errors
        signal1.ShouldNotBeNull();
        signal2.ShouldNotBeNull();
        signal3.ShouldNotBeNull();
    }

    [Fact]
    public void GenerateSignal_WithInsufficientData_ReturnsHold()
    {
        // Arrange
        StrategyDefinition definition = CreateSimpleRSIDefinition();
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 101m, 102m)  // Only 3 prices (not enough for RSI 14)
            .Build();

        var strategy = new CustomRuleBasedStrategy(_indicatorCalculator, definition, "Insufficient", "Not enough data");
        strategy.Initialize(prices.AsReadOnly());

        // Act
        TradeSignal signal = strategy.GenerateSignal(2, 10000m, 0);

        // Assert
        signal.Type.ShouldBe(SignalType.Hold);
    }

    [Fact]
    public void GenerateSignal_WithFixedQuantitySizing_ReturnsFixedQuantity()
    {
        // Arrange
        StrategyDefinition definition = new(
            EntryRules: new List<StrategyRule>
            {
                new StrategyRule(
                    "RSI",
                    new Dictionary<string, object> { ["Period"] = 14 },
                    ComparisonOperator.LessThan,
                    RuleValueType.Constant,
                    30,
                    null,
                    null,
                    LogicalOperator.None
                )
            },
            ExitRules: new List<StrategyRule>
            {
                new StrategyRule(
                    "RSI",
                    new Dictionary<string, object> { ["Period"] = 14 },
                    ComparisonOperator.GreaterThan,
                    RuleValueType.Constant,
                    70,
                    null,
                    null,
                    LogicalOperator.None
                )
            },
            SizingMode: PositionSizingMode.FixedQuantity,
            SizingParameters: new Dictionary<string, decimal> { ["Quantity"] = 50 }
        );

        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(
                150m, 145m, 140m, 135m, 130m,
                125m, 120m, 115m, 110m, 105m,
                100m, 98m, 96m, 95m, 94m,
                93m, 92m, 91m, 90m, 89m)
            .Build();

        var strategy = new CustomRuleBasedStrategy(_indicatorCalculator, definition, "Fixed Qty", "Fixed quantity sizing");
        strategy.Initialize(prices.AsReadOnly());

        // Act
        TradeSignal signal = strategy.GenerateSignal(19, 10000m, 0);

        // Assert
        signal.Type.ShouldBe(SignalType.Buy);
        signal.Quantity.ShouldBe(50);
    }

    [Fact]
    public void GenerateSignal_WithExitConditionMet_ReturnsSellSignal()
    {
        // Arrange
        StrategyDefinition definition = CreateSimpleRSIDefinition();

        // Create prices with high RSI (overbought)
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(
                50m, 55m, 60m, 65m, 70m,
                75m, 80m, 85m, 90m, 95m,
                100m, 102m, 104m, 105m, 106m,
                107m, 108m, 109m, 110m, 111m)
            .Build();

        var strategy = new CustomRuleBasedStrategy(_indicatorCalculator, definition, "Exit Test", "Test exit");
        strategy.Initialize(prices.AsReadOnly());

        // Act - Test with existing position
        TradeSignal signal = strategy.GenerateSignal(19, 0m, 100);

        // Assert
        signal.Type.ShouldBe(SignalType.Sell);
        signal.Quantity.ShouldBe(100);
        signal.Reason.ShouldContain("Exit triggered");
    }

    [Fact]
    public void GetParameters_ReturnsCorrectMetadata()
    {
        // Arrange
        StrategyDefinition definition = CreateSimpleRSIDefinition();
        var strategy = new CustomRuleBasedStrategy(_indicatorCalculator, definition, "Params Test", "Test parameters");

        // Act
        Dictionary<string, object> parameters = strategy.GetParameters();

        // Assert
        parameters.ShouldContainKey("EntryRuleCount");
        parameters.ShouldContainKey("ExitRuleCount");
        parameters.ShouldContainKey("SizingMode");
        parameters["EntryRuleCount"].ShouldBe(1);
        parameters["ExitRuleCount"].ShouldBe(1);
        parameters["SizingMode"].ShouldBe("FixedPercentage");
    }

    #region Helper Methods

    private StrategyDefinition CreateSimpleRSIDefinition()
    {
        return new StrategyDefinition(
            EntryRules: new List<StrategyRule>
            {
                new StrategyRule(
                    "RSI",
                    new Dictionary<string, object> { ["Period"] = 14 },
                    ComparisonOperator.LessThan,
                    RuleValueType.Constant,
                    30,
                    null,
                    null,
                    LogicalOperator.None
                )
            },
            ExitRules: new List<StrategyRule>
            {
                new StrategyRule(
                    "RSI",
                    new Dictionary<string, object> { ["Period"] = 14 },
                    ComparisonOperator.GreaterThan,
                    RuleValueType.Constant,
                    70,
                    null,
                    null,
                    LogicalOperator.None
                )
            },
            SizingMode: PositionSizingMode.FixedPercentage,
            SizingParameters: new Dictionary<string, decimal> { ["Percentage"] = 0.95m }
        );
    }

    #endregion

    #region Indicator Caching Verification Tests

    [Fact]
    public void CachingVerification_SameIndicatorUsedTwice_CalculatedOnlyOnce()
    {
        // Arrange - Strategy with 3 rules all using RSI(14)
        IIndicatorCalculator fakeCalculator = A.Fake<IIndicatorCalculator>();
        decimal[] rsiValues = new decimal[100];
        for (int i = 0; i < 100; i++)
        {
            rsiValues[i] = 50m + (i % 20); // Generate test data
        }

        A.CallTo(() => fakeCalculator.CalculateRSI(A<decimal[]>._, 14))
            .Returns(rsiValues);

        StrategyDefinition definition = new(
            EntryRules: new List<StrategyRule>
            {
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.LessThan,
                    RuleValueType.Constant, 30, null, null, LogicalOperator.And),
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.GreaterThan,
                    RuleValueType.Constant, 20, null, null, LogicalOperator.None)
            },
            ExitRules: new List<StrategyRule>
            {
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.GreaterThan,
                    RuleValueType.Constant, 70, null, null, LogicalOperator.None)
            },
            SizingMode: PositionSizingMode.FixedPercentage,
            SizingParameters: new() { ["Percentage"] = 0.1m }
        );

        var strategy = new CustomRuleBasedStrategy(
            fakeCalculator,
            definition,
            "Caching Test",
            "Tests indicator caching");

        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(Enumerable.Range(0, 100).Select(i => 100m + i).ToArray())
            .Build();

        // Act
        strategy.Initialize(prices);

        // Assert - Should only call CalculateRSI once despite 3 rules using RSI(14)
        A.CallTo(() => fakeCalculator.CalculateRSI(A<decimal[]>._, 14))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void Initialize_WithDifferentParameters_CreatesSeparateCacheEntries()
    {
        // Arrange - Strategy with RSI(14) and RSI(21)
        IIndicatorCalculator fakeCalculator = A.Fake<IIndicatorCalculator>();
        decimal[] rsi14 = new decimal[100];
        decimal[] rsi21 = new decimal[100];

        for (int i = 0; i < 100; i++)
        {
            rsi14[i] = 40m + (i % 20);
            rsi21[i] = 50m + (i % 20);
        }

        A.CallTo(() => fakeCalculator.CalculateRSI(A<decimal[]>._, 14))
            .Returns(rsi14);
        A.CallTo(() => fakeCalculator.CalculateRSI(A<decimal[]>._, 21))
            .Returns(rsi21);

        StrategyDefinition definition = new(
            EntryRules: new List<StrategyRule>
            {
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.LessThan,
                    RuleValueType.Constant, 30, null, null, LogicalOperator.And),
                new("RSI", new() { ["Period"] = 21 }, ComparisonOperator.LessThan,
                    RuleValueType.Constant, 30, null, null, LogicalOperator.None)
            },
            ExitRules: new List<StrategyRule>(),
            SizingMode: PositionSizingMode.FixedPercentage,
            SizingParameters: new() { ["Percentage"] = 0.1m }
        );

        var strategy = new CustomRuleBasedStrategy(
            fakeCalculator,
            definition,
            "Multi-param Test",
            "Tests different parameters");

        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(Enumerable.Range(0, 100).Select(i => 100m + i).ToArray())
            .Build();

        // Act
        strategy.Initialize(prices);

        // Assert - Should call CalculateRSI twice (once for each parameter set)
        A.CallTo(() => fakeCalculator.CalculateRSI(A<decimal[]>._, 14))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => fakeCalculator.CalculateRSI(A<decimal[]>._, 21))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void ReInitialize_ClearsCacheAndRecalculates()
    {
        // Arrange
        IIndicatorCalculator fakeCalculator = A.Fake<IIndicatorCalculator>();
        decimal[] rsiValues = new decimal[100];
        for (int i = 0; i < 100; i++)
        {
            rsiValues[i] = 50m + (i % 20);
        }

        A.CallTo(() => fakeCalculator.CalculateRSI(A<decimal[]>._, 14))
            .Returns(rsiValues);

        StrategyDefinition definition = CreateSimpleRSIDefinition();
        var strategy = new CustomRuleBasedStrategy(
            fakeCalculator,
            definition,
            "Re-init Test",
            "Tests cache clearing");

        List<HistoricalPrice> prices1 = HistoricalPriceBuilder.Create()
            .WithPrices(Enumerable.Range(0, 100).Select(i => 100m + i).ToArray())
            .Build();
        List<HistoricalPrice> prices2 = HistoricalPriceBuilder.Create()
            .WithPrices(Enumerable.Range(0, 100).Select(i => 150m + i).ToArray())
            .Build();

        // Act - Initialize twice with different data
        strategy.Initialize(prices1);
        strategy.Initialize(prices2);

        // Assert - Should call CalculateRSI twice (once per initialization)
        A.CallTo(() => fakeCalculator.CalculateRSI(A<decimal[]>._, 14))
            .MustHaveHappenedTwiceExactly();
    }

    [Fact]
    public void GenerateSignal_UsesCachedIndicators_DoesNotRecalculate()
    {
        // Arrange
        IIndicatorCalculator fakeCalculator = A.Fake<IIndicatorCalculator>();
        decimal[] rsiValues = new decimal[100];
        for (int i = 0; i < 100; i++)
        {
            rsiValues[i] = 25m; // Below 30 = oversold
        }

        A.CallTo(() => fakeCalculator.CalculateRSI(A<decimal[]>._, 14))
            .Returns(rsiValues);

        StrategyDefinition definition = CreateSimpleRSIDefinition();
        var strategy = new CustomRuleBasedStrategy(
            fakeCalculator,
            definition,
            "Signal Test",
            "Tests caching during signals");

        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(Enumerable.Range(0, 100).Select(i => 100m + i).ToArray())
            .Build();

        strategy.Initialize(prices.AsReadOnly());

        // Reset call count after initialization
        Fake.ClearRecordedCalls(fakeCalculator);

        // Act - Generate multiple signals
        strategy.GenerateSignal(50, 10000m, 0);
        strategy.GenerateSignal(51, 10000m, 0);
        strategy.GenerateSignal(52, 10000m, 0);

        // Assert - Should NOT call CalculateRSI again (uses cached values)
        A.CallTo(() => fakeCalculator.CalculateRSI(A<decimal[]>._, A<int>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public void Initialize_WithTwoIndicatorComparison_CachesBothIndicators()
    {
        // Arrange - Strategy comparing SMA(20) vs SMA(50)
        IIndicatorCalculator fakeCalculator = A.Fake<IIndicatorCalculator>();
        decimal[] sma20 = new decimal[100];
        decimal[] sma50 = new decimal[100];

        for (int i = 0; i < 100; i++)
        {
            sma20[i] = 100m + i;
            sma50[i] = 105m + i;
        }

        A.CallTo(() => fakeCalculator.CalculateSMA(A<decimal[]>._, 20))
            .Returns(sma20);
        A.CallTo(() => fakeCalculator.CalculateSMA(A<decimal[]>._, 50))
            .Returns(sma50);

        StrategyDefinition definition = new(
            EntryRules: new List<StrategyRule>
            {
                new("SMA", new() { ["Period"] = 20 },
                    ComparisonOperator.GreaterThan,
                    RuleValueType.Indicator,
                    null,
                    "SMA",
                    new() { ["Period"] = 50 },
                    LogicalOperator.None)
            },
            ExitRules: new List<StrategyRule>(),
            SizingMode: PositionSizingMode.FixedPercentage,
            SizingParameters: new() { ["Percentage"] = 0.1m }
        );

        var strategy = new CustomRuleBasedStrategy(
            fakeCalculator,
            definition,
            "Two Indicator Test",
            "Tests two-indicator caching");

        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(Enumerable.Range(0, 100).Select(i => 100m + i).ToArray())
            .Build();

        // Act
        strategy.Initialize(prices);

        // Assert - Should call both SMA calculations once each
        A.CallTo(() => fakeCalculator.CalculateSMA(A<decimal[]>._, 20))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => fakeCalculator.CalculateSMA(A<decimal[]>._, 50))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void CacheKey_WithSameIndicatorDifferentParams_CreatesUniqueCacheEntries()
    {
        // Arrange - Multiple SMA with different periods
        IIndicatorCalculator fakeCalculator = A.Fake<IIndicatorCalculator>();

        A.CallTo(() => fakeCalculator.CalculateSMA(A<decimal[]>._, 10))
            .Returns(new decimal[100]);
        A.CallTo(() => fakeCalculator.CalculateSMA(A<decimal[]>._, 20))
            .Returns(new decimal[100]);
        A.CallTo(() => fakeCalculator.CalculateSMA(A<decimal[]>._, 50))
            .Returns(new decimal[100]);

        StrategyDefinition definition = new(
            EntryRules: new List<StrategyRule>
            {
                new("SMA", new() { ["Period"] = 10 }, ComparisonOperator.GreaterThan,
                    RuleValueType.Constant, 100, null, null, LogicalOperator.And),
                new("SMA", new() { ["Period"] = 20 }, ComparisonOperator.GreaterThan,
                    RuleValueType.Constant, 100, null, null, LogicalOperator.And),
                new("SMA", new() { ["Period"] = 50 }, ComparisonOperator.GreaterThan,
                    RuleValueType.Constant, 100, null, null, LogicalOperator.None)
            },
            ExitRules: new List<StrategyRule>(),
            SizingMode: PositionSizingMode.FixedPercentage,
            SizingParameters: new() { ["Percentage"] = 0.1m }
        );

        var strategy = new CustomRuleBasedStrategy(
            fakeCalculator,
            definition,
            "Multiple SMA Test",
            "Tests unique cache keys");

        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(Enumerable.Range(0, 100).Select(i => 100m + i).ToArray())
            .Build();

        // Act
        strategy.Initialize(prices);

        // Assert - Should call CalculateSMA three times (once for each unique parameter set)
        A.CallTo(() => fakeCalculator.CalculateSMA(A<decimal[]>._, 10))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => fakeCalculator.CalculateSMA(A<decimal[]>._, 20))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => fakeCalculator.CalculateSMA(A<decimal[]>._, 50))
            .MustHaveHappenedOnceExactly();
    }

    #endregion
}
