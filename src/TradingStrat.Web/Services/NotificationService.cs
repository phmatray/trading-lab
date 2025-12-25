using TradingStrat.Domain.ValueObjects;
using TradingStrat.Web.Models;
using TradingStrat.Web.Models.State;

namespace TradingStrat.Web.Services;

public class NotificationService : IDisposable
{
    private const string STORAGE_KEY = "tradingstrat_notifications";
    private const int MAX_HISTORY_ITEMS = 100;
    private const int CLEANUP_DAYS = 7;

    private readonly LocalStorageService _localStorage;
    private NotificationHistory? _cachedHistory;
    private bool _disposed = false;

    public event Action<Notification>? OnNotificationAdded;
    public event Action? OnNotificationsChanged;
    public event Action<int>? OnUnreadCountChanged;

    public NotificationService(LocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public virtual async Task<Notification> AddNotificationAsync(
        NotificationType type,
        NotificationSeverity severity,
        string title,
        string message,
        string? ticker = null,
        NotificationAction? action = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            Type = type,
            Severity = severity,
            Title = title,
            Message = message,
            RelatedTicker = ticker,
            Action = action,
            Metadata = metadata,
            Icon = GetIconForType(type, severity)
        };

        var history = await GetHistoryAsync(cancellationToken);
        history.Notifications.Insert(0, notification);

        await CleanupIfNeededAsync(history, cancellationToken);
        await SaveHistoryAsync(history, cancellationToken);

        OnNotificationAdded?.Invoke(notification);
        OnNotificationsChanged?.Invoke();

        int unreadCount = history.Notifications.Count(n => !n.IsRead);
        OnUnreadCountChanged?.Invoke(unreadCount);

        return notification;
    }

    public virtual async Task<List<Notification>> GetNotificationsAsync(
        bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
        var history = await GetHistoryAsync(cancellationToken);
        var notifications = history.Notifications;

        return unreadOnly
            ? notifications.Where(n => !n.IsRead).ToList()
            : notifications;
    }

    public virtual async Task MarkAsReadAsync(
        string notificationId,
        CancellationToken cancellationToken = default)
    {
        var history = await GetHistoryAsync(cancellationToken);
        var notification = history.Notifications.FirstOrDefault(n => n.Id == notificationId);

        if (notification != null)
        {
            notification.IsRead = true;
            await SaveHistoryAsync(history, cancellationToken);

            OnNotificationsChanged?.Invoke();

            int unreadCount = history.Notifications.Count(n => !n.IsRead);
            OnUnreadCountChanged?.Invoke(unreadCount);
        }
    }

    public virtual async Task MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        var history = await GetHistoryAsync(cancellationToken);

        foreach (var notification in history.Notifications)
        {
            notification.IsRead = true;
        }

        await SaveHistoryAsync(history, cancellationToken);

        OnNotificationsChanged?.Invoke();
        OnUnreadCountChanged?.Invoke(0);
    }

    public virtual async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        var history = new NotificationHistory();
        await SaveHistoryAsync(history, cancellationToken);

        OnNotificationsChanged?.Invoke();
        OnUnreadCountChanged?.Invoke(0);
    }

    public virtual async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        var history = await GetHistoryAsync(cancellationToken);
        return history.Notifications.Count(n => !n.IsRead);
    }

    public static Notification CreateSignalNotification(
        string ticker,
        SignalType signal,
        decimal price,
        float confidence)
    {
        var severity = signal == SignalType.Buy
            ? NotificationSeverity.Success
            : NotificationSeverity.Warning;

        return new Notification
        {
            Type = NotificationType.Signal,
            Severity = severity,
            Title = $"{signal} Signal: {ticker}",
            Message = $"Price: ${price:F2} | Confidence: {confidence:F1}%",
            RelatedTicker = ticker,
            Icon = signal == SignalType.Buy ? "trend-up" : "trend-down",
            Metadata = new Dictionary<string, object>
            {
                ["signal"] = signal.ToString(),
                ["price"] = price,
                ["confidence"] = confidence
            }
        };
    }

    public static Notification CreateBacktestNotification(
        string ticker,
        string strategyName,
        int tradeCount,
        decimal totalReturn)
    {
        var severity = totalReturn >= 0
            ? NotificationSeverity.Success
            : NotificationSeverity.Warning;

        return new Notification
        {
            Type = NotificationType.Backtest,
            Severity = severity,
            Title = "Backtest Complete",
            Message = $"{strategyName} | {tradeCount} trades | {totalReturn:+0.0;-0.0;0.0}% return",
            RelatedTicker = ticker,
            Icon = "chart-bar",
            Action = new NotificationAction
            {
                Label = "View Results",
                TargetPage = "/backtest"
            },
            Metadata = new Dictionary<string, object>
            {
                ["strategyName"] = strategyName,
                ["tradeCount"] = tradeCount,
                ["totalReturn"] = totalReturn
            }
        };
    }

    public static Notification CreateRecommendationNotification(
        string ticker,
        string recommendation,
        int confidenceScore)
    {
        var severity = confidenceScore >= 70
            ? NotificationSeverity.Info
            : NotificationSeverity.Warning;

        return new Notification
        {
            Type = NotificationType.Recommendation,
            Severity = severity,
            Title = "Strategy Recommendation",
            Message = recommendation,
            RelatedTicker = ticker,
            Icon = "light-bulb",
            Metadata = new Dictionary<string, object>
            {
                ["confidenceScore"] = confidenceScore
            }
        };
    }

    public static Notification CreateDataFreshnessNotification(
        string ticker,
        int daysOld)
    {
        var severity = daysOld > 7
            ? NotificationSeverity.Warning
            : NotificationSeverity.Info;

        return new Notification
        {
            Type = NotificationType.DataFreshness,
            Severity = severity,
            Title = "Data Refresh Recommended",
            Message = $"Data for {ticker} is {daysOld} day{(daysOld == 1 ? "" : "s")} old",
            RelatedTicker = ticker,
            Icon = "exclamation-triangle",
            Action = new NotificationAction
            {
                Label = "Refresh Now",
                TargetPage = "/data",
                Parameters = new Dictionary<string, object> { ["ticker"] = ticker }
            },
            Metadata = new Dictionary<string, object>
            {
                ["daysOld"] = daysOld
            }
        };
    }

    private async Task<NotificationHistory> GetHistoryAsync(CancellationToken cancellationToken)
    {
        if (_cachedHistory != null)
        {
            return _cachedHistory;
        }

        _cachedHistory = await _localStorage.GetItemAsync<NotificationHistory>(STORAGE_KEY, cancellationToken)
            ?? new NotificationHistory();

        return _cachedHistory;
    }

    private async Task SaveHistoryAsync(NotificationHistory history, CancellationToken cancellationToken)
    {
        _cachedHistory = history;
        await _localStorage.SetItemAsync(STORAGE_KEY, history, cancellationToken);
    }

    private Task CleanupIfNeededAsync(NotificationHistory history, CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;
        double daysSinceLastCleanup = (now - history.LastCleanup).TotalDays;

        if (daysSinceLastCleanup < 1)
        {
            return Task.CompletedTask;
        }

        var cutoffDate = now.AddDays(-CLEANUP_DAYS);
        history.Notifications = history.Notifications
            .Where(n => n.Timestamp > cutoffDate)
            .Take(MAX_HISTORY_ITEMS)
            .ToList();

        history.LastCleanup = now;
        return Task.CompletedTask;
    }

    private static string GetIconForType(NotificationType type, NotificationSeverity severity)
    {
        return type switch
        {
            NotificationType.Signal => severity == NotificationSeverity.Success ? "trend-up" : "trend-down",
            NotificationType.Backtest => "chart-bar",
            NotificationType.Recommendation => "light-bulb",
            NotificationType.DataFreshness => "exclamation-triangle",
            NotificationType.System => "information-circle",
            _ => "bell"
        };
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        OnNotificationAdded = null;
        OnNotificationsChanged = null;
        OnUnreadCountChanged = null;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
