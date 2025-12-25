using FakeItEasy;
using Shouldly;
using TradingStrat.Application.Factories;
using TradingStrat.Application.Strategies;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.Strategies;

namespace TradingStrat.Application.Tests.Factories;

public class StrategyFactoryTests
{
    private readonly IIndicatorCalculator _fakeIndicatorCalculator;
    private readonly IMLPredictionService _fakeMLPredictionService;
    private readonly IStrategyRegistry _registry;
    private readonly StrategyFactory _factory;

    public StrategyFactoryTests()
    {
        _fakeIndicatorCalculator = A.Fake<IIndicatorCalculator>();
        _fakeMLPredictionService = A.Fake<IMLPredictionService>();

        // Use real registry for tests (it's immutable metadata, no need to fake)
        _registry = new StrategyRegistry();
        _factory = new StrategyFactory(_fakeIndicatorCalculator, _registry, _fakeMLPredictionService);
    }

    #region Strategy Creation Tests

    [Fact]
    public void CreateStrategy_WithMA_CreatesMovingAverageCrossoverStrategy()
    {
        // Act
        IStrategy strategy = _factory.CreateStrategy(StrategyType.MovingAverageCrossover);

        // Assert
        strategy.ShouldBeOfType<MovingAverageCrossoverStrategy>();
        strategy.Name.ShouldContain("MA Crossover");
    }

    [Fact]
    public void CreateStrategy_WithRSI_CreatesRSIStrategy()
    {
        // Act
        IStrategy strategy = _factory.CreateStrategy(StrategyType.RSI);

        // Assert
        strategy.ShouldBeOfType<RSIStrategy>();
        strategy.Name.ShouldContain("RSI");
    }

    [Fact]
    public void CreateStrategy_WithMACD_CreatesMACDStrategy()
    {
        // Act
        IStrategy strategy = _factory.CreateStrategy(StrategyType.MACD);

        // Assert
        strategy.ShouldBeOfType<MACDStrategy>();
        strategy.Name.ShouldContain("MACD");
    }

    [Fact]
    public void CreateStrategy_WithML_CreatesMachineLearningStrategy()
    {
        // Act
        IStrategy strategy = _factory.CreateStrategy(StrategyType.MachineLearning);

        // Assert
        strategy.ShouldBeOfType<MachineLearningStrategy>();
        strategy.Name.ShouldContain("ML");
    }

    [Fact]
    public void CreateStrategy_WithIchimoku_CreatesIchimokuStrategy()
    {
        // Act
        IStrategy strategy = _factory.CreateStrategy(StrategyType.Ichimoku);

        // Assert
        strategy.ShouldBeOfType<IchimokuStrategy>();
        strategy.Name.ShouldContain("Ichimoku");
    }

    [Fact]
    public void CreateStrategy_WithNullParameters_UsesDefaults()
    {
        // Act
        IStrategy strategy = _factory.CreateStrategy(StrategyType.MovingAverageCrossover);

        // Assert
        strategy.ShouldNotBeNull();
        strategy.Name.ShouldBe("MA Crossover (20/50)"); // Default parameters
    }

    [Fact]
    public void CreateStrategy_WithEmptyParameters_UsesDefaults()
    {
        // Act
        IStrategy strategy = _factory.CreateStrategy(StrategyType.RSI, new Dictionary<string, object>());

        // Assert
        strategy.ShouldNotBeNull();
        strategy.Name.ShouldBe("RSI (14, 30/70)"); // Default parameters
    }

    [Fact]
    public void CreateStrategy_InjectsIndicatorCalculator()
    {
        // Act
        IStrategy strategy = _factory.CreateStrategy(StrategyType.MovingAverageCrossover);

        // Assert
        strategy.ShouldNotBeNull();
        // IndicatorCalculator should be injected (verified by successful creation)
    }

    #endregion

    #region Parameter Handling Tests

    [Fact]
    public void CreateStrategy_MA_WithCustomParameters_UsesProvidedValues()
    {
        // Arrange
        Dictionary<string, object> parameters = new()
        {
            { "FastPeriod", 10 },
            { "SlowPeriod", 30 }
        };

        // Act
        IStrategy strategy = _factory.CreateStrategy(StrategyType.MovingAverageCrossover, parameters);

        // Assert
        strategy.Name.ShouldBe("MA Crossover (10/30)");
    }

    [Fact]
    public void CreateStrategy_RSI_WithCustomParameters_UsesProvidedValues()
    {
        // Arrange
        Dictionary<string, object> parameters = new()
        {
            { "Period", 9 },
            { "OversoldThreshold", 20m },
            { "OverboughtThreshold", 80m }
        };

        // Act
        IStrategy strategy = _factory.CreateStrategy(StrategyType.RSI, parameters);

        // Assert
        strategy.Name.ShouldBe("RSI (9, 20/80)");
    }

    [Fact]
    public void CreateStrategy_MACD_WithCustomParameters_UsesProvidedValues()
    {
        // Arrange
        Dictionary<string, object> parameters = new()
        {
            { "FastPeriod", 8 },
            { "SlowPeriod", 21 },
            { "SignalPeriod", 5 }
        };

        // Act
        IStrategy strategy = _factory.CreateStrategy(StrategyType.MACD, parameters);

        // Assert
        strategy.Name.ShouldBe("MACD (8/21/5)");
    }

    [Fact]
    public void CreateStrategy_ML_WithCustomThresholds_UsesProvidedValues()
    {
        // Arrange
        Dictionary<string, object> parameters = new()
        {
            { "BuyThreshold", 0.02m },
            { "SellThreshold", -0.02m }
        };

        // Act
        IStrategy strategy = _factory.CreateStrategy(StrategyType.MachineLearning, parameters);

        // Assert
        strategy.ShouldBeOfType<MachineLearningStrategy>();
        Dictionary<string, object> strategyParams = strategy.GetParameters();
        strategyParams["BuyThreshold"].ShouldBe(0.02m);
        strategyParams["SellThreshold"].ShouldBe(-0.02m);
    }

    [Fact]
    public void CreateStrategy_WithInvalidParameterType_UsesDefault()
    {
        // Arrange - Provide a string where an int is expected
        Dictionary<string, object> parameters = new()
        {
            { "FastPeriod", "invalid" },
            { "SlowPeriod", 30 }
        };

        // Act
        IStrategy strategy = _factory.CreateStrategy(StrategyType.MovingAverageCrossover, parameters);

        // Assert
        // Should use default for FastPeriod (20) and provided SlowPeriod (30)
        strategy.Name.ShouldBe("MA Crossover (20/30)");
    }

    [Fact]
    public void CreateStrategy_WithMissingParameter_UsesDefault()
    {
        // Arrange - Only provide one parameter
        Dictionary<string, object> parameters = new()
        {
            { "FastPeriod", 15 }
        };

        // Act
        IStrategy strategy = _factory.CreateStrategy(StrategyType.MovingAverageCrossover, parameters);

        // Assert
        // Should use provided FastPeriod (15) and default SlowPeriod (50)
        strategy.Name.ShouldBe("MA Crossover (15/50)");
    }

    [Fact]
    public void CreateStrategy_WithExtraParameters_IgnoresThem()
    {
        // Arrange
        Dictionary<string, object> parameters = new()
        {
            { "FastPeriod", 10 },
            { "SlowPeriod", 30 },
            { "ExtraParameter", "ignored" },
            { "AnotherExtra", 999 }
        };

        // Act
        IStrategy strategy = _factory.CreateStrategy(StrategyType.MovingAverageCrossover, parameters);

        // Assert
        strategy.Name.ShouldBe("MA Crossover (10/30)");
    }

    [Fact]
    public void CreateStrategy_WithDecimalParameter_ConvertsCorrectly()
    {
        // Arrange - Test decimal conversion
        Dictionary<string, object> parameters = new()
        {
            { "OversoldThreshold", 25 }, // int to decimal
            { "OverboughtThreshold", 75m } // already decimal
        };

        // Act
        IStrategy strategy = _factory.CreateStrategy(StrategyType.RSI, parameters);

        // Assert
        strategy.Name.ShouldBe("RSI (14, 25/75)");
    }

    #endregion
}
