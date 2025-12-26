using Microsoft.AspNetCore.Components;
using TradingStrat.Application.Ports.Inbound;

namespace TradingStrat.Web.Components.Shared;

/// <summary>
/// Table displaying data coverage information for each ticker.
/// </summary>
public partial class DataCoverageTable : ComponentBase
{
    #region Parameters

    /// <summary>
    /// List of ticker data statuses to display.
    /// </summary>
    [Parameter]
    public List<TickerDataStatus> TickerStatuses { get; set; } = new();

    #endregion

    #region Helper Methods

    private string GetCoverageBarColor(decimal coverage)
    {
        if (coverage >= 95)
        {
            return "bg-green-500 dark:bg-green-400";
        }

        if (coverage >= 80)
        {
            return "bg-yellow-500 dark:bg-yellow-400";
        }

        return "bg-red-500 dark:bg-red-400";
    }

    #endregion
}
