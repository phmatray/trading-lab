using TradingStrat.Domain.Strategies;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Services;

/// <summary>
/// Service for generating predefined parameter combinations for A/B testing.
/// Provides common variants for each strategy type based on best practices.
/// </summary>
public static class ParameterGenerator
{
    /// <summary>
    /// Gets predefined variant pairs for a strategy type.
    /// Returns common A/B test configurations based on trading best practices.
    /// </summary>
    public static List<(StrategyVariant VariantA, StrategyVariant VariantB)> GetPredefinedVariants(
        StrategyType strategyType)
    {
        return strategyType switch
        {
            StrategyType.MovingAverageCrossover => GetMovingAverageVariants(),
            StrategyType.RSI => GetRSIVariants(),
            StrategyType.MACD => GetMACDVariants(),
            StrategyType.MachineLearning => GetMLVariants(),
            StrategyType.Ichimoku => GetIchimokuVariants(),
            _ => throw new ArgumentException($"Unknown strategy type: {strategyType}")
        };
    }

    private static List<(StrategyVariant, StrategyVariant)> GetMovingAverageVariants()
    {
        return new List<(StrategyVariant, StrategyVariant)>
        {
            // Conservative vs Aggressive
            (
                new StrategyVariant(
                    "Variant A",
                    StrategyType.MovingAverageCrossover,
                    new Dictionary<string, object> { ["FastPeriod"] = 20, ["SlowPeriod"] = 50 },
                    "Conservative (20/50)"),
                new StrategyVariant(
                    "Variant B",
                    StrategyType.MovingAverageCrossover,
                    new Dictionary<string, object> { ["FastPeriod"] = 10, ["SlowPeriod"] = 30 },
                    "Aggressive (10/30)")
            ),
            // Medium vs Very Conservative
            (
                new StrategyVariant(
                    "Variant A",
                    StrategyType.MovingAverageCrossover,
                    new Dictionary<string, object> { ["FastPeriod"] = 15, ["SlowPeriod"] = 40 },
                    "Medium (15/40)"),
                new StrategyVariant(
                    "Variant B",
                    StrategyType.MovingAverageCrossover,
                    new Dictionary<string, object> { ["FastPeriod"] = 30, ["SlowPeriod"] = 100 },
                    "Very Conservative (30/100)")
            )
        };
    }

    private static List<(StrategyVariant, StrategyVariant)> GetRSIVariants()
    {
        return new List<(StrategyVariant, StrategyVariant)>
        {
            // Standard vs Aggressive
            (
                new StrategyVariant(
                    "Variant A",
                    StrategyType.RSI,
                    new Dictionary<string, object>
                    {
                        ["Period"] = 14,
                        ["OversoldThreshold"] = 30m,
                        ["OverboughtThreshold"] = 70m
                    },
                    "Standard (14, 30/70)"),
                new StrategyVariant(
                    "Variant B",
                    StrategyType.RSI,
                    new Dictionary<string, object>
                    {
                        ["Period"] = 14,
                        ["OversoldThreshold"] = 25m,
                        ["OverboughtThreshold"] = 75m
                    },
                    "Aggressive (14, 25/75)")
            ),
            // Short Period vs Long Period
            (
                new StrategyVariant(
                    "Variant A",
                    StrategyType.RSI,
                    new Dictionary<string, object>
                    {
                        ["Period"] = 7,
                        ["OversoldThreshold"] = 30m,
                        ["OverboughtThreshold"] = 70m
                    },
                    "Fast (7, 30/70)"),
                new StrategyVariant(
                    "Variant B",
                    StrategyType.RSI,
                    new Dictionary<string, object>
                    {
                        ["Period"] = 21,
                        ["OversoldThreshold"] = 30m,
                        ["OverboughtThreshold"] = 70m
                    },
                    "Slow (21, 30/70)")
            )
        };
    }

    private static List<(StrategyVariant, StrategyVariant)> GetMACDVariants()
    {
        return new List<(StrategyVariant, StrategyVariant)>
        {
            // Standard vs Fast
            (
                new StrategyVariant(
                    "Variant A",
                    StrategyType.MACD,
                    new Dictionary<string, object>
                    {
                        ["FastPeriod"] = 12,
                        ["SlowPeriod"] = 26,
                        ["SignalPeriod"] = 9
                    },
                    "Standard (12/26/9)"),
                new StrategyVariant(
                    "Variant B",
                    StrategyType.MACD,
                    new Dictionary<string, object>
                    {
                        ["FastPeriod"] = 8,
                        ["SlowPeriod"] = 17,
                        ["SignalPeriod"] = 9
                    },
                    "Fast (8/17/9)")
            )
        };
    }

    private static List<(StrategyVariant, StrategyVariant)> GetMLVariants()
    {
        return new List<(StrategyVariant, StrategyVariant)>
        {
            // Conservative vs Aggressive thresholds
            (
                new StrategyVariant(
                    "Variant A",
                    StrategyType.MachineLearning,
                    new Dictionary<string, object>
                    {
                        ["BuyThreshold"] = 0.01m,
                        ["SellThreshold"] = -0.01m
                    },
                    "Conservative (±1%)"),
                new StrategyVariant(
                    "Variant B",
                    StrategyType.MachineLearning,
                    new Dictionary<string, object>
                    {
                        ["BuyThreshold"] = 0.005m,
                        ["SellThreshold"] = -0.005m
                    },
                    "Aggressive (±0.5%)")
            )
        };
    }

    private static List<(StrategyVariant, StrategyVariant)> GetIchimokuVariants()
    {
        return new List<(StrategyVariant, StrategyVariant)>
        {
            // Standard vs Custom
            (
                new StrategyVariant(
                    "Variant A",
                    StrategyType.Ichimoku,
                    new Dictionary<string, object>
                    {
                        ["ConversionLinePeriod"] = 9,
                        ["BaseLinePeriod"] = 26,
                        ["LeadingSpanBPeriod"] = 52,
                        ["Displacement"] = 26,
                        ["ExitMode"] = "CloseBelowBaseLine",
                        ["EntryMode"] = "AllConditionsOnly",
                        ["CrossLookbackDays"] = 5,
                        ["RiskPercentage"] = 0.02m
                    },
                    "Standard (9/26/52)"),
                new StrategyVariant(
                    "Variant B",
                    StrategyType.Ichimoku,
                    new Dictionary<string, object>
                    {
                        ["ConversionLinePeriod"] = 7,
                        ["BaseLinePeriod"] = 22,
                        ["LeadingSpanBPeriod"] = 44,
                        ["Displacement"] = 22,
                        ["ExitMode"] = "CloseBelowBaseLine",
                        ["EntryMode"] = "AllConditionsOnly",
                        ["CrossLookbackDays"] = 5,
                        ["RiskPercentage"] = 0.02m
                    },
                    "Fast (7/22/44)")
            )
        };
    }
}
