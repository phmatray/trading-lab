using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Services;

/// <summary>
/// Domain service for validating strategy definitions.
/// Pure business logic with zero infrastructure dependencies.
/// Validates entry/exit rules and position sizing configuration.
/// </summary>
public class StrategyDefinitionValidator
{
    /// <summary>
    /// Validates a complete strategy definition.
    /// Returns ValidationResult with IsValid flag and list of error messages.
    /// </summary>
    /// <param name="definition">The strategy definition to validate.</param>
    /// <returns>ValidationResult containing validation status and any errors.</returns>
    public ValidationResult Validate(StrategyDefinition definition)
    {
        var errors = new List<string>();

        // Validate entry rules
        if (definition.EntryRules.Count == 0)
        {
            errors.Add("Strategy must have at least one entry rule");
        }

        // Validate exit rules
        if (definition.ExitRules.Count == 0)
        {
            errors.Add("Strategy must have at least one exit rule");
        }

        // Validate each entry rule
        foreach (StrategyRule rule in definition.EntryRules)
        {
            ValidateRule(rule, errors);
        }

        // Validate each exit rule
        foreach (StrategyRule rule in definition.ExitRules)
        {
            ValidateRule(rule, errors);
        }

        // Validate position sizing
        ValidatePositionSizing(definition, errors);

        return new ValidationResult(errors.Count == 0, errors);
    }

    /// <summary>
    /// Validates an individual strategy rule.
    /// Checks indicator name, comparison values, and parameters.
    /// </summary>
    private void ValidateRule(StrategyRule rule, List<string> errors)
    {
        // Validate indicator name
        if (string.IsNullOrWhiteSpace(rule.IndicatorName))
        {
            errors.Add("Rule must specify an indicator name");
        }

        // Validate constant comparisons
        if (rule.ValueType == RuleValueType.Constant && rule.ConstantValue is null)
        {
            errors.Add($"Rule with constant comparison must provide ConstantValue (Indicator: {rule.IndicatorName})");
        }

        // Validate indicator comparisons
        if (rule.ValueType == RuleValueType.Indicator)
        {
            if (string.IsNullOrWhiteSpace(rule.SecondIndicatorName))
            {
                errors.Add($"Rule comparing two indicators must provide SecondIndicatorName (Indicator: {rule.IndicatorName})");
            }

            if (rule.SecondIndicatorParameters is null || rule.SecondIndicatorParameters.Count == 0)
            {
                errors.Add($"Rule comparing two indicators must provide SecondIndicatorParameters (Indicator: {rule.IndicatorName})");
            }
        }

        // Validate indicator parameters
        if (rule.IndicatorParameters.Count == 0)
        {
            errors.Add($"Rule must provide IndicatorParameters (Indicator: {rule.IndicatorName})");
        }
    }

    /// <summary>
    /// Validates position sizing mode and required parameters.
    /// Checks for required parameters and valid ranges.
    /// </summary>
    private void ValidatePositionSizing(StrategyDefinition definition, List<string> errors)
    {
        switch (definition.SizingMode)
        {
            case PositionSizingMode.FixedPercentage:
                if (!definition.SizingParameters.ContainsKey("Percentage"))
                {
                    errors.Add("FixedPercentage sizing mode requires 'Percentage' parameter");
                }
                else
                {
                    decimal percentage = definition.SizingParameters["Percentage"];
                    if (percentage <= 0 || percentage > 1)
                    {
                        errors.Add("Percentage must be between 0 and 1 (e.g., 0.95 for 95%)");
                    }
                }
                break;

            case PositionSizingMode.FixedQuantity:
                if (!definition.SizingParameters.ContainsKey("Quantity"))
                {
                    errors.Add("FixedQuantity sizing mode requires 'Quantity' parameter");
                }
                else
                {
                    decimal quantity = definition.SizingParameters["Quantity"];
                    if (quantity <= 0)
                    {
                        errors.Add("Quantity must be greater than 0");
                    }
                }
                break;

            case PositionSizingMode.RiskBased:
                if (!definition.SizingParameters.ContainsKey("RiskPercentage"))
                {
                    errors.Add("RiskBased sizing mode requires 'RiskPercentage' parameter");
                }
                else
                {
                    decimal riskPercentage = definition.SizingParameters["RiskPercentage"];
                    if (riskPercentage <= 0 || riskPercentage > 0.1m)
                    {
                        errors.Add("RiskPercentage must be between 0 and 0.1 (e.g., 0.02 for 2%)");
                    }
                }
                break;
        }
    }
}

/// <summary>
/// Result of strategy definition validation.
/// Contains validation status and list of error messages.
/// </summary>
public record ValidationResult(
    bool IsValid,
    List<string> Errors
);
