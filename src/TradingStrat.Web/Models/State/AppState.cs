namespace TradingStrat.Web.Models.State;

public class AppState
{
    public string? CurrentTicker { get; set; }
    public string? CurrentStrategyType { get; set; }
    public Dictionary<string, object> CurrentStrategyParameters { get; set; } = new();
    public NavigationState NavigationState { get; set; } = new();
}

public class NavigationState
{
    public string? LastVisitedPage { get; set; }
    public DateTime LastNavigationTime { get; set; }
}
