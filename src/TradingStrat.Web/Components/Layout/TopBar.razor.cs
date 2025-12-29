using Microsoft.AspNetCore.Components;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Entities;
using TradingStrat.Web.Services.State;

namespace TradingStrat.Web.Components.Layout;

public partial class TopBar : ComponentBase
{
    [Inject] private IPortfolioPort PortfolioPort { get; set; } = null!;
    [Inject] private PortfolioStateService PortfolioState { get; set; } = null!;

    private List<Portfolio> _portfolios = new();

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

    protected override async Task OnInitializedAsync()
    {
        _portfolios = await PortfolioPort.GetAllPortfoliosAsync();
    }

    private async Task HandlePortfolioSelection(int portfolioId)
    {
        await PortfolioState.SetSelectedPortfolioAsync(portfolioId);
    }

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
}
