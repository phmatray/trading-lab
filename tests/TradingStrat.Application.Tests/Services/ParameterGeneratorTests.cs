using Shouldly;
using TradingStrat.Application.Services;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Tests.Services;

public class ParameterGeneratorTests
{
    [Theory]
    [InlineData("ma")]
    [InlineData("rsi")]
    [InlineData("macd")]
    [InlineData("ml")]
    public void GetPredefinedVariants_WithValidStrategy_ShouldReturnVariants(string strategyType)
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
        List<(StrategyVariant VariantA, StrategyVariant VariantB)> variants = ParameterGenerator.GetPredefinedVariants("ma");
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
        List<(StrategyVariant VariantA, StrategyVariant VariantB)> variants = ParameterGenerator.GetPredefinedVariants("rsi");
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
        List<(StrategyVariant VariantA, StrategyVariant VariantB)> variants = ParameterGenerator.GetPredefinedVariants("macd");
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
        List<(StrategyVariant VariantA, StrategyVariant VariantB)> variants = ParameterGenerator.GetPredefinedVariants("ml");
        (StrategyVariant variantA, StrategyVariant variantB) = variants.First();

        // Assert
        variantA.Parameters.ShouldContainKey("BuyThreshold");
        variantA.Parameters.ShouldContainKey("SellThreshold");
        variantB.Parameters.ShouldContainKey("BuyThreshold");
        variantB.Parameters.ShouldContainKey("SellThreshold");
    }

    [Fact]
    public void GetPredefinedVariants_WithInvalidStrategy_ShouldThrow()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            ParameterGenerator.GetPredefinedVariants("invalid"));
    }

    [Fact]
    public void GetPredefinedVariants_ShouldSupportAliases()
    {
        // Arrange & Act
        List<(StrategyVariant VariantA, StrategyVariant VariantB)> variants1 = ParameterGenerator.GetPredefinedVariants("ma");
        List<(StrategyVariant VariantA, StrategyVariant VariantB)> variants2 = ParameterGenerator.GetPredefinedVariants("movingaverage");
        List<(StrategyVariant VariantA, StrategyVariant VariantB)> variants3 = ParameterGenerator.GetPredefinedVariants("macrossover");

        // Assert - All aliases should return the same number of variants
        variants1.Count.ShouldBe(variants2.Count);
        variants1.Count.ShouldBe(variants3.Count);
    }
}
