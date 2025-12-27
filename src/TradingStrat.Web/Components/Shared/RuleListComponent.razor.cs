using Microsoft.AspNetCore.Components;
using TradingStrat.Domain.ValueObjects;
using TradingStrat.Web.Models;
using TradingStrat.Web.Services;
using static TradingStrat.Web.Services.DebugLogger;

namespace TradingStrat.Web.Components.Shared;

public partial class RuleListComponent : ComponentBase
{
    [Inject] private IndicatorMetadataService IndicatorService { get; set; } = null!;

    [Parameter]
    public List<RuleFormModel> Rules { get; set; } = [];

    [Parameter]
    public string EmptyStateMessage { get; set; } = "No rules defined yet. Add your first rule to get started.";

    [Parameter]
    public EventCallback OnRulesChanged { get; set; }

    private async Task AddRule()
    {
        RuleFormModel newRule = new()
        {
            IndicatorName = string.Empty,
            Operator = ComparisonOperator.GreaterThan,
            ValueType = RuleValueType.Constant,
            ConstantValue = 0,
            LogicalOperator = Rules.Count > 0 ? LogicalOperator.And : LogicalOperator.None
        };
        Rules.Add(newRule);
        Log($"[RuleListComponent] Added new rule at index {Rules.Count - 1}. Total rules: {Rules.Count}");

        await NotifyChanged();
    }

    private async Task RemoveRule(int index)
    {
        Rules.RemoveAt(index);

        // Update logical operators
        if (Rules.Count > 0)
        {
            Rules[^1].LogicalOperator = LogicalOperator.None;
        }

        await NotifyChanged();
    }

    private async Task OnIndicatorChanged(int index, string indicatorName)
    {
        Log($"[RuleListComponent] OnIndicatorChanged: index={index}, indicator={indicatorName}");
        Rules[index].IndicatorName = indicatorName;
        Rules[index].IndicatorParameters = IndicatorService.GetDefaultParameters(indicatorName);
        Log($"[RuleListComponent]   Parameters count: {Rules[index].IndicatorParameters.Count}");
        foreach (KeyValuePair<string, object> param in Rules[index].IndicatorParameters)
        {
            Log($"[RuleListComponent]     {param.Key} = {param.Value}");
        }
        await NotifyChanged();
    }

    private async Task OnParameterChanged(int index, string paramName, object? value)
    {
        Log($"[RuleListComponent] OnParameterChanged: index={index}, param={paramName}, value={value}");
        if (value != null && int.TryParse(value.ToString(), out int intValue))
        {
            Rules[index].IndicatorParameters[paramName] = intValue;
            await NotifyChanged();
        }
    }

    private async Task OnOperatorChanged(int index, string operatorValue)
    {
        Log($"[RuleListComponent] OnOperatorChanged: index={index}, operator={operatorValue}");
        if (Enum.TryParse<ComparisonOperator>(operatorValue, out ComparisonOperator op))
        {
            Rules[index].Operator = op;
            Log($"[RuleListComponent]   Parsed to: {op}");
            await NotifyChanged();
        }
    }

    private async Task OnValueTypeChanged(int index, string valueType)
    {
        Log($"[RuleListComponent] OnValueTypeChanged: index={index}, valueType={valueType}");
        if (Enum.TryParse<RuleValueType>(valueType, out RuleValueType vt))
        {
            Rules[index].ValueType = vt;

            // Reset values when type changes
            if (vt == RuleValueType.Constant)
            {
                Rules[index].ConstantValue = 0;
                Rules[index].SecondIndicatorName = null;
            }
            else if (vt == RuleValueType.Indicator)
            {
                Rules[index].ConstantValue = null;
                Rules[index].SecondIndicatorName = string.Empty;
            }
            else // Price
            {
                Rules[index].ConstantValue = null;
                Rules[index].SecondIndicatorName = null;
            }

            await NotifyChanged();
        }
    }

    private async Task OnConstantValueChanged(int index, object? value)
    {
        Log($"[RuleListComponent] OnConstantValueChanged: index={index}, value={value}, type={value?.GetType().Name}");
        if (value != null && decimal.TryParse(value.ToString(), out decimal decValue))
        {
            Rules[index].ConstantValue = decValue;
            Log($"[RuleListComponent]   Parsed to decimal: {decValue}");
            await NotifyChanged();
        }
        else
        {
            Log($"[RuleListComponent]   FAILED to parse to decimal!");
        }
    }

    private async Task OnSecondIndicatorChanged(int index, string indicatorName)
    {
        Rules[index].SecondIndicatorName = indicatorName;
        Rules[index].SecondIndicatorParameters = IndicatorService.GetDefaultParameters(indicatorName);
        await NotifyChanged();
    }

    private async Task OnLogicalOperatorChanged(int index, LogicalOperator logicalOp)
    {
        Rules[index].LogicalOperator = logicalOp;
        await NotifyChanged();
    }

    private object GetParameterValue(RuleFormModel rule, string paramName)
    {
        if (rule.IndicatorParameters.TryGetValue(paramName, out object? value))
        {
            return value;
        }

        return 14; // default
    }

    private async Task NotifyChanged()
    {
        await OnRulesChanged.InvokeAsync();
    }
}
