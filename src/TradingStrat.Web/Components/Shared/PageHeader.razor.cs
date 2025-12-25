using Microsoft.AspNetCore.Components;

namespace TradingStrat.Web.Components.Shared;

public partial class PageHeader : ComponentBase
{
    /// <summary>
    /// Page title
    /// </summary>
    [Parameter]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional page description
    /// </summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>
    /// Optional action buttons (right-aligned)
    /// </summary>
    [Parameter]
    public RenderFragment? Actions { get; set; }

    /// <summary>
    /// Optional breadcrumbs
    /// </summary>
    [Parameter]
    public RenderFragment? Breadcrumbs { get; set; }

    /// <summary>
    /// Optional back button callback
    /// </summary>
    [Parameter]
    public EventCallback OnBack { get; set; }

    private async Task HandleBack()
    {
        if (OnBack.HasDelegate)
        {
            await OnBack.InvokeAsync();
        }
    }
}
