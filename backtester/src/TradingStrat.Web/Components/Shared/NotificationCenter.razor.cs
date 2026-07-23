using Microsoft.AspNetCore.Components;
using TradingStrat.Web.Models;
using TradingStrat.Web.Services;

namespace TradingStrat.Web.Components.Shared;

public partial class NotificationCenter : ComponentBase, IDisposable
{
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    private bool _isOpen;
    private List<Notification> _notifications = new();
    private bool _hasLoadedNotifications;

    protected override void OnInitialized()
    {
        NotificationService.OnNotificationsChanged += HandleNotificationsChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_hasLoadedNotifications)
        {
            _hasLoadedNotifications = true;
            await LoadNotificationsAsync();
        }
    }

    protected override void OnParametersSet()
    {
        if (_isOpen != IsOpen)
        {
            _isOpen = IsOpen;
            if (_isOpen && _hasLoadedNotifications)
            {
                _ = LoadNotificationsAsync();
            }
        }
    }

    private async Task LoadNotificationsAsync()
    {
        _notifications = await NotificationService.GetNotificationsAsync();
        StateHasChanged();
    }

    private void HandleNotificationsChanged()
    {
        InvokeAsync(LoadNotificationsAsync);
    }

    private async Task HandleNotificationClick(Notification notification)
    {
        if (!notification.IsRead)
        {
            await NotificationService.MarkAsReadAsync(notification.Id);
        }

        if (notification.Action is not null)
        {
            Navigation.NavigateTo(notification.Action.TargetPage);
            await Close();
        }
    }

    private async Task MarkAllAsRead()
    {
        await NotificationService.MarkAllAsReadAsync();
        await LoadNotificationsAsync();
    }

    private async Task ClearAll()
    {
        await NotificationService.ClearAllAsync();
        await LoadNotificationsAsync();
    }

    private async Task Close()
    {
        _isOpen = false;
        await IsOpenChanged.InvokeAsync(false);
    }

    private Dictionary<string, List<Notification>> GetGroupedNotifications()
    {
        DateTime now = DateTime.UtcNow.Date;
        DateTime yesterday = now.AddDays(-1);

        return _notifications
            .GroupBy(n => n.Timestamp.Date >= now ? "Today" :
                         n.Timestamp.Date >= yesterday ? "Yesterday" :
                         "Older")
            .OrderBy(g => g.Key == "Today" ? 0 : g.Key == "Yesterday" ? 1 : 2)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private string GetNotificationItemClasses(Notification notification)
    {
        string baseClasses = "border-l-4 transition-colors";
        if (!notification.IsRead)
        {
            return $"{baseClasses} border-l-trading-blue dark:border-l-dark-accent-blue bg-blue-50 dark:bg-blue-900/10";
        }
        return $"{baseClasses} border-l-transparent bg-white dark:bg-dark-card";
    }

    private string GetNotificationIconClasses(Notification notification)
    {
        return notification.Severity switch
        {
            NotificationSeverity.Success => "w-8 h-8 rounded-full bg-green-100 dark:bg-green-900/20 text-green-600 dark:text-green-400 flex items-center justify-center",
            NotificationSeverity.Warning => "w-8 h-8 rounded-full bg-yellow-100 dark:bg-yellow-900/20 text-yellow-600 dark:text-yellow-400 flex items-center justify-center",
            NotificationSeverity.Error => "w-8 h-8 rounded-full bg-red-100 dark:bg-red-900/20 text-red-600 dark:text-red-400 flex items-center justify-center",
            _ => "w-8 h-8 rounded-full bg-blue-100 dark:bg-blue-900/20 text-blue-600 dark:text-blue-400 flex items-center justify-center"
        };
    }

    private RenderFragment GetNotificationIcon(Notification notification) => builder =>
    {
        string svgPath = notification.Severity switch
        {
            NotificationSeverity.Success => "M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z",
            NotificationSeverity.Warning => "M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z",
            NotificationSeverity.Error => "M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z",
            _ => "M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
        };

        builder.OpenElement(0, "svg");
        builder.AddAttribute(1, "class", "w-5 h-5");
        builder.AddAttribute(2, "fill", "none");
        builder.AddAttribute(3, "viewBox", "0 0 24 24");
        builder.AddAttribute(4, "stroke", "currentColor");

        builder.OpenElement(5, "path");
        builder.AddAttribute(6, "stroke-linecap", "round");
        builder.AddAttribute(7, "stroke-linejoin", "round");
        builder.AddAttribute(8, "stroke-width", "2");
        builder.AddAttribute(9, "d", svgPath);
        builder.CloseElement();

        builder.CloseElement();
    };

    private string GetRelativeTime(DateTime timestamp)
    {
        DateTime now = DateTime.UtcNow;
        TimeSpan diff = now - timestamp;

        if (diff.TotalMinutes < 1)
        {
            return "Just now";
        }
        if (diff.TotalMinutes < 60)
        {
            return $"{(int)diff.TotalMinutes} min ago";
        }
        if (diff.TotalHours < 24)
        {
            return $"{(int)diff.TotalHours}h ago";
        }
        if (diff.TotalDays < 7)
        {
            return $"{(int)diff.TotalDays}d ago";
        }

        return timestamp.ToLocalTime().ToString("MMM d");
    }

    private string PanelClasses =>
        "fixed md:absolute md:right-0 md:top-12 " +
        "inset-x-0 bottom-0 md:inset-auto " +
        "w-full md:w-96 " +
        "bg-white dark:bg-dark-card " +
        "border border-gray-200 dark:border-dark-border " +
        "md:rounded-lg md:shadow-2xl " +
        "z-50 flex flex-col " +
        "h-3/4 md:h-auto";

    public void Dispose()
    {
        NotificationService.OnNotificationsChanged -= HandleNotificationsChanged;
    }
}
