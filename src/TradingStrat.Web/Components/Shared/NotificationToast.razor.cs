using Microsoft.AspNetCore.Components;
using TradingStrat.Web.Models;

namespace TradingStrat.Web.Components.Shared;

public partial class NotificationToast : ComponentBase, IDisposable
{
    [Parameter] public Notification? Notification { get; set; }
    [Parameter] public EventCallback OnDismiss { get; set; }
    [Parameter] public int AutoDismissMs { get; set; } = 5000;

    private Timer? _autoDismissTimer;

    protected override void OnParametersSet()
    {
        if (Notification != null && AutoDismissMs > 0)
        {
            _autoDismissTimer?.Dispose();
            _autoDismissTimer = new Timer(_ => InvokeAsync(HandleDismiss), null, AutoDismissMs, Timeout.Infinite);
        }
    }

    private async Task HandleDismiss()
    {
        _autoDismissTimer?.Dispose();
        await OnDismiss.InvokeAsync();
    }

    private async Task HandleActionClick()
    {
        if (Notification?.Action != null)
        {
            _autoDismissTimer?.Dispose();
            await OnDismiss.InvokeAsync();
            // Navigation will be handled by the parent component
        }
    }

    private string ContainerClasses => Notification?.Severity switch
    {
        NotificationSeverity.Success => "bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 text-green-900 dark:text-green-100 rounded-lg shadow-lg p-4 w-96 max-w-[calc(100vw-2rem)] animate-slide-in",
        NotificationSeverity.Warning => "bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 text-yellow-900 dark:text-yellow-100 rounded-lg shadow-lg p-4 w-96 max-w-[calc(100vw-2rem)] animate-slide-in",
        NotificationSeverity.Error => "bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-900 dark:text-red-100 rounded-lg shadow-lg p-4 w-96 max-w-[calc(100vw-2rem)] animate-slide-in",
        _ => "bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 text-blue-900 dark:text-blue-100 rounded-lg shadow-lg p-4 w-96 max-w-[calc(100vw-2rem)] animate-slide-in"
    };

    private string IconClasses => Notification?.Severity switch
    {
        NotificationSeverity.Success => "w-6 h-6 text-green-600 dark:text-green-400",
        NotificationSeverity.Warning => "w-6 h-6 text-yellow-600 dark:text-yellow-400",
        NotificationSeverity.Error => "w-6 h-6 text-red-600 dark:text-red-400",
        _ => "w-6 h-6 text-blue-600 dark:text-blue-400"
    };

    private string TextClasses => Notification?.Severity switch
    {
        NotificationSeverity.Success => "text-green-900 dark:text-green-100",
        NotificationSeverity.Warning => "text-yellow-900 dark:text-yellow-100",
        NotificationSeverity.Error => "text-red-900 dark:text-red-100",
        _ => "text-blue-900 dark:text-blue-100"
    };

    private string CloseButtonClasses => Notification?.Severity switch
    {
        NotificationSeverity.Success => "text-green-600 dark:text-green-400 hover:bg-green-100 dark:hover:bg-green-800 focus:ring-green-600",
        NotificationSeverity.Warning => "text-yellow-600 dark:text-yellow-400 hover:bg-yellow-100 dark:hover:bg-yellow-800 focus:ring-yellow-600",
        NotificationSeverity.Error => "text-red-600 dark:text-red-400 hover:bg-red-100 dark:hover:bg-red-800 focus:ring-red-600",
        _ => "text-blue-600 dark:text-blue-400 hover:bg-blue-100 dark:hover:bg-blue-800 focus:ring-blue-600"
    };

    public void Dispose()
    {
        _autoDismissTimer?.Dispose();
    }
}
