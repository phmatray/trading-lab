using System.Reflection;
using TradingStrat.Web.Models;
using TradingStrat.Web.Services;

namespace TradingStrat.ComponentTests.TestDoubles;

/// <summary>
/// Fake implementation of NotificationService for testing.
/// Stores notifications in-memory instead of localStorage.
/// </summary>
public class FakeNotificationService : NotificationService
{
    private readonly List<Notification> _notifications = new();
    private bool _disposed = false;

    public FakeNotificationService() : base(new FakeLocalStorageService())
    {
    }

    public override async Task<Notification> AddNotificationAsync(
        NotificationType type,
        NotificationSeverity severity,
        string title,
        string message,
        string? ticker = null,
        NotificationAction? action = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken); // Simulate async

        Notification notification = new()
        {
            Type = type,
            Severity = severity,
            Title = title,
            Message = message,
            RelatedTicker = ticker,
            Action = action,
            Metadata = metadata
        };

        _notifications.Insert(0, notification);

        // Invoke the base class event using reflection
        RaiseBaseEvent("OnNotificationAdded", notification);
        RaiseBaseEvent("OnNotificationsChanged");

        int unreadCount = _notifications.Count(n => !n.IsRead);
        RaiseBaseEvent("OnUnreadCountChanged", unreadCount);

        return notification;
    }

    private void RaiseBaseEvent(string eventName, params object[] args)
    {
        // Get the base class event field via reflection
        FieldInfo? eventField = typeof(NotificationService).GetField(
            eventName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (eventField != null)
        {
            Delegate? eventDelegate = eventField.GetValue(this) as Delegate;
            eventDelegate?.DynamicInvoke(args);
        }
    }

    public override async Task<List<Notification>> GetNotificationsAsync(
        bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken); // Simulate async

        return unreadOnly
            ? _notifications.Where(n => !n.IsRead).ToList()
            : new List<Notification>(_notifications);
    }

    public override async Task MarkAsReadAsync(
        string notificationId,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken); // Simulate async

        Notification? notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
        if (notification != null)
        {
            notification.IsRead = true;

            RaiseBaseEvent("OnNotificationsChanged");

            int unreadCount = _notifications.Count(n => !n.IsRead);
            RaiseBaseEvent("OnUnreadCountChanged", unreadCount);
        }
    }

    public override async Task MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken); // Simulate async

        foreach (Notification notification in _notifications)
        {
            notification.IsRead = true;
        }

        RaiseBaseEvent("OnNotificationsChanged");
        RaiseBaseEvent("OnUnreadCountChanged", 0);
    }

    public override async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken); // Simulate async

        _notifications.Clear();

        RaiseBaseEvent("OnNotificationsChanged");
        RaiseBaseEvent("OnUnreadCountChanged", 0);
    }

    public override async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken); // Simulate async
        return _notifications.Count(n => !n.IsRead);
    }

    /// <summary>
    /// Clears all notifications. Useful for resetting state between tests.
    /// </summary>
    public void Reset()
    {
        _notifications.Clear();
        // Note: Cannot clear base class events as they're not shadowed
    }

    public new void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Reset();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
