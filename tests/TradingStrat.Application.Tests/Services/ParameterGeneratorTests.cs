using Shouldly;
using TradingStrat.Application.Services;
using TradingStrat.Domain.Strategies;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Tests.Services;

public class ParameterGeneratorTests
{
    [Theory]
    [InlineData(StrategyType.MovingAverageCrossover)]
    [InlineData(StrategyType.RSI)]
    [InlineData(StrategyType.MACD)]
    [InlineData(StrategyType.MachineLearning)]
    public void GetPredefinedVariants_WithValidStrategy_ShouldReturnVariants(StrategyType strategyType)
    {
        // Act
        List<(StrategyVariant VariantA, StrategyVariant VariantB)> variants = ParameterGenerator.GetPredefinedVariants(strategyType);

        // Assert
        variants.ShouldNotBeEmpty();
        foreach ((StrategyVariant variantA, StrategyVariant variantB) in variants)
        {
            variantA.ShouldNotBeNull();
            variantB.ShouldNotBeNull();
            variantA.StrategyType.ShouldBe(strategyType);
            variantB.StrategyType.ShouldBe(strategyType);
            variantA.Label.ShouldBe("Variant A");
            variantB.Label.ShouldBe("Variant B");
        }
    }

    [Fact]
    public void GetPredefinedVariants_ForMA_ShouldHaveDifferentPeriods()
    {
        // Act
        List<(StrategyVariant VariantA, StrategyVariant VariantB)> variants = ParameterGenerator.GetPredefinedVariants(StrategyType.MovingAverageCrossover);
        (StrategyVariant variantA, StrategyVariant variantB) = variants.First();

        // Assert
        variantA.Parameters.ShouldContainKey("FastPeriod");
        variantA.Parameters.ShouldContainKey("SlowPeriod");
        variantB.Parameters.ShouldContainKey("FastPeriod");
        variantB.Parameters.ShouldContainKey("SlowPeriod");

        variantA.Parameters["FastPeriod"].ShouldNotBe(variantB.Parameters["FastPeriod"]);
    }

    [Fact]
    public void GetPredefinedVariants_ForRSI_ShouldHaveDifferentThresholds()
    {
        // Act
        List<(StrategyVariant VariantA, StrategyVariant VariantB)> variants = ParameterGenerator.GetPredefinedVariants(StrategyType.RSI);
        (StrategyVariant variantA, StrategyVariant variantB) = variants.First();

        // Assert
        variantA.Parameters.ShouldContainKey("Period");
        variantA.Parameters.ShouldContainKey("OversoldThreshold");
        variantA.Parameters.ShouldContainKey("OverboughtThreshold");
        variantB.Parameters.ShouldContainKey("Period");
        variantB.Parameters.ShouldContainKey("OversoldThreshold");
        variantB.Parameters.ShouldContainKey("OverboughtThreshold");
    }

    [Fact]
    public void GetPredefinedVariants_ForMACD_ShouldHaveDifferentPeriods()
    {
        // Act
        List<(StrategyVariant VariantA, StrategyVariant VariantB)> variants = ParameterGenerator.GetPredefinedVariants(StrategyType.MACD);
        (StrategyVariant variantA, StrategyVariant variantB) = variants.First();

        // Assert
        variantA.Parameters.ShouldContainKey("FastPeriod");
        variantA.Parameters.ShouldContainKey("SlowPeriod");
        variantA.Parameters.ShouldContainKey("SignalPeriod");
        variantB.Parameters.ShouldContainKey("FastPeriod");
        variantB.Parameters.ShouldContainKey("SlowPeriod");
        variantB.Parameters.ShouldContainKey("SignalPeriod");
    }

    [Fact]
    public void GetPredefinedVariants_ForML_ShouldHaveDifferentThresholds()
    {
        // Act
        List<(StrategyVariant VariantA, StrategyVariant VariantB)> variants = ParameterGenerator.GetPredefinedVariants(StrategyType.MachineLearning);
        (StrategyVariant variantA, StrategyVariant variantB) = variants.First();

        // Assert
        variantA.Parameters.ShouldContainKey("BuyThreshold");
        variantA.Parameters.ShouldContainKey("SellThreshold");
        variantB.Parameters.ShouldContainKey("BuyThreshold");
        variantB.Parameters.ShouldContainKey("SellThreshold");
    }

    // Note: Tests for invalid strategy types and string aliases removed
    // because enum-based API provides compile-time safety
}
