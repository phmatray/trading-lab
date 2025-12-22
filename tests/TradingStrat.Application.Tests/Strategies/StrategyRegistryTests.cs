using Shouldly;
using TradingStrat.Application.Strategies;
using TradingStrat.Domain.Strategies;

namespace TradingStrat.Application.Tests.Strategies;

public class StrategyRegistryTests
{
    private readonly IStrategyRegistry _registry;

    public StrategyRegistryTests()
    {
        _registry = new StrategyRegistry();
    }

    [Fact]
    public void GetAll_ReturnsAllFiveStrategies()
    {
        // Act
        IReadOnlyCollection<StrategyDescriptor> descriptors = _registry.GetAll();

        // Assert
        descriptors.Count.ShouldBe(5);
        descriptors.Select(d => d.Type).ShouldBe(
            [
                StrategyType.MovingAverageCrossover,
                StrategyType.RSI,
                StrategyType.MACD,
                StrategyType.MachineLearning,
                StrategyType.Ichimoku
            ],
            ignoreOrder: true);
    }

    [Theory]
    [InlineData(StrategyType.MovingAverageCrossover, "ma", "Moving Average Crossover")]
    [InlineData(StrategyType.RSI, "rsi", "RSI Strategy")]
    [InlineData(StrategyType.MACD, "macd", "MACD Strategy")]
    [InlineData(StrategyType.MachineLearning, "ml", "ML FastTree")]
    [InlineData(StrategyType.Ichimoku, "ichimoku", "Ichimoku Cloud")]
    public void GetDescriptor_WithValidType_ReturnsCorrectDescriptor(
        StrategyType type,
        string expectedKey,
        string expectedDisplayName)
    {
        // Act
        StrategyDescriptor descriptor = _registry.GetDescriptor(type);

        // Assert
        descriptor.ShouldNotBeNull();
        descriptor.Type.ShouldBe(type);
        descriptor.Key.ShouldBe(expectedKey);
        descriptor.DisplayName.ShouldBe(expectedDisplayName);
        descriptor.Description.ShouldNotBeNullOrWhiteSpace();
        descriptor.Parameters.ShouldNotBeNull();
        descriptor.Parameters.Count.ShouldBeGreaterThan(0);
    }

    [Theory]
    [InlineData("ma", StrategyType.MovingAverageCrossover)]
    [InlineData("movingaverage", StrategyType.MovingAverageCrossover)]
    [InlineData("macrossover", StrategyType.MovingAverageCrossover)]
    [InlineData("rsi", StrategyType.RSI)]
    [InlineData("macd", StrategyType.MACD)]
    [InlineData("ml", StrategyType.MachineLearning)]
    [InlineData("machinelearning", StrategyType.MachineLearning)]
    [InlineData("ichimoku", StrategyType.Ichimoku)]
    [InlineData("ichi", StrategyType.Ichimoku)]
    public void ParseStrategyType_WithValidKey_ReturnsCorrectType(string key, StrategyType expectedType)
    {
        // Act
        StrategyType result = _registry.ParseStrategyType(key);

        // Assert
        result.ShouldBe(expectedType);
    }

    [Theory]
    [InlineData("MA", StrategyType.MovingAverageCrossover)]
    [InlineData("Ma", StrategyType.MovingAverageCrossover)]
    [InlineData("  ma  ", StrategyType.MovingAverageCrossover)]
    [InlineData("RSI", StrategyType.RSI)]
    [InlineData("  ML  ", StrategyType.MachineLearning)]
    public void ParseStrategyType_IsCaseInsensitive(string key, StrategyType expectedType)
    {
        // Act
        StrategyType result = _registry.ParseStrategyType(key);

        // Assert
        result.ShouldBe(expectedType);
    }

    [Fact]
    public void ParseStrategyType_WithInvalidKey_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException ex = Should.Throw<ArgumentException>(() =>
            _registry.ParseStrategyType("invalid"));

        ex.Message.ShouldContain("Unknown strategy key");
        ex.Message.ShouldContain("invalid");
    }

    [Theory]
    [InlineData("ma", true, StrategyType.MovingAverageCrossover)]
    [InlineData("rsi", true, StrategyType.RSI)]
    [InlineData("invalid", false, default(StrategyType))]
    [InlineData("", false, default(StrategyType))]
    public void TryParseStrategyType_ReturnsExpectedResult(
        string key,
        bool expectedSuccess,
        StrategyType expectedType)
    {
        // Act
        bool success = _registry.TryParseStrategyType(key, out StrategyType result);

        // Assert
        success.ShouldBe(expectedSuccess);
        result.ShouldBe(expectedType);
    }

    [Fact]
    public void GetDescriptor_MovingAverage_HasCorrectParameters()
    {
        // Act
        StrategyDescriptor descriptor = _registry.GetDescriptor(StrategyType.MovingAverageCrossover);

        // Assert
        descriptor.Parameters.Count.ShouldBe(2);
        descriptor.Parameters.ShouldContainKey("FastPeriod");
        descriptor.Parameters.ShouldContainKey("SlowPeriod");

        ParameterSchema fastPeriod = descriptor.Parameters["FastPeriod"];
        fastPeriod.ParameterType.ShouldBe(typeof(int));
        fastPeriod.DefaultValue.ShouldBe(20);
        fastPeriod.MinValue.ShouldBe(1);
        fastPeriod.MaxValue.ShouldBe(200);
    }

    [Fact]
    public void GetDescriptor_RSI_HasCorrectParameters()
    {
        // Act
        StrategyDescriptor descriptor = _registry.GetDescriptor(StrategyType.RSI);

        // Assert
        descriptor.Parameters.Count.ShouldBe(3);
        descriptor.Parameters.ShouldContainKey("Period");
        descriptor.Parameters.ShouldContainKey("OversoldThreshold");
        descriptor.Parameters.ShouldContainKey("OverboughtThreshold");

        ParameterSchema oversold = descriptor.Parameters["OversoldThreshold"];
        oversold.ParameterType.ShouldBe(typeof(decimal));
        oversold.DefaultValue.ShouldBe(30m);
    }

    [Fact]
    public void GetDescriptor_MACD_HasCorrectParameters()
    {
        // Act
        StrategyDescriptor descriptor = _registry.GetDescriptor(StrategyType.MACD);

        // Assert
        descriptor.Parameters.Count.ShouldBe(3);
        descriptor.Parameters.ShouldContainKey("FastPeriod");
        descriptor.Parameters.ShouldContainKey("SlowPeriod");
        descriptor.Parameters.ShouldContainKey("SignalPeriod");
    }

    [Fact]
    public void GetDescriptor_MachineLearning_HasCorrectParameters()
    {
        // Act
        StrategyDescriptor descriptor = _registry.GetDescriptor(StrategyType.MachineLearning);

        // Assert
        descriptor.Parameters.Count.ShouldBe(2);
        descriptor.Parameters.ShouldContainKey("BuyThreshold");
        descriptor.Parameters.ShouldContainKey("SellThreshold");

        ParameterSchema buyThreshold = descriptor.Parameters["BuyThreshold"];
        buyThreshold.ParameterType.ShouldBe(typeof(decimal));
        buyThreshold.DefaultValue.ShouldBe(0.01m);
        buyThreshold.Step.ShouldBe(0.001m);
    }

    [Fact]
    public void GetDescriptor_Ichimoku_HasCorrectParameters()
    {
        // Act
        StrategyDescriptor descriptor = _registry.GetDescriptor(StrategyType.Ichimoku);

        // Assert
        descriptor.Parameters.Count.ShouldBe(8);
        descriptor.Parameters.ShouldContainKey("TenkanPeriod");
        descriptor.Parameters.ShouldContainKey("KijunPeriod");
        descriptor.Parameters.ShouldContainKey("SenkouBPeriod");
        descriptor.Parameters.ShouldContainKey("Displacement");
        descriptor.Parameters.ShouldContainKey("ExitMode");
        descriptor.Parameters.ShouldContainKey("EntryMode");
        descriptor.Parameters.ShouldContainKey("CrossLookbackDays");
        descriptor.Parameters.ShouldContainKey("RiskPercentage");
    }

    [Fact]
    public void AllDescriptors_HaveCategories()
    {
        // Act
        IReadOnlyCollection<StrategyDescriptor> descriptors = _registry.GetAll();

        // Assert
        foreach (StrategyDescriptor descriptor in descriptors)
        {
            descriptor.Category.ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void AllDescriptors_HaveNonEmptyDescriptions()
    {
        // Act
        IReadOnlyCollection<StrategyDescriptor> descriptors = _registry.GetAll();

        // Assert
        foreach (StrategyDescriptor descriptor in descriptors)
        {
            descriptor.Description.ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void AllParameters_HaveValidDefaults()
    {
        // Act
        IReadOnlyCollection<StrategyDescriptor> descriptors = _registry.GetAll();

        // Assert
        foreach (StrategyDescriptor descriptor in descriptors)
        {
            foreach (ParameterSchema param in descriptor.Parameters.Values)
            {
                param.DefaultValue.ShouldNotBeNull();
                param.Name.ShouldNotBeNullOrWhiteSpace();
                param.ParameterType.ShouldNotBeNull();
            }
        }
    }
}
