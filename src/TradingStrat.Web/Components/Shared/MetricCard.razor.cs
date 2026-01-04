using Microsoft.AspNetCore.Components;

namespace TradingStrat.Web.Components.Shared;

public partial class MetricCard : ComponentBase
{
    [Parameter] public string Label { get; set; } = string.Empty;
    [Parameter] public string Value { get; set; } = string.Empty;
    [Parameter] public bool? IsPositive { get; set; }
    [Parameter] public bool ShowIcon { get; set; } = false;
    [Parameter] public string Icon { get; set; } = "chart";
    [Parameter] public IconCategory IconCategory { get; set; } = IconCategory.Default;

    private string GetColorClass()
    {
        if (!IsPositive.HasValue)
        {
            return "text-gray-900 dark:text-dark-text-primary";
        }
        return IsPositive.Value ? "metric-positive" : "metric-negative";
    }
}
