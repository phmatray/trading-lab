using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace TradingStrat.Web.Components.Shared;

/// <summary>
/// Portfolio card component displaying portfolio summary information.
/// Used in the Portfolios page to show portfolio list in a grid layout.
/// </summary>
public partial class PortfolioCard : ComponentBase
{
    /// <summary>
    /// Portfolio name
    /// </summary>
    [Parameter, EditorRequired]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Portfolio description (optional)
    /// </summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>
    /// Cash balance in the portfolio
    /// </summary>
    [Parameter, EditorRequired]
    public decimal Cash { get; set; }

    /// <summary>
    /// Number of positions in the portfolio
    /// </summary>
    [Parameter, EditorRequired]
    public int PositionCount { get; set; }

    /// <summary>
    /// Portfolio creation date
    /// </summary>
    [Parameter, EditorRequired]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Callback invoked when the card is clicked
    /// </summary>
    [Parameter]
    public EventCallback OnClick { get; set; }

    /// <summary>
    /// Callback invoked when the delete button is clicked
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnDelete { get; set; }

    private async Task HandleClick()
    {
        await OnClick.InvokeAsync();
    }

    private async Task HandleDeleteClick(MouseEventArgs e)
    {
        await OnDelete.InvokeAsync(e);
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" || e.Key == " ")
        {
            await OnClick.InvokeAsync();
        }
    }
}
