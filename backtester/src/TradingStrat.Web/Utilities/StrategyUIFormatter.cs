using TradingStrat.Domain.Strategies;

namespace TradingStrat.Web.Utilities;

/// <summary>
/// Utility class for strategy-related UI formatting.
/// Provides consistent strategy description and metric value formatting across components.
/// </summary>
public static class StrategyUIFormatter
{
    /// <summary>
    /// Generates a human-readable description for a strategy with its parameters.
    /// </summary>
    /// <param name="strategyType">The strategy type enum.</param>
    /// <param name="parameters">Dictionary of strategy parameters.</param>
    /// <returns>Formatted description string (e.g., "MA Crossover (5/20)").</returns>
    public static string GetStrategyDescription(StrategyType strategyType, Dictionary<string, object> parameters)
    {
        return strategyType switch
        {
            StrategyType.MovingAverageCrossover => $"MA Crossover ({parameters.GetValueOrDefault("FastPeriod")}/{parameters.GetValueOrDefault("SlowPeriod")})",
            StrategyType.RSI => $"RSI ({parameters.GetValueOrDefault("Period")}, {parameters.GetValueOrDefault("OversoldLevel")}/{parameters.GetValueOrDefault("OverboughtLevel")})",
            StrategyType.MACD => $"MACD ({parameters.GetValueOrDefault("FastPeriod")}/{parameters.GetValueOrDefault("SlowPeriod")}/{parameters.GetValueOrDefault("SignalPeriod")})",
            StrategyType.MachineLearning => $"ML FastTree ({(decimal)parameters.GetValueOrDefault("BuyThreshold", 0m) * 100:F1}%/{(decimal)parameters.GetValueOrDefault("SellThreshold", 0m) * 100:F1}%)",
            StrategyType.Ichimoku => $"Ichimoku ({parameters.GetValueOrDefault("TenkanPeriod")}/{parameters.GetValueOrDefault("KijunPeriod")})",
            _ => strategyType.ToString()
        };
    }

    /// <summary>
    /// Formats a metric value based on the metric name conventions.
    /// Adds appropriate suffixes and decimal precision.
    /// </summary>
    /// <param name="metric">The metric name (e.g., "Sharpe Ratio", "Total Return %").</param>
    /// <param name="value">The numeric value to format.</param>
    /// <returns>Formatted string with appropriate suffix and precision.</returns>
    public static string FormatMetricValue(string metric, decimal value)
    {
        // Sharpe Ratio and Factor metrics use F2 without suffix
        if (metric.Contains("Sharpe") || metric.Contains("Factor"))
        {
            return value.ToString("F2");
        }

        // Percentage metrics get "%" suffix
        if (metric.Contains("%") ||
            metric.Contains("Return") ||
            metric.Contains("Drawdown") ||
            metric.Contains("Rate"))
        {
            return $"{value:F2}%";
        }

        // Default: F2 precision without suffix
        return value.ToString("F2");
    }

    /// <summary>
    /// Gets a display-friendly name for a strategy type.
    /// </summary>
    /// <param name="strategyType">The strategy type enum.</param>
    /// <returns>Human-readable strategy name.</returns>
    public static string GetStrategyDisplayName(StrategyType strategyType) => strategyType switch
    {
        StrategyType.MovingAverageCrossover => "Moving Average Crossover",
        StrategyType.RSI => "RSI Strategy",
        StrategyType.MACD => "MACD Strategy",
        StrategyType.MachineLearning => "Machine Learning (FastTree)",
        StrategyType.Ichimoku => "Ichimoku Strategy",
        _ => strategyType.ToString()
    };
}
