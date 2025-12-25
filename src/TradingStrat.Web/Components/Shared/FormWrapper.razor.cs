using Microsoft.AspNetCore.Components;

namespace TradingStrat.Web.Components.Shared;

public partial class FormWrapper : ComponentBase
{
    /// <summary>
    /// Form content
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Error message to display
    /// </summary>
    [Parameter]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Warning message to display
    /// </summary>
    [Parameter]
    public string? WarningMessage { get; set; }

    /// <summary>
    /// Success message to display
    /// </summary>
    [Parameter]
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Whether to show the progress indicator
    /// </summary>
    [Parameter]
    public bool ShowProgressIndicator { get; set; }
}
