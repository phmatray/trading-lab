using Microsoft.AspNetCore.Components;

namespace TradingStrat.Web.Components.Layout;

public partial class TopBar : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    /// <summary>
    /// Selected portfolio name
    /// </summary>
    [Parameter]
    public string? SelectedPortfolioName { get; set; }

    /// <summary>
    /// Current portfolio value
    /// </summary>
    [Parameter]
    public decimal? PortfolioValue { get; set; }

    /// <summary>
    /// Year-to-date performance percentage
    /// </summary>
    [Parameter]
    public decimal? YtdPerformance { get; set; }

    /// <summary>
    /// Whether to show the AI mode selector
    /// </summary>
    [Parameter]
    public bool ShowAiModeSelector { get; set; } = true;

    private string GetPerformanceClass()
    {
        if (!YtdPerformance.HasValue)
        {
            return "";
        }

        return YtdPerformance.Value >= 0
            ? "metric-positive text-sm font-semibold"
            : "metric-negative text-sm font-semibold";
    }

    private void NavigateToSettings()
    {
        Navigation.NavigateTo("/settings");
    }
}
