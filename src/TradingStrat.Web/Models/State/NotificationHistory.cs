namespace TradingStrat.Web.Models.State;

public class NotificationHistory
{
    public List<Notification> Notifications { get; set; } = new();
    public DateTime LastCleanup { get; set; } = DateTime.UtcNow;
    public int MaxHistoryItems { get; set; } = 100;
}
