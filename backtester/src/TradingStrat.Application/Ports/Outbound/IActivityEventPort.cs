using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.Ports.Outbound;

/// <summary>
/// Outbound port for accessing activity events.
/// </summary>
public interface IActivityEventPort
{
    /// <summary>
    /// Records a new activity event.
    /// </summary>
    /// <param name="activityEvent">The activity event to record.</param>
    /// <returns>The saved activity event with assigned ID.</returns>
    Task<ActivityEvent> RecordActivityAsync(ActivityEvent activityEvent);

    /// <summary>
    /// Gets recent activity events.
    /// </summary>
    /// <param name="limit">Maximum number of events to return.</param>
    /// <param name="eventType">Optional event type filter.</param>
    /// <returns>List of activity events ordered by timestamp descending.</returns>
    Task<List<ActivityEvent>> GetRecentActivityAsync(int limit = 10, string? eventType = null);

    /// <summary>
    /// Gets the total count of activity events.
    /// </summary>
    /// <returns>Total number of activity events.</returns>
    Task<int> GetActivityCountAsync();
}
