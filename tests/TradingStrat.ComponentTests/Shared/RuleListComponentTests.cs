using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Domain.ValueObjects;
using TradingStrat.Web.Components.Shared;
using TradingStrat.Web.Models;
using TradingStrat.Web.Services;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the RuleListComponent - a complex component for managing trading strategy rules.
/// </summary>
public class RuleListComponentTests : BunitTestContext
{
    private readonly IndicatorMetadataService _indicatorService;

    public RuleListComponentTests()
    {
        _indicatorService = new IndicatorMetadataService();
        Services.Add(new ServiceDescriptor(typeof(IndicatorMetadataService), _indicatorService));
    }

    [Fact]
    public void RuleListComponent_WithEmptyRules_DisplaysEmptyState()
    {
        // Arrange
        var rules = new List<RuleFormModel>();

        // Act
        var cut = Render<RuleListComponent>(parameters => parameters
            .Add(p => p.Rules, rules));

        // Assert
        cut.Markup.ShouldContain("No rules defined yet");
        cut.Markup.ShouldContain("Add First Rule");
        var button = cut.Find("button");
        button.TextContent.ShouldContain("Add First Rule");
    }

    [Fact]
    public void RuleListComponent_WithCustomEmptyMessage_DisplaysMessage()
    {
        // Arrange
        var rules = new List<RuleFormModel>();

        // Act
        var cut = Render<RuleListComponent>(parameters => parameters
            .Add(p => p.Rules, rules)
            .Add(p => p.EmptyStateMessage, "Create your first entry rule"));

        // Assert
        cut.Markup.ShouldContain("Create your first entry rule");
    }

    [Fact]
    public void RuleListComponent_AddFirstRule_CreatesNewRule()
    {
        // Arrange
        var rules = new List<RuleFormModel>();
        bool changedCalled = false;

        var cut = Render<RuleListComponent>(parameters => parameters
            .Add(p => p.Rules, rules)
            .Add(p => p.OnRulesChanged, () => changedCalled = true));

        var addButton = cut.Find("button");

        // Act
        addButton.Click();

        // Assert
        rules.Count.ShouldBe(1);
        rules[0].Operator.ShouldBe(ComparisonOperator.GreaterThan);
        rules[0].ValueType.ShouldBe(RuleValueType.Constant);
        rules[0].ConstantValue.ShouldBe(0);
        rules[0].LogicalOperator.ShouldBe(LogicalOperator.None);
        changedCalled.ShouldBeTrue();
    }

    [Fact]
    public void RuleListComponent_WithSingleRule_DisplaysRuleFields()
    {
        // Arrange
        var rules = new List<RuleFormModel>
        {
            new RuleFormModel
            {
                IndicatorName = "RSI",
                IndicatorParameters = new Dictionary<string, object> { ["Period"] = 14 },
                Operator = ComparisonOperator.LessThan,
                ValueType = RuleValueType.Constant,
                ConstantValue = 30
            }
        };

        // Act
        var cut = Render<RuleListComponent>(parameters => parameters
            .Add(p => p.Rules, rules));

        // Assert
        // Should display indicator dropdown
        var indicatorSelect = cut.Find("[data-testid='rule-0-indicator']");
        indicatorSelect.ShouldNotBeNull();

        // Should display operator dropdown
        var operatorSelect = cut.Find("[data-testid='rule-0-operator']");
        operatorSelect.ShouldNotBeNull();

        // Should display value type dropdown
        var valueTypeSelect = cut.Find("[data-testid='rule-0-value-type']");
        valueTypeSelect.ShouldNotBeNull();

        // Should display constant value input
        var valueInput = cut.Find("[data-testid='rule-0-value']");
        valueInput.ShouldNotBeNull();
    }

    [Fact]
    public void RuleListComponent_WithSingleRule_DoesNotShowLogicalOperator()
    {
        // Arrange
        var rules = new List<RuleFormModel>
        {
            new RuleFormModel { IndicatorName = "RSI" }
        };

        // Act
        var cut = Render<RuleListComponent>(parameters => parameters
            .Add(p => p.Rules, rules));

        // Assert
        // Should not display AND/OR radio buttons (more specific check for radio inputs)
        var radios = cut.FindAll("input[type='radio']");
        radios.ShouldBeEmpty();
    }

    [Fact]
    public void RuleListComponent_WithMultipleRules_ShowsLogicalOperator()
    {
        // Arrange
        var rules = new List<RuleFormModel>
        {
            new RuleFormModel
            {
                IndicatorName = "RSI",
                LogicalOperator = LogicalOperator.And
            },
            new RuleFormModel { IndicatorName = "MACD" }
        };

        // Act
        var cut = Render<RuleListComponent>(parameters => parameters
            .Add(p => p.Rules, rules));

        // Assert
        // Should display AND/OR radio buttons for first rule only
        cut.Markup.ShouldContain("AND");
        cut.Markup.ShouldContain("OR");
    }

    [Fact]
    public void RuleListComponent_RemoveRule_UpdatesRulesList()
    {
        // Arrange
        var rules = new List<RuleFormModel>
        {
            new RuleFormModel { IndicatorName = "RSI" },
            new RuleFormModel { IndicatorName = "MACD", LogicalOperator = LogicalOperator.Or }
        };
        bool changedCalled = false;

        var cut = Render<RuleListComponent>(parameters => parameters
            .Add(p => p.Rules, rules)
            .Add(p => p.OnRulesChanged, () => changedCalled = true));

        // Act - Find and click the first remove button
        var removeButtons = cut.FindAll("button.btn-danger");
        removeButtons[0].Click();

        // Assert
        rules.Count.ShouldBe(1);
        rules[0].IndicatorName.ShouldBe("MACD");
        rules[0].LogicalOperator.ShouldBe(LogicalOperator.None); // Last rule should have None
        changedCalled.ShouldBeTrue();
    }

    [Fact]
    public void RuleListComponent_AddSecondRule_SetsAndLogicalOperator()
    {
        // Arrange
        var rules = new List<RuleFormModel>
        {
            new RuleFormModel { IndicatorName = "RSI" }
        };

        var cut = Render<RuleListComponent>(parameters => parameters
            .Add(p => p.Rules, rules));

        // Act
        var addButton = cut.Find("button.btn-secondary"); // "Add Another Rule"
        addButton.Click();

        // Assert
        rules.Count.ShouldBe(2);
        rules[1].LogicalOperator.ShouldBe(LogicalOperator.And);
    }

    [Fact]
    public void RuleListComponent_ChangeIndicator_UpdatesRule()
    {
        // Arrange
        var rules = new List<RuleFormModel>
        {
            new RuleFormModel { IndicatorName = "" }
        };
        bool changedCalled = false;

        var cut = Render<RuleListComponent>(parameters => parameters
            .Add(p => p.Rules, rules)
            .Add(p => p.OnRulesChanged, () => changedCalled = true));

        // Act
        var indicatorSelect = cut.Find("[data-testid='rule-0-indicator']");
        indicatorSelect.Change("RSI");

        // Assert
        rules[0].IndicatorName.ShouldBe("RSI");
        rules[0].IndicatorParameters.ShouldNotBeEmpty();
        rules[0].IndicatorParameters.ContainsKey("Period").ShouldBeTrue();
        changedCalled.ShouldBeTrue();
    }

    [Fact]
    public void RuleListComponent_ChangeOperator_UpdatesRule()
    {
        // Arrange
        var rules = new List<RuleFormModel>
        {
            new RuleFormModel
            {
                IndicatorName = "RSI",
                Operator = ComparisonOperator.GreaterThan
            }
        };
        bool changedCalled = false;

        var cut = Render<RuleListComponent>(parameters => parameters
            .Add(p => p.Rules, rules)
            .Add(p => p.OnRulesChanged, () => changedCalled = true));

        // Act
        var operatorSelect = cut.Find("[data-testid='rule-0-operator']");
        operatorSelect.Change(ComparisonOperator.LessThan.ToString());

        // Assert
        rules[0].Operator.ShouldBe(ComparisonOperator.LessThan);
        changedCalled.ShouldBeTrue();
    }

    [Fact]
    public void RuleListComponent_ChangeValueType_ToConstant_ResetsValues()
    {
        // Arrange
        var rules = new List<RuleFormModel>
        {
            new RuleFormModel
            {
                IndicatorName = "RSI",
                ValueType = RuleValueType.Indicator,
                SecondIndicatorName = "SMA"
            }
        };
        bool changedCalled = false;

        var cut = Render<RuleListComponent>(parameters => parameters
            .Add(p => p.Rules, rules)
            .Add(p => p.OnRulesChanged, () => changedCalled = true));

        // Act
        var valueTypeSelect = cut.Find("[data-testid='rule-0-value-type']");
        valueTypeSelect.Change(RuleValueType.Constant.ToString());

        // Assert
        rules[0].ValueType.ShouldBe(RuleValueType.Constant);
        rules[0].ConstantValue.ShouldBe(0);
        rules[0].SecondIndicatorName.ShouldBeNull();
        changedCalled.ShouldBeTrue();
    }

    [Fact]
    public void RuleListComponent_ChangeValueType_ToPrice_ResetsValues()
    {
        // Arrange
        var rules = new List<RuleFormModel>
        {
            new RuleFormModel
            {
                IndicatorName = "RSI",
                ValueType = RuleValueType.Constant,
                ConstantValue = 30
            }
        };

        var cut = Render<RuleListComponent>(parameters => parameters
            .Add(p => p.Rules, rules));

        // Act
        var valueTypeSelect = cut.Find("[data-testid='rule-0-value-type']");
        valueTypeSelect.Change(RuleValueType.Price.ToString());

        // Assert
        rules[0].ValueType.ShouldBe(RuleValueType.Price);
        rules[0].ConstantValue.ShouldBeNull();
        rules[0].SecondIndicatorName.ShouldBeNull();
    }

    [Fact]
    public void RuleListComponent_ChangeValueType_ToIndicator_ResetsValues()
    {
        // Arrange
        var rules = new List<RuleFormModel>
        {
            new RuleFormModel
            {
                IndicatorName = "SMA",
                ValueType = RuleValueType.Constant,
                ConstantValue = 50
            }
        };

        var cut = Render<RuleListComponent>(parameters => parameters
            .Add(p => p.Rules, rules));

        // Act
        var valueTypeSelect = cut.Find("[data-testid='rule-0-value-type']");
        valueTypeSelect.Change(RuleValueType.Indicator.ToString());

        // Assert
        rules[0].ValueType.ShouldBe(RuleValueType.Indicator);
        rules[0].ConstantValue.ShouldBeNull();
        rules[0].SecondIndicatorName.ShouldBe(string.Empty);
    }

    [Fact]
    public void RuleListComponent_ValueTypeConstant_DisplaysNumberInput()
    {
        // Arrange
        var rules = new List<RuleFormModel>
        {
            new RuleFormModel
            {
                IndicatorName = "RSI",
                ValueType = RuleValueType.Constant,
                ConstantValue = 30
            }
        };

        // Act
        var cut = Render<RuleListComponent>(parameters => parameters
            .Add(p => p.Rules, rules));

        // Assert
        var valueInput = cut.Find("[data-testid='rule-0-value']");
        valueInput.GetAttribute("type").ShouldBe("number");
        valueInput.GetAttribute("step").ShouldBe("0.01");
    }

    [Fact]
    public void RuleListComponent_ValueTypePrice_DisplaysDisabledInput()
    {
        // Arrange
        var rules = new List<RuleFormModel>
        {
            new RuleFormModel
            {
                IndicatorName = "RSI",
                ValueType = RuleValueType.Price
            }
        };

        // Act
        var cut = Render<RuleListComponent>(parameters => parameters
            .Add(p => p.Rules, rules));

        // Assert
        var inputs = cut.FindAll("input[disabled]");
        inputs.ShouldContain(input => input.GetAttribute("value") == "Current Price");
    }

    [Fact]
    public void RuleListComponent_ValueTypeIndicator_DisplaysIndicatorDropdown()
    {
        // Arrange
        var rules = new List<RuleFormModel>
        {
            new RuleFormModel
            {
                IndicatorName = "SMA",
                ValueType = RuleValueType.Indicator
            }
        };

        // Act
        var cut = Render<RuleListComponent>(parameters => parameters
            .Add(p => p.Rules, rules));

        // Assert
        var secondIndicatorSelect = cut.Find("[data-testid='rule-0-second-indicator']");
        secondIndicatorSelect.ShouldNotBeNull();
        secondIndicatorSelect.TagName.ShouldBe("SELECT");
    }

    [Fact]
    public void RuleListComponent_ChangeConstantValue_UpdatesRule()
    {
        // Arrange
        var rules = new List<RuleFormModel>
        {
            new RuleFormModel
            {
                IndicatorName = "RSI",
                ValueType = RuleValueType.Constant,
                ConstantValue = 0
            }
        };
        bool changedCalled = false;

        var cut = Render<RuleListComponent>(parameters => parameters
            .Add(p => p.Rules, rules)
            .Add(p => p.OnRulesChanged, () => changedCalled = true));

        // Act
        var valueInput = cut.Find("[data-testid='rule-0-value']");
        valueInput.Change("70");

        // Assert
        rules[0].ConstantValue.ShouldBe(70m);
        changedCalled.ShouldBeTrue();
    }

    [Fact]
    public void RuleListComponent_ChangeSecondIndicator_UpdatesRule()
    {
        // Arrange
        var rules = new List<RuleFormModel>
        {
            new RuleFormModel
            {
                IndicatorName = "SMA",
                ValueType = RuleValueType.Indicator,
                SecondIndicatorName = ""
            }
        };
        bool changedCalled = false;

        var cut = Render<RuleListComponent>(parameters => parameters
            .Add(p => p.Rules, rules)
            .Add(p => p.OnRulesChanged, () => changedCalled = true));

        // Act
        var secondIndicatorSelect = cut.Find("[data-testid='rule-0-second-indicator']");
        secondIndicatorSelect.Change("EMA");

        // Assert
        rules[0].SecondIndicatorName.ShouldBe("EMA");
        rules[0].SecondIndicatorParameters.ShouldNotBeNull();
        changedCalled.ShouldBeTrue();
    }

    [Fact]
    public void RuleListComponent_WithIndicatorParameters_DisplaysParameterInput()
    {
        // Arrange
        var rules = new List<RuleFormModel>
        {
            new RuleFormModel
            {
                IndicatorName = "RSI", // RSI has a Period parameter
                IndicatorParameters = new Dictionary<string, object> { ["Period"] = 14 }
            }
        };

        // Act
        var cut = Render<RuleListComponent>(parameters => parameters
            .Add(p => p.Rules, rules));

        // Assert
        // Should display parameter input (number input)
        var paramInputs = cut.FindAll("input[type='number']");
        paramInputs.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void RuleListComponent_HasRemoveButton_ForEachRule()
    {
        // Arrange
        var rules = new List<RuleFormModel>
        {
            new RuleFormModel { IndicatorName = "RSI" },
            new RuleFormModel { IndicatorName = "MACD" },
            new RuleFormModel { IndicatorName = "SMA" }
        };

        // Act
        var cut = Render<RuleListComponent>(parameters => parameters
            .Add(p => p.Rules, rules));

        // Assert
        var removeButtons = cut.FindAll("button.btn-danger");
        removeButtons.Count.ShouldBe(3);
    }

    [Fact]
    public void RuleListComponent_WithRules_DisplaysAddAnotherButton()
    {
        // Arrange
        var rules = new List<RuleFormModel>
        {
            new RuleFormModel { IndicatorName = "RSI" }
        };

        // Act
        var cut = Render<RuleListComponent>(parameters => parameters
            .Add(p => p.Rules, rules));

        // Assert
        var addButton = cut.Find("button.btn-secondary");
        addButton.TextContent.ShouldContain("Add Another Rule");
    }

    [Fact]
    public void RuleListComponent_IndicatorDropdown_HasOptionGroups()
    {
        // Arrange
        var rules = new List<RuleFormModel>
        {
            new RuleFormModel { IndicatorName = "" }
        };

        // Act
        var cut = Render<RuleListComponent>(parameters => parameters
            .Add(p => p.Rules, rules));

        // Assert
        var optgroups = cut.FindAll("optgroup");
        optgroups.ShouldNotBeEmpty();
        // Should have categories like "Price", "Momentum", etc.
        optgroups.ShouldContain(og => og.GetAttribute("label") == "Momentum");
    }

    [Fact]
    public void RuleListComponent_OperatorDropdown_HasAllOperators()
    {
        // Arrange
        var rules = new List<RuleFormModel>
        {
            new RuleFormModel { IndicatorName = "RSI" }
        };

        // Act
        var cut = Render<RuleListComponent>(parameters => parameters
            .Add(p => p.Rules, rules));

        // Assert
        cut.Markup.ShouldContain("Crosses Above");
        cut.Markup.ShouldContain("Crosses Below");
        cut.Markup.ShouldContain("&gt;"); // > operator
        cut.Markup.ShouldContain("&lt;"); // < operator
    }

    [Fact]
    public void RuleListComponent_ValueTypeDropdown_HasAllValueTypes()
    {
        // Arrange
        var rules = new List<RuleFormModel>
        {
            new RuleFormModel { IndicatorName = "RSI" }
        };

        // Act
        var cut = Render<RuleListComponent>(parameters => parameters
            .Add(p => p.Rules, rules));

        // Assert
        cut.Markup.ShouldContain("Number");
        cut.Markup.ShouldContain("Price");
        cut.Markup.ShouldContain("Indicator");
    }
}
