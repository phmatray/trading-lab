using Microsoft.AspNetCore.Components;
using TradingStrat.Application.Ports.Inbound;

namespace TradingStrat.Web.Components.Shared;

/// <summary>
/// Collapsible filter panel for data status and coverage filtering.
/// </summary>
public partial class FilterPanel : ComponentBase
{
    #region Parameters

    /// <summary>
    /// The selected status filter.
    /// </summary>
    [Parameter]
    public DataStatusFilter? StatusFilter { get; set; }

    /// <summary>
    /// Minimum coverage percentage filter.
    /// </summary>
    [Parameter]
    public decimal? MinCoverage { get; set; }

    /// <summary>
    /// Maximum coverage percentage filter.
    /// </summary>
    [Parameter]
    public decimal? MaxCoverage { get; set; }

    /// <summary>
    /// Callback invoked when any filter changes.
    /// </summary>
    [Parameter]
    public EventCallback<FilterValues> OnFilterChanged { get; set; }

    /// <summary>
    /// Callback invoked when filters are reset.
    /// </summary>
    [Parameter]
    public EventCallback OnReset { get; set; }

    #endregion

    #region Private Fields

    private bool _isCollapsed = false;

    #endregion

    #region Properties

    private bool HasActiveFilters =>
        StatusFilter.HasValue || MinCoverage.HasValue || MaxCoverage.HasValue;

    #endregion

    #region Event Handlers

    private void ToggleCollapse()
    {
        _isCollapsed = !_isCollapsed;
    }

    private async Task HandleStatusFilterChanged(ChangeEventArgs e)
    {
        DataStatusFilter? newFilter = null;
        if (!string.IsNullOrEmpty(e.Value?.ToString()) &&
            Enum.TryParse<DataStatusFilter>(e.Value.ToString(), out DataStatusFilter parsedFilter))
        {
            newFilter = parsedFilter;
        }

        await OnFilterChanged.InvokeAsync(new FilterValues(newFilter, MinCoverage, MaxCoverage));
    }

    private async Task HandleMinCoverageChanged(ChangeEventArgs e)
    {
        decimal? newMin = null;
        if (decimal.TryParse(e.Value?.ToString(), out decimal parsedMin))
        {
            newMin = parsedMin;
        }

        await OnFilterChanged.InvokeAsync(new FilterValues(StatusFilter, newMin, MaxCoverage));
    }

    private async Task HandleMaxCoverageChanged(ChangeEventArgs e)
    {
        decimal? newMax = null;
        if (decimal.TryParse(e.Value?.ToString(), out decimal parsedMax))
        {
            newMax = parsedMax;
        }

        await OnFilterChanged.InvokeAsync(new FilterValues(StatusFilter, MinCoverage, newMax));
    }

    private async Task HandleReset()
    {
        await OnReset.InvokeAsync();
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// Value object containing all filter values.
    /// </summary>
    public record FilterValues(
        DataStatusFilter? StatusFilter,
        decimal? MinCoverage,
        decimal? MaxCoverage);

    #endregion
}
