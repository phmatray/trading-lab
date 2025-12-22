using System.ComponentModel.DataAnnotations;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Web.Models;

/// <summary>
/// Form model for creating/editing custom strategies.
/// </summary>
public class StrategyFormModel
{
    [Required(ErrorMessage = "Strategy name is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Author name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Author must be between 2 and 100 characters")]
    public string Author { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category is required")]
    [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters")]
    public string Category { get; set; } = "Custom";

    [Required(ErrorMessage = "Position sizing mode is required")]
    public PositionSizingMode SizingMode { get; set; } = PositionSizingMode.FixedPercentage;

    [Range(0.01, 100, ErrorMessage = "Fixed percentage must be between 0.01 and 100")]
    public decimal FixedPercentage { get; set; } = 10.0m;

    [Range(1, 1000000, ErrorMessage = "Fixed quantity must be between 1 and 1,000,000")]
    public int FixedQuantity { get; set; } = 100;

    [Range(0.001, 0.1, ErrorMessage = "Risk percentage must be between 0.1% and 10%")]
    public decimal RiskPercentage { get; set; } = 0.02m;

    public List<RuleFormModel> EntryRules { get; set; } = [];
    public List<RuleFormModel> ExitRules { get; set; } = [];

    /// <summary>
    /// Converts sizing parameters to dictionary based on selected mode.
    /// </summary>
    public Dictionary<string, decimal> GetSizingParameters()
    {
        return SizingMode switch
        {
            PositionSizingMode.FixedPercentage => new Dictionary<string, decimal>
            {
                ["Percentage"] = FixedPercentage
            },
            PositionSizingMode.FixedQuantity => new Dictionary<string, decimal>
            {
                ["Quantity"] = FixedQuantity
            },
            PositionSizingMode.RiskBased => new Dictionary<string, decimal>
            {
                ["RiskPercentage"] = RiskPercentage
            },
            _ => throw new InvalidOperationException($"Unknown sizing mode: {SizingMode}")
        };
    }

    /// <summary>
    /// Creates a StrategyDefinition from the form model.
    /// </summary>
    public StrategyDefinition ToStrategyDefinition()
    {
        return new StrategyDefinition(
            EntryRules.Select(r => r.ToStrategyRule()).ToList(),
            ExitRules.Select(r => r.ToStrategyRule()).ToList(),
            SizingMode,
            GetSizingParameters()
        );
    }
}

/// <summary>
/// Form model for individual trading rules.
/// </summary>
public class RuleFormModel
{
    [Required(ErrorMessage = "Indicator is required")]
    public string IndicatorName { get; set; } = string.Empty;

    public Dictionary<string, object> IndicatorParameters { get; set; } = new();

    [Required(ErrorMessage = "Comparison operator is required")]
    public ComparisonOperator Operator { get; set; } = ComparisonOperator.GreaterThan;

    [Required(ErrorMessage = "Value type is required")]
    public RuleValueType ValueType { get; set; } = RuleValueType.Constant;

    public decimal? ConstantValue { get; set; }

    public string? SecondIndicatorName { get; set; }

    public Dictionary<string, object>? SecondIndicatorParameters { get; set; }

    public LogicalOperator LogicalOperator { get; set; } = LogicalOperator.None;

    /// <summary>
    /// Converts the form model to a domain StrategyRule.
    /// </summary>
    public StrategyRule ToStrategyRule()
    {
        return new StrategyRule(
            IndicatorName,
            IndicatorParameters,
            Operator,
            ValueType,
            ConstantValue,
            SecondIndicatorName,
            SecondIndicatorParameters,
            LogicalOperator
        );
    }

    /// <summary>
    /// Creates a form model from a domain StrategyRule.
    /// </summary>
    public static RuleFormModel FromStrategyRule(StrategyRule rule)
    {
        return new RuleFormModel
        {
            IndicatorName = rule.IndicatorName,
            IndicatorParameters = new Dictionary<string, object>(rule.IndicatorParameters),
            Operator = rule.Operator,
            ValueType = rule.ValueType,
            ConstantValue = rule.ConstantValue,
            SecondIndicatorName = rule.SecondIndicatorName,
            SecondIndicatorParameters = rule.SecondIndicatorParameters != null
                ? new Dictionary<string, object>(rule.SecondIndicatorParameters)
                : null,
            LogicalOperator = rule.LogicalOperator
        };
    }
}

/// <summary>
/// Metadata about available indicators for UI dropdowns.
/// </summary>
public class IndicatorMetadata
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<IndicatorParameter> Parameters { get; set; } = [];
}

/// <summary>
/// Parameter definition for an indicator.
/// </summary>
public class IndicatorParameter
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Type { get; set; } = "int"; // int, decimal, string
    public object DefaultValue { get; set; } = 14;
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
}
