using Microsoft.AspNetCore.Components;

namespace TradingStrat.Web.Components.Shared;

public partial class ConfirmDialog : ComponentBase
{
    /// <summary>
    /// Controls the visibility of the dialog
    /// </summary>
    [Parameter]
    public bool IsOpen { get; set; }

    /// <summary>
    /// Dialog title
    /// </summary>
    [Parameter]
    public string Title { get; set; } = "Confirm Action";

    /// <summary>
    /// Message to display
    /// </summary>
    [Parameter]
    public string Message { get; set; } = "Are you sure you want to proceed?";

    /// <summary>
    /// Callback invoked when the user confirms
    /// </summary>
    [Parameter]
    public EventCallback OnConfirm { get; set; }

    /// <summary>
    /// Callback invoked when the user cancels
    /// </summary>
    [Parameter]
    public EventCallback OnCancel { get; set; }

    /// <summary>
    /// Text for the confirm button
    /// </summary>
    [Parameter]
    public string ConfirmText { get; set; } = "Confirm";

    /// <summary>
    /// Text for the cancel button
    /// </summary>
    [Parameter]
    public string CancelText { get; set; } = "Cancel";

    /// <summary>
    /// Text to display while processing
    /// </summary>
    [Parameter]
    public string ProcessingText { get; set; } = "Processing...";

    /// <summary>
    /// Type of confirmation (affects icon and button color)
    /// </summary>
    [Parameter]
    public ConfirmType Type { get; set; } = ConfirmType.Danger;

    /// <summary>
    /// Whether the action is currently being processed
    /// </summary>
    [Parameter]
    public bool IsProcessing { get; set; }

    /// <summary>
    /// Width of the dialog (Tailwind class)
    /// </summary>
    [Parameter]
    public string Width { get; set; } = "w-96";

    private async Task HandleConfirm()
    {
        if (OnConfirm.HasDelegate && !IsProcessing)
        {
            await OnConfirm.InvokeAsync();
        }
    }

    private async Task HandleCancel()
    {
        if (OnCancel.HasDelegate && !IsProcessing)
        {
            await OnCancel.InvokeAsync();
        }
    }

    private string GetIconColor()
    {
        return Type switch
        {
            ConfirmType.Danger => "text-red-600 dark:text-dark-danger",
            ConfirmType.Warning => "text-yellow-600 dark:text-yellow-500",
            ConfirmType.Info => "text-blue-600 dark:text-blue-500",
            _ => "text-red-600 dark:text-dark-danger"
        };
    }

    private string GetConfirmButtonClass()
    {
        return Type switch
        {
            ConfirmType.Danger => "bg-red-600 hover:bg-red-700 dark:bg-dark-danger dark:hover:bg-red-800",
            ConfirmType.Warning => "bg-yellow-600 hover:bg-yellow-700 dark:bg-yellow-500 dark:hover:bg-yellow-600",
            ConfirmType.Info => "bg-blue-600 hover:bg-blue-700 dark:bg-blue-500 dark:hover:bg-blue-600",
            _ => "bg-red-600 hover:bg-red-700 dark:bg-dark-danger dark:hover:bg-red-800"
        };
    }

    /// <summary>
    /// Type of confirmation dialog
    /// </summary>
    public enum ConfirmType
    {
        Info,
        Warning,
        Danger
    }
}
