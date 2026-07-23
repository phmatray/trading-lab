namespace TradingStrat.Infrastructure.Configuration;

/// <summary>
/// Configuration for the data refresh background service.
/// </summary>
public class DataRefreshConfiguration
{
    /// <summary>
    /// Whether the background refresh service is enabled.
    /// Default: false (disabled for safety).
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Hour of day (0-23) when refresh should run.
    /// Default: 6 (6:00 AM).
    /// </summary>
    public int ScheduleHour { get; set; } = 6;

    /// <summary>
    /// Number of hours before data is considered stale.
    /// Default: 24 hours for daily data.
    /// </summary>
    public int StaleThresholdHours { get; set; } = 24;

    /// <summary>
    /// Timeframes to refresh (e.g., "D1", "W1").
    /// Default: Daily and Weekly only.
    /// </summary>
    public List<string> TimeFrames { get; set; } = new() { "D1", "W1" };
}
