using Microsoft.AspNetCore.Components;
using TradingStrat.Domain.Entities;
using TradingStrat.Web.Services.State;

namespace TradingStrat.Web.Components.Pages.Workspace;

/// <summary>
/// Tab component for optimizing strategy parameters (gateway to full optimization page).
/// </summary>
public partial class TabOptimize : ComponentBase
{
    #region Parameters

    /// <summary>
    /// The custom strategy to optimize.
    /// </summary>
    [Parameter]
    public CustomStrategy? Strategy { get; set; }

    /// <summary>
    /// Callback invoked when optimization completes.
    /// </summary>
    [Parameter]
    public EventCallback<WorkspaceOptimizationResult> OnOptimizationComplete { get; set; }

    #endregion

    #region Private Fields

#pragma warning disable CS0649  // Field is never assigned to (intentionally - set externally if needed)
#pragma warning disable IDE0044 // Make field readonly
    private WorkspaceOptimizationResult? _optimizationResult;
#pragma warning restore IDE0044 // Make field readonly
#pragma warning restore CS0649

    #endregion

    // This tab primarily serves as a gateway to the full optimization page
    // The actual optimization logic is handled in /strategies/optimize
}
