namespace TradingStrat.Web.Models.State;

/// <summary>
/// State model for right panel configuration.
/// </summary>
public class RightPanelState
{
    /// <summary>
    /// Active tab identifier.
    /// </summary>
    public RightPanelTab ActiveTab { get; set; } = RightPanelTab.StrategyCopilot;

    /// <summary>
    /// Panel collapse state (true = collapsed to icon bar, false = expanded).
    /// </summary>
    public bool IsCollapsed { get; set; } = false;

    /// <summary>
    /// Last updated timestamp for state versioning.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Available right panel tabs.
/// </summary>
public enum RightPanelTab
{
    Notifications,
    StrategyCopilot
}
