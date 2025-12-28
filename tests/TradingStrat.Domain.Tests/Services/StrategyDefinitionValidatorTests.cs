using Shouldly;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Tests.Services;

/// <summary>
/// Tests for StrategyDefinitionValidator domain service.
/// Verifies strategy rule validation and position sizing validation logic.
/// </summary>
public class StrategyDefinitionValidatorTests
{
    private readonly StrategyDefinitionValidator _validator;

    public StrategyDefinitionValidatorTests()
    {
        _validator = new StrategyDefinitionValidator();
    }

    #region Valid Strategy Tests

    [Fact]
    public void Validate_WithValidStrategy_ReturnsValid()
    {
        // Arrange
        var definition = new StrategyDefinition(
            entryRules: new List<StrategyRule>
            {
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.LessThan,
                    RuleValueType.Constant, 30m, null, null, LogicalOperator.None)
            },
            exitRules: new List<StrategyRule>
            {
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.GreaterThan,
                    RuleValueType.Constant, 70m, null, null, LogicalOperator.None)
            },
            sizingMode: PositionSizingMode.FixedPercentage,
            sizingParameters: new() { ["Percentage"] = 0.95m }
        );

        // Act
        ValidationResult result = _validator.Validate(definition);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    #endregion

    #region Entry/Exit Rules Validation Tests

    [Fact]
    public void Validate_WithNoEntryRules_ReturnsError()
    {
        // Arrange
        var definition = new StrategyDefinition(
            entryRules: new List<StrategyRule>(),
            exitRules: new List<StrategyRule>
            {
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.GreaterThan,
                    RuleValueType.Constant, 70m, null, null, LogicalOperator.None)
            },
            sizingMode: PositionSizingMode.FixedPercentage,
            sizingParameters: new() { ["Percentage"] = 0.95m }
        );

        // Act
        ValidationResult result = _validator.Validate(definition);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("Strategy must have at least one entry rule");
    }

    [Fact]
    public void Validate_WithNoExitRules_ReturnsError()
    {
        // Arrange
        var definition = new StrategyDefinition(
            entryRules: new List<StrategyRule>
            {
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.LessThan,
                    RuleValueType.Constant, 30m, null, null, LogicalOperator.None)
            },
            exitRules: new List<StrategyRule>(),
            sizingMode: PositionSizingMode.FixedPercentage,
            sizingParameters: new() { ["Percentage"] = 0.95m }
        );

        // Act
        ValidationResult result = _validator.Validate(definition);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("Strategy must have at least one exit rule");
    }

    #endregion

    #region Rule Validation Tests

    [Fact]
    public void Validate_WithMissingIndicatorName_ReturnsError()
    {
        // Arrange
        var definition = new StrategyDefinition(
            entryRules: new List<StrategyRule>
            {
                new("", new() { ["Period"] = 14 }, ComparisonOperator.LessThan,
                    RuleValueType.Constant, 30m, null, null, LogicalOperator.None)
            },
            exitRules: new List<StrategyRule>
            {
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.GreaterThan,
                    RuleValueType.Constant, 70m, null, null, LogicalOperator.None)
            },
            sizingMode: PositionSizingMode.FixedPercentage,
            sizingParameters: new() { ["Percentage"] = 0.95m }
        );

        // Act
        ValidationResult result = _validator.Validate(definition);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("Rule must specify an indicator name");
    }

    [Fact]
    public void Validate_WithConstantValueType_ButNoConstantValue_ReturnsError()
    {
        // Arrange
        var definition = new StrategyDefinition(
            entryRules: new List<StrategyRule>
            {
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.LessThan,
                    RuleValueType.Constant, null, null, null, LogicalOperator.None)
            },
            exitRules: new List<StrategyRule>
            {
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.GreaterThan,
                    RuleValueType.Constant, 70m, null, null, LogicalOperator.None)
            },
            sizingMode: PositionSizingMode.FixedPercentage,
            sizingParameters: new() { ["Percentage"] = 0.95m }
        );

        // Act
        ValidationResult result = _validator.Validate(definition);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("must provide ConstantValue"));
    }

    [Fact]
    public void Validate_WithIndicatorValueType_ButNoSecondIndicatorName_ReturnsError()
    {
        // Arrange
        var definition = new StrategyDefinition(
            entryRules: new List<StrategyRule>
            {
                new("SMA", new() { ["Period"] = 20 }, ComparisonOperator.CrossesAbove,
                    RuleValueType.Indicator, null, "", new() { ["Period"] = 50 }, LogicalOperator.None)
            },
            exitRules: new List<StrategyRule>
            {
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.GreaterThan,
                    RuleValueType.Constant, 70m, null, null, LogicalOperator.None)
            },
            sizingMode: PositionSizingMode.FixedPercentage,
            sizingParameters: new() { ["Percentage"] = 0.95m }
        );

        // Act
        ValidationResult result = _validator.Validate(definition);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("must provide SecondIndicatorName"));
    }

    [Fact]
    public void Validate_WithIndicatorValueType_ButNoSecondIndicatorParameters_ReturnsError()
    {
        // Arrange
        var definition = new StrategyDefinition(
            entryRules: new List<StrategyRule>
            {
                new("SMA", new() { ["Period"] = 20 }, ComparisonOperator.CrossesAbove,
                    RuleValueType.Indicator, null, "SMA", new(), LogicalOperator.None)
            },
            exitRules: new List<StrategyRule>
            {
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.GreaterThan,
                    RuleValueType.Constant, 70m, null, null, LogicalOperator.None)
            },
            sizingMode: PositionSizingMode.FixedPercentage,
            sizingParameters: new() { ["Percentage"] = 0.95m }
        );

        // Act
        ValidationResult result = _validator.Validate(definition);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("must provide SecondIndicatorParameters"));
    }

    [Fact]
    public void Validate_WithMissingIndicatorParameters_ReturnsError()
    {
        // Arrange
        var definition = new StrategyDefinition(
            entryRules: new List<StrategyRule>
            {
                new("RSI", new(), ComparisonOperator.LessThan,
                    RuleValueType.Constant, 30m, null, null, LogicalOperator.None)
            },
            exitRules: new List<StrategyRule>
            {
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.GreaterThan,
                    RuleValueType.Constant, 70m, null, null, LogicalOperator.None)
            },
            sizingMode: PositionSizingMode.FixedPercentage,
            sizingParameters: new() { ["Percentage"] = 0.95m }
        );

        // Act
        ValidationResult result = _validator.Validate(definition);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("must provide IndicatorParameters"));
    }

    #endregion

    #region Position Sizing Validation Tests

    [Fact]
    public void Validate_WithFixedPercentage_MissingPercentageParameter_ReturnsError()
    {
        // Arrange
        var definition = new StrategyDefinition(
            entryRules: new List<StrategyRule>
            {
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.LessThan,
                    RuleValueType.Constant, 30m, null, null, LogicalOperator.None)
            },
            exitRules: new List<StrategyRule>
            {
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.GreaterThan,
                    RuleValueType.Constant, 70m, null, null, LogicalOperator.None)
            },
            sizingMode: PositionSizingMode.FixedPercentage,
            sizingParameters: new()
        );

        // Act
        ValidationResult result = _validator.Validate(definition);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("FixedPercentage sizing mode requires 'Percentage' parameter");
    }

    [Fact]
    public void Validate_WithFixedPercentage_InvalidPercentageZero_ReturnsError()
    {
        // Arrange
        var definition = new StrategyDefinition(
            entryRules: new List<StrategyRule>
            {
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.LessThan,
                    RuleValueType.Constant, 30m, null, null, LogicalOperator.None)
            },
            exitRules: new List<StrategyRule>
            {
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.GreaterThan,
                    RuleValueType.Constant, 70m, null, null, LogicalOperator.None)
            },
            sizingMode: PositionSizingMode.FixedPercentage,
            sizingParameters: new() { ["Percentage"] = 0m }
        );

        // Act
        ValidationResult result = _validator.Validate(definition);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("Percentage must be between 0 and 1 (e.g., 0.95 for 95%)");
    }

    [Fact]
    public void Validate_WithFixedPercentage_InvalidPercentageGreaterThanOne_ReturnsError()
    {
        // Arrange
        var definition = new StrategyDefinition(
            entryRules: new List<StrategyRule>
            {
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.LessThan,
                    RuleValueType.Constant, 30m, null, null, LogicalOperator.None)
            },
            exitRules: new List<StrategyRule>
            {
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.GreaterThan,
                    RuleValueType.Constant, 70m, null, null, LogicalOperator.None)
            },
            sizingMode: PositionSizingMode.FixedPercentage,
            sizingParameters: new() { ["Percentage"] = 1.5m }
        );

        // Act
        ValidationResult result = _validator.Validate(definition);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("Percentage must be between 0 and 1 (e.g., 0.95 for 95%)");
    }

    [Fact]
    public void Validate_WithFixedQuantity_MissingQuantityParameter_ReturnsError()
    {
        // Arrange
        var definition = new StrategyDefinition(
            entryRules: new List<StrategyRule>
            {
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.LessThan,
                    RuleValueType.Constant, 30m, null, null, LogicalOperator.None)
            },
            exitRules: new List<StrategyRule>
            {
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.GreaterThan,
                    RuleValueType.Constant, 70m, null, null, LogicalOperator.None)
            },
            sizingMode: PositionSizingMode.FixedQuantity,
            sizingParameters: new()
        );

        // Act
        ValidationResult result = _validator.Validate(definition);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("FixedQuantity sizing mode requires 'Quantity' parameter");
    }

    [Fact]
    public void Validate_WithFixedQuantity_InvalidQuantityZero_ReturnsError()
    {
        // Arrange
        var definition = new StrategyDefinition(
            entryRules: new List<StrategyRule>
            {
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.LessThan,
                    RuleValueType.Constant, 30m, null, null, LogicalOperator.None)
            },
            exitRules: new List<StrategyRule>
            {
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.GreaterThan,
                    RuleValueType.Constant, 70m, null, null, LogicalOperator.None)
            },
            sizingMode: PositionSizingMode.FixedQuantity,
            sizingParameters: new() { ["Quantity"] = 0m }
        );

        // Act
        ValidationResult result = _validator.Validate(definition);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("Quantity must be greater than 0");
    }

    [Fact]
    public void Validate_WithRiskBased_MissingRiskPercentageParameter_ReturnsError()
    {
        // Arrange
        var definition = new StrategyDefinition(
            entryRules: new List<StrategyRule>
            {
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.LessThan,
                    RuleValueType.Constant, 30m, null, null, LogicalOperator.None)
            },
            exitRules: new List<StrategyRule>
            {
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.GreaterThan,
                    RuleValueType.Constant, 70m, null, null, LogicalOperator.None)
            },
            sizingMode: PositionSizingMode.RiskBased,
            sizingParameters: new()
        );

        // Act
        ValidationResult result = _validator.Validate(definition);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("RiskBased sizing mode requires 'RiskPercentage' parameter");
    }

    [Fact]
    public void Validate_WithRiskBased_InvalidRiskPercentageZero_ReturnsError()
    {
        // Arrange
        var definition = new StrategyDefinition(
            entryRules: new List<StrategyRule>
            {
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.LessThan,
                    RuleValueType.Constant, 30m, null, null, LogicalOperator.None)
            },
            exitRules: new List<StrategyRule>
            {
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.GreaterThan,
                    RuleValueType.Constant, 70m, null, null, LogicalOperator.None)
            },
            sizingMode: PositionSizingMode.RiskBased,
            sizingParameters: new() { ["RiskPercentage"] = 0m }
        );

        // Act
        ValidationResult result = _validator.Validate(definition);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("RiskPercentage must be between 0 and 0.1 (e.g., 0.02 for 2%)");
    }

    [Fact]
    public void Validate_WithRiskBased_InvalidRiskPercentageTooHigh_ReturnsError()
    {
        // Arrange
        var definition = new StrategyDefinition(
            entryRules: new List<StrategyRule>
            {
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.LessThan,
                    RuleValueType.Constant, 30m, null, null, LogicalOperator.None)
            },
            exitRules: new List<StrategyRule>
            {
                new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.GreaterThan,
                    RuleValueType.Constant, 70m, null, null, LogicalOperator.None)
            },
            sizingMode: PositionSizingMode.RiskBased,
            sizingParameters: new() { ["RiskPercentage"] = 0.2m }
        );

        // Act
        ValidationResult result = _validator.Validate(definition);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("RiskPercentage must be between 0 and 0.1 (e.g., 0.02 for 2%)");
    }

    #endregion

    #region Multiple Errors Test

    [Fact]
    public void Validate_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var definition = new StrategyDefinition(
            entryRules: new List<StrategyRule>(),
            exitRules: new List<StrategyRule>(),
            sizingMode: PositionSizingMode.FixedPercentage,
            sizingParameters: new() { ["Percentage"] = 2m }
        );

        // Act
        ValidationResult result = _validator.Validate(definition);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBeGreaterThanOrEqualTo(3);
        result.Errors.ShouldContain("Strategy must have at least one entry rule");
        result.Errors.ShouldContain("Strategy must have at least one exit rule");
        result.Errors.ShouldContain("Percentage must be between 0 and 1 (e.g., 0.95 for 95%)");
    }

    #endregion
}
