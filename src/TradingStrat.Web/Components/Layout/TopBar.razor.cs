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
    /// Today's return in dollars
    /// </summary>
    [Parameter]
    public decimal? TodayReturnDollars { get; set; }

    /// <summary>
    /// Today's return as a percentage
    /// </summary>
    [Parameter]
    public decimal? TodayReturnPercentage { get; set; }

    /// <summary>
    /// Win rate percentage (% of positions with unrealized gains > 0)
    /// </summary>
    [Parameter]
    public decimal? WinRatePercentage { get; set; }

    /// <summary>
    /// Whether to show the AI mode selector
    /// </summary>
    [Parameter]
    public bool ShowAiModeSelector { get; set; } = true;

    private string GetTodayReturnClass()
    {
        if (!TodayReturnDollars.HasValue)
        {
            return "text-sm font-semibold text-gray-900 dark:text-dark-text-primary";
        }

        return TodayReturnDollars.Value >= 0
            ? "text-sm font-semibold metric-positive"
            : "text-sm font-semibold metric-negative";
    }

    private string GetWinRateClass()
    {
        if (!WinRatePercentage.HasValue)
        {
            return "text-sm font-semibold text-gray-900 dark:text-dark-text-primary";
        }

        // Green if > 60%, gray if 40-60%, red if < 40%
        if (WinRatePercentage.Value > 60)
        {
            return "text-sm font-semibold metric-positive";
        }

        if (WinRatePercentage.Value < 40)
        {
            return "text-sm font-semibold metric-negative";
        }

        return "text-sm font-semibold text-gray-900 dark:text-dark-text-primary";
    }

    private void NavigateToSettings()
    {
        Navigation.NavigateTo("/settings");
    }
}
