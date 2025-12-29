namespace TradingStrat.Web.Models.State;

/// <summary>
/// State model for selected ticker tracking in AI Analysis panel.
/// </summary>
public class TickerSelectionState
{
    /// <summary>
    /// Gets or sets the selected ticker symbol for analysis, or null if no ticker is selected.
    /// </summary>
    public string? SelectedTicker { get; set; }
}
