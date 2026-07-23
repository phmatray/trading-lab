using Microsoft.AspNetCore.Components;

namespace TradingStrat.Web.Components.Shared;

/// <summary>
/// Sticky bulk action bar that appears when items are selected.
/// </summary>
public partial class BulkActionBar : ComponentBase
{
    #region Parameters

    /// <summary>
    /// Number of items currently selected.
    /// </summary>
    [Parameter]
    public int SelectedCount { get; set; }

    /// <summary>
    /// Whether to show the Refresh action button.
    /// </summary>
    [Parameter]
    public bool ShowRefreshAction { get; set; } = true;

    /// <summary>
    /// Whether to show the Delete action button.
    /// </summary>
    [Parameter]
    public bool ShowDeleteAction { get; set; } = true;

    /// <summary>
    /// Whether to show the Export action button.
    /// </summary>
    [Parameter]
    public bool ShowExportAction { get; set; } = true;

    /// <summary>
    /// Optional custom actions to display.
    /// </summary>
    [Parameter]
    public RenderFragment? CustomActions { get; set; }

    /// <summary>
    /// Callback invoked when the user clicks "Refresh Selected".
    /// </summary>
    [Parameter]
    public EventCallback OnRefreshSelected { get; set; }

    /// <summary>
    /// Callback invoked when the user clicks "Delete Selected".
    /// </summary>
    [Parameter]
    public EventCallback OnDeleteSelected { get; set; }

    /// <summary>
    /// Callback invoked when the user clicks "Export Selected".
    /// </summary>
    [Parameter]
    public EventCallback OnExportSelected { get; set; }

    /// <summary>
    /// Callback invoked when the user clicks "Clear".
    /// </summary>
    [Parameter]
    public EventCallback OnClearSelection { get; set; }

    #endregion
}
