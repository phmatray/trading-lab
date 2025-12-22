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
    /// <param name="strategyType">The strategy type code (ma, rsi, macd, ml, ichimoku).</param>
    /// <param name="parameters">Dictionary of strategy parameters.</param>
    /// <returns>Formatted description string (e.g., "MA Crossover (5/20)").</returns>
    public static string GetStrategyDescription(string strategyType, Dictionary<string, object> parameters)
    {
        return strategyType switch
        {
            "ma" => $"MA Crossover ({parameters.GetValueOrDefault("FastPeriod")}/{parameters.GetValueOrDefault("SlowPeriod")})",
            "rsi" => $"RSI ({parameters.GetValueOrDefault("Period")}, {parameters.GetValueOrDefault("OversoldLevel")}/{parameters.GetValueOrDefault("OverboughtLevel")})",
            "macd" => $"MACD ({parameters.GetValueOrDefault("FastPeriod")}/{parameters.GetValueOrDefault("SlowPeriod")}/{parameters.GetValueOrDefault("SignalPeriod")})",
            "ml" => $"ML FastTree ({(decimal)parameters.GetValueOrDefault("BuyThreshold", 0m) * 100:F1}%/{(decimal)parameters.GetValueOrDefault("SellThreshold", 0m) * 100:F1}%)",
            "ichimoku" => $"Ichimoku ({parameters.GetValueOrDefault("TenkanPeriod")}/{parameters.GetValueOrDefault("KijunPeriod")})",
            _ => strategyType
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
    /// Gets a display-friendly name for a strategy type code.
    /// </summary>
    /// <param name="strategyType">The strategy type code (ma, rsi, macd, ml, ichimoku).</param>
    /// <returns>Human-readable strategy name.</returns>
    public static string GetStrategyDisplayName(string strategyType) => strategyType switch
    {
        "ma" => "Moving Average Crossover",
        "rsi" => "RSI Strategy",
        "macd" => "MACD Strategy",
        "ml" => "Machine Learning (FastTree)",
        "ichimoku" => "Ichimoku Strategy",
        _ => strategyType.ToUpperInvariant()
    };
}
