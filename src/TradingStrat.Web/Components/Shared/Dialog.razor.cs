using Microsoft.AspNetCore.Components;

namespace TradingStrat.Web.Components.Shared;

public partial class Dialog : ComponentBase
{
    /// <summary>
    /// Controls the visibility of the dialog
    /// </summary>
    [Parameter]
    public bool IsOpen { get; set; }

    /// <summary>
    /// Dialog title displayed in the header
    /// </summary>
    [Parameter]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Callback invoked when the dialog should be closed
    /// </summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    /// <summary>
    /// Main content of the dialog
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Footer content (typically action buttons)
    /// </summary>
    [Parameter]
    public RenderFragment? Footer { get; set; }

    /// <summary>
    /// Optional custom header actions (replaces default close button)
    /// </summary>
    [Parameter]
    public RenderFragment? HeaderActions { get; set; }

    /// <summary>
    /// Width of the dialog (Tailwind class, e.g., "w-96", "w-[500px]")
    /// </summary>
    [Parameter]
    public string Width { get; set; } = "w-96";

    /// <summary>
    /// Whether clicking the backdrop should close the dialog
    /// </summary>
    [Parameter]
    public bool CloseOnBackdropClick { get; set; } = true;

    private async Task HandleClose()
    {
        if (OnClose.HasDelegate)
        {
            await OnClose.InvokeAsync();
        }
    }

    private async Task HandleBackdropClick()
    {
        if (CloseOnBackdropClick)
        {
            await HandleClose();
        }
    }
}
