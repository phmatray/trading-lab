using Microsoft.AspNetCore.Components;
using TradingStrat.Web.Components.UI;

namespace TradingStrat.Web.Components.Shared;

public partial class Dialog : ComponentBase, IAsyncDisposable
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

    protected DialogSize MapSize() => Width switch
    {
        "w-96" => DialogSize.Large,
        "max-w-md" or "w-[500px]" => DialogSize.Medium,
        "max-w-lg" => DialogSize.Large,
        "max-w-xl" => DialogSize.XL,
        "max-w-2xl" => DialogSize.XL2,
        "max-w-3xl" => DialogSize.XL3,
        "max-w-4xl" => DialogSize.XL4,
        "max-w-5xl" => DialogSize.XL5,
        _ => DialogSize.Large
    };

    protected async Task HandleIsOpenChanged(bool newValue)
    {
        // Only trigger OnClose when dialog is being closed (newValue = false)
        if (!newValue && CloseOnBackdropClick && OnClose.HasDelegate)
        {
            await OnClose.InvokeAsync();
        }
    }

    protected async Task HandleClose()
    {
        if (OnClose.HasDelegate)
        {
            await OnClose.InvokeAsync();
        }
    }

    protected async Task HandleBackdropClick()
    {
        if (CloseOnBackdropClick && OnClose.HasDelegate)
        {
            await OnClose.InvokeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        // UI.Dialog handles its own disposal
        await Task.CompletedTask;
    }
}
