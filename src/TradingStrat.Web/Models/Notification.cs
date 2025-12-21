namespace TradingStrat.Web.Models;

public class Notification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public NotificationType Type { get; set; }
    public NotificationSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
    public string? Icon { get; set; }
    public string? RelatedTicker { get; set; }
    public NotificationAction? Action { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public enum NotificationType
{
    Signal,          // Buy/Sell signals from Live Analysis
    Backtest,        // Backtest completion
    Recommendation,  // Strategy recommendations
    DataFreshness,   // Data staleness warnings
    System          // General system notifications
}

public enum NotificationSeverity
{
    Info,    // Blue - informational
    Success, // Green - positive signals/results
    Warning, // Orange - data freshness, caution
    Error    // Red - errors/failures
}

public class NotificationAction
{
    public string Label { get; set; } = string.Empty;
    public string TargetPage { get; set; } = string.Empty;
    public Dictionary<string, object>? Parameters { get; set; }
}
