using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Strategies;

/// <summary>
/// Executes user-defined custom strategies by interpreting StrategyDefinition rules at runtime.
/// Pre-calculates all indicators during initialization for performance.
/// Supports complex rule combinations with AND/OR logic and crossover detection.
/// </summary>
public class CustomRuleBasedStrategy : BaseStrategy
{
    private readonly StrategyDefinition _definition;
    private readonly string _name;
    private readonly string _description;

    // Cache calculated indicators to avoid recalculation on every bar
    // Key format: "IndicatorName(param1=value1,param2=value2)"
    private readonly Dictionary<string, decimal[]> _indicatorCache = new();

    public override string Name => _name;
    public override string Description => _description;

    public CustomRuleBasedStrategy(
        IIndicatorCalculator indicatorCalculator,
        StrategyDefinition definition,
        string name,
        string description)
        : base(indicatorCalculator)
    {
        _definition = definition;
        _name = name;
        _description = description;
    }

    public override void Initialize(IReadOnlyList<HistoricalPrice> historicalData)
    {
        base.Initialize(historicalData);

        // Clear cache on re-initialization to ensure fresh calculations with new data
        _indicatorCache.Clear();

        // Pre-calculate all indicators referenced in entry and exit rules
        PreCalculateIndicators(_definition.EntryRules);
        PreCalculateIndicators(_definition.ExitRules);
    }

    public override TradeSignal GenerateSignal(int currentIndex, decimal currentCash, int currentPosition)
    {
        bool hasPosition = currentPosition > 0;

        // Evaluate entry rules if no position
        if (!hasPosition)
        {
            if (EvaluateRules(_definition.EntryRules, currentIndex))
            {
                int quantity = CalculatePositionSize(currentCash, currentIndex, currentPosition);
                decimal currentPrice = ClosePrices[currentIndex];

                if (quantity > 0)
                {
                    return new TradeSignal(
                        SignalType.Buy,
                        currentPrice,
                        quantity,
                        BuildRuleExplanation(_definition.EntryRules, currentIndex, "Entry"));
                }
            }
        }
        // Evaluate exit rules if has position
        else
        {
            if (EvaluateRules(_definition.ExitRules, currentIndex))
            {
                decimal currentPrice = ClosePrices[currentIndex];
                return new TradeSignal(
                    SignalType.Sell,
                    currentPrice,
                    currentPosition,
                    BuildRuleExplanation(_definition.ExitRules, currentIndex, "Exit"));
            }
        }

        return new TradeSignal(SignalType.Hold, 0, 0, "No rule conditions met");
    }

    public override Dictionary<string, object> GetParameters()
    {
        return new Dictionary<string, object>
        {
            { "EntryRuleCount", _definition.EntryRules.Count },
            { "ExitRuleCount", _definition.ExitRules.Count },
            { "SizingMode", _definition.SizingMode.ToString() }
        };
    }

    /// <summary>
    /// Pre-calculates all indicators referenced in the given rules.
    /// Results are cached for fast lookup during signal generation.
    /// </summary>
    private void PreCalculateIndicators(List<StrategyRule> rules)
    {
        foreach (StrategyRule rule in rules)
        {
            // Calculate primary indicator
            string cacheKey = GetIndicatorCacheKey(rule.IndicatorName, rule.IndicatorParameters);

            if (!_indicatorCache.ContainsKey(cacheKey))
            {
                decimal[] values = CalculateIndicatorValues(
                    rule.IndicatorName,
                    rule.IndicatorParameters);
                _indicatorCache[cacheKey] = values;
            }

            // If rule compares two indicators, pre-calculate the second one
            if (rule.ValueType == RuleValueType.Indicator && rule.SecondIndicatorName is not null)
            {
                string secondKey = GetIndicatorCacheKey(
                    rule.SecondIndicatorName,
                    rule.SecondIndicatorParameters!);

                if (!_indicatorCache.ContainsKey(secondKey))
                {
                    decimal[] values = CalculateIndicatorValues(
                        rule.SecondIndicatorName,
                        rule.SecondIndicatorParameters!);
                    _indicatorCache[secondKey] = values;
                }
            }
        }
    }

    /// <summary>
    /// Evaluates all rules in the list, combining them with AND/OR logic.
    /// Returns true if all conditions are met, false otherwise.
    /// </summary>
    private bool EvaluateRules(List<StrategyRule> rules, int currentIndex)
    {
        if (rules.Count == 0)
        {
            return false;
        }

        // Evaluate first rule
        bool result = EvaluateSingleRule(rules[0], currentIndex);

        // Combine with subsequent rules using their logical operators
        for (int i = 1; i < rules.Count; i++)
        {
            bool currentRuleResult = EvaluateSingleRule(rules[i], currentIndex);
            LogicalOperator op = rules[i - 1].LogicalOperator;

            result = op switch
            {
                LogicalOperator.And => result && currentRuleResult,
                LogicalOperator.Or => result || currentRuleResult,
                _ => currentRuleResult
            };
        }

        return result;
    }

    /// <summary>
    /// Evaluates a single rule at the given index.
    /// Handles all comparison operators including crossover detection.
    /// </summary>
    private bool EvaluateSingleRule(StrategyRule rule, int currentIndex)
    {
        string cacheKey = GetIndicatorCacheKey(rule.IndicatorName, rule.IndicatorParameters);
        decimal[] indicatorValues = _indicatorCache[cacheKey];

        // Insufficient data
        if (currentIndex >= indicatorValues.Length || currentIndex < 1)
        {
            return false;
        }

        decimal leftValue = indicatorValues[currentIndex];

        // Determine right-hand side value based on rule type
        decimal rightValue = rule.ValueType switch
        {
            RuleValueType.Constant => rule.ConstantValue ?? 0,
            RuleValueType.Price => ClosePrices[currentIndex],
            RuleValueType.Indicator => GetIndicatorValue(
                rule.SecondIndicatorName!,
                rule.SecondIndicatorParameters!,
                currentIndex),
            _ => 0
        };

        // Evaluate comparison
        return rule.Operator switch
        {
            ComparisonOperator.GreaterThan => leftValue > rightValue,
            ComparisonOperator.GreaterThanOrEqual => leftValue >= rightValue,
            ComparisonOperator.LessThan => leftValue < rightValue,
            ComparisonOperator.LessThanOrEqual => leftValue <= rightValue,
            ComparisonOperator.Equal => Math.Abs(leftValue - rightValue) < 0.0001m,

            // Crossover detection using CrossoverDetector service
            ComparisonOperator.CrossesAbove =>
                CrossoverDetector.DetectCrossAbove(indicatorValues, currentIndex, rightValue),

            ComparisonOperator.CrossesBelow =>
                CrossoverDetector.DetectCrossBelow(indicatorValues, currentIndex, rightValue),

            _ => false
        };
    }

    /// <summary>
    /// Gets the value of a cached indicator at the specified index.
    /// </summary>
    private decimal GetIndicatorValue(
        string indicatorName,
        Dictionary<string, object> parameters,
        int index)
    {
        string cacheKey = GetIndicatorCacheKey(indicatorName, parameters);
        decimal[] values = _indicatorCache[cacheKey];
        return index < values.Length ? values[index] : 0;
    }

    /// <summary>
    /// Calculates indicator values by delegating to IIndicatorCalculator.
    /// Uses switch expression for reflection-free performance.
    /// </summary>
    private decimal[] CalculateIndicatorValues(
        string indicatorName,
        Dictionary<string, object> parameters)
    {
        return indicatorName.ToUpperInvariant() switch
        {
            "SMA" => CalculateSMA(GetIntParameter(parameters, "Period", 20)),
            "EMA" => CalculateEMA(GetIntParameter(parameters, "Period", 20)),
            "RSI" => CalculateRSI(GetIntParameter(parameters, "Period", 14)),

            "MACD" => CalculateMACD(
                GetIntParameter(parameters, "FastPeriod", 12),
                GetIntParameter(parameters, "SlowPeriod", 26),
                GetIntParameter(parameters, "SignalPeriod", 9)).macd,

            "MACD_SIGNAL" => CalculateMACD(
                GetIntParameter(parameters, "FastPeriod", 12),
                GetIntParameter(parameters, "SlowPeriod", 26),
                GetIntParameter(parameters, "SignalPeriod", 9)).signal,

            "MACD_HISTOGRAM" => CalculateMACD(
                GetIntParameter(parameters, "FastPeriod", 12),
                GetIntParameter(parameters, "SlowPeriod", 26),
                GetIntParameter(parameters, "SignalPeriod", 9)).histogram,

            // Add more indicators as needed - all 26 from IIndicatorCalculator

            _ => throw new ArgumentException($"Unknown indicator: {indicatorName}")
        };
    }

    /// <summary>
    /// Generates a unique cache key for an indicator with its parameters.
    /// Format: "IndicatorName(param1=value1,param2=value2)"
    /// </summary>
    private string GetIndicatorCacheKey(string name, Dictionary<string, object> parameters)
    {
        string paramString = string.Join(",",
            parameters.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key}={kvp.Value}"));
        return $"{name}({paramString})";
    }

    /// <summary>
    /// Calculates the number of shares to buy based on the position sizing mode.
    /// </summary>
    private int CalculatePositionSize(decimal currentCash, int currentIndex, int currentPosition)
    {
        decimal currentPrice = ClosePrices[currentIndex];

        return _definition.SizingMode switch
        {
            PositionSizingMode.FixedPercentage =>
                (int)((currentCash * GetDecimalParameter(_definition.SizingParameters, "Percentage", 0.95m)) / currentPrice),

            PositionSizingMode.FixedQuantity =>
                (int)GetDecimalParameter(_definition.SizingParameters, "Quantity", 10),

            PositionSizingMode.RiskBased =>
                CalculateRiskBasedSize(currentCash, currentIndex),

            _ => CalculateQuantity(currentCash, currentPrice, currentPosition)
        };
    }

    /// <summary>
    /// Calculates position size based on risk percentage and ATR-based stop distance.
    /// </summary>
    private int CalculateRiskBasedSize(decimal currentCash, int currentIndex)
    {
        decimal riskPercent = GetDecimalParameter(_definition.SizingParameters, "RiskPercentage", 0.02m);
        decimal currentPrice = ClosePrices[currentIndex];

        // For risk-based sizing, we would typically use ATR to determine stop distance
        // Simplified implementation here - just use 2% of price as stop distance
        decimal stopDistance = currentPrice * 0.02m;
        decimal accountRisk = currentCash * riskPercent;

        int quantity = (int)(accountRisk / stopDistance);
        return quantity > 0 ? quantity : 1;
    }

    /// <summary>
    /// Builds a human-readable explanation of which rules triggered the signal.
    /// </summary>
    private string BuildRuleExplanation(List<StrategyRule> rules, int currentIndex, string type)
    {
        var explanations = new List<string>();

        foreach (StrategyRule rule in rules)
        {
            string cacheKey = GetIndicatorCacheKey(rule.IndicatorName, rule.IndicatorParameters);
            decimal value = _indicatorCache[cacheKey][currentIndex];

            string comparison = rule.Operator switch
            {
                ComparisonOperator.GreaterThan => ">",
                ComparisonOperator.GreaterThanOrEqual => ">=",
                ComparisonOperator.LessThan => "<",
                ComparisonOperator.LessThanOrEqual => "<=",
                ComparisonOperator.Equal => "=",
                ComparisonOperator.CrossesAbove => "crosses above",
                ComparisonOperator.CrossesBelow => "crosses below",
                _ => "?"
            };

            string rightSide = rule.ValueType switch
            {
                RuleValueType.Constant => rule.ConstantValue?.ToString("F2") ?? "0",
                RuleValueType.Price => $"Price({ClosePrices[currentIndex]:F2})",
                RuleValueType.Indicator => rule.SecondIndicatorName ?? "Unknown",
                _ => "Unknown"
            };

            explanations.Add($"{rule.IndicatorName}({value:F2}) {comparison} {rightSide}");
        }

        string combined = rules.Count > 1
            ? string.Join(" AND ", explanations)  // Simplified - doesn't show OR
            : explanations.FirstOrDefault() ?? "No conditions";

        return $"{type} triggered: {combined}";
    }

    private int GetIntParameter(Dictionary<string, object> parameters, string key, int defaultValue)
    {
        if (!parameters.TryGetValue(key, out object? value))
        {
            return defaultValue;
        }

        return Convert.ToInt32(value);
    }

    private decimal GetDecimalParameter(Dictionary<string, decimal> parameters, string key, decimal defaultValue)
    {
        if (!parameters.TryGetValue(key, out decimal value))
        {
            return defaultValue;
        }

        return value;
    }
}
