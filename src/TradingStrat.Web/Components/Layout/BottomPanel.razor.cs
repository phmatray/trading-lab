using Microsoft.AspNetCore.Components;

namespace TradingStrat.Web.Components.Layout;

public partial class BottomPanel : ComponentBase
{
    /// <summary>
    /// Whether the panel is visible
    /// </summary>
    [Parameter]
    public bool IsVisible { get; set; }

    /// <summary>
    /// Callback when visibility changes
    /// </summary>
    [Parameter]
    public EventCallback<bool> IsVisibleChanged { get; set; }

    /// <summary>
    /// Panel content
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Panel title
    /// </summary>
    [Parameter]
    public string Title { get; set; } = "Additional Information";

    /// <summary>
    /// Panel height in pixels
    /// </summary>
    [Parameter]
    public int Height { get; set; } = 200;

    private string GetPanelClasses()
    {
        return "fixed bottom-0 left-0 right-0 z-20 border-t border-gray-200 dark:border-dark-border shadow-lg";
    }

    private async Task HandleClose()
    {
        await IsVisibleChanged.InvokeAsync(false);
    }
}
