using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TradingStrat.Application.Ports.Inbound;

namespace TradingStrat.Web.Components.Shared;

/// <summary>
/// Sortable table column header component with keyboard accessibility.
/// </summary>
public partial class SortableColumnHeader : ComponentBase
{
    #region Parameters

    /// <summary>
    /// The display label for the column header.
    /// </summary>
    [Parameter, EditorRequired]
    public required string Label { get; set; }

    /// <summary>
    /// The column identifier that this header represents.
    /// </summary>
    [Parameter, EditorRequired]
    public required SortColumn Column { get; set; }

    /// <summary>
    /// The currently active sort column.
    /// </summary>
    [Parameter]
    public SortColumn? CurrentSortColumn { get; set; }

    /// <summary>
    /// The current sort direction.
    /// </summary>
    [Parameter]
    public SortDirection CurrentSortDirection { get; set; } = SortDirection.Ascending;

    /// <summary>
    /// Callback invoked when the user clicks to sort by this column.
    /// </summary>
    [Parameter]
    public EventCallback<(SortColumn Column, SortDirection Direction)> OnSort { get; set; }

    #endregion

    #region Event Handlers

    private async Task HandleClick()
    {
        SortDirection newDirection;

        if (CurrentSortColumn == Column)
        {
            // Toggle direction if already sorting by this column
            newDirection = CurrentSortDirection == SortDirection.Ascending
                ? SortDirection.Descending
                : SortDirection.Ascending;
        }
        else
        {
            // Default to ascending when clicking a new column
            newDirection = SortDirection.Ascending;
        }

        await OnSort.InvokeAsync((Column, newDirection));
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" || e.Key == " ")
        {
            await HandleClick();
        }
    }

    #endregion
}
