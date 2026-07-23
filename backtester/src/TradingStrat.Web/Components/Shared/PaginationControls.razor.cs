using Microsoft.AspNetCore.Components;

namespace TradingStrat.Web.Components.Shared;

/// <summary>
/// Pagination controls component with page size selection.
/// </summary>
public partial class PaginationControls : ComponentBase
{
    #region Parameters

    /// <summary>
    /// Total number of pages available.
    /// </summary>
    [Parameter]
    public int TotalPages { get; set; }

    /// <summary>
    /// The current page number (1-based).
    /// </summary>
    [Parameter]
    public int CurrentPage { get; set; } = 1;

    /// <summary>
    /// The number of items per page.
    /// </summary>
    [Parameter]
    public int PageSize { get; set; } = 25;

    /// <summary>
    /// Available page size options.
    /// </summary>
    [Parameter]
    public int[] PageSizeOptions { get; set; } = [25, 50, 100, 200];

    /// <summary>
    /// Callback invoked when the user changes the page.
    /// </summary>
    [Parameter]
    public EventCallback<int> OnPageChanged { get; set; }

    /// <summary>
    /// Callback invoked when the user changes the page size.
    /// </summary>
    [Parameter]
    public EventCallback<int> OnPageSizeChanged { get; set; }

    #endregion

    #region Event Handlers

    private async Task HandlePreviousPage()
    {
        if (CurrentPage > 1)
        {
            await OnPageChanged.InvokeAsync(CurrentPage - 1);
        }
    }

    private async Task HandleNextPage()
    {
        if (CurrentPage < TotalPages)
        {
            await OnPageChanged.InvokeAsync(CurrentPage + 1);
        }
    }

    private async Task HandlePageSizeChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int newPageSize))
        {
            await OnPageSizeChanged.InvokeAsync(newPageSize);
        }
    }

    #endregion
}
