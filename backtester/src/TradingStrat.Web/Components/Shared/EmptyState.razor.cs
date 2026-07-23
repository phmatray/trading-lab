using Microsoft.AspNetCore.Components;

namespace TradingStrat.Web.Components.Shared;

public partial class EmptyState : ComponentBase
{
    /// <summary>
    /// Icon to display (folder, document, chart, portfolio, alert, search, database)
    /// </summary>
    [Parameter]
    public string Icon { get; set; } = "folder";

    /// <summary>
    /// Main title text
    /// </summary>
    [Parameter]
    public string Title { get; set; } = "No data";

    /// <summary>
    /// Optional description text
    /// </summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>
    /// Optional action buttons or links
    /// </summary>
    [Parameter]
    public RenderFragment? Actions { get; set; }

    /// <summary>
    /// Size of the icon (small: 8x8, medium: 12x12, large: 16x16)
    /// </summary>
    [Parameter]
    public string Size { get; set; } = "medium";

    private IconSize GetIconSize()
    {
        return Size.ToLowerInvariant() switch
        {
            "small" => IconSize.Small,
            "large" => IconSize.Hero,
            _ => IconSize.Large // medium (default) - maps to Large for empty states
        };
    }
}
