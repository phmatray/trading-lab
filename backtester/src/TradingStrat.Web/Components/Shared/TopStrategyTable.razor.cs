using Microsoft.AspNetCore.Components;
using TradingStrat.Application.Ports.Inbound;

namespace TradingStrat.Web.Components.Shared;

/// <summary>
/// Displays top-performing strategies in a table format.
/// </summary>
public partial class TopStrategyTable : ComponentBase
{
    #region Parameters

    /// <summary>
    /// List of top-performing strategies to display.
    /// </summary>
    [Parameter]
    public List<TopStrategyResult> Strategies { get; set; } = new();

    #endregion
}
