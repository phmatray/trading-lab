namespace TradingStrat.Web.Models.State;

/// <summary>
/// State model for selected portfolio tracking.
/// </summary>
public class PortfolioState
{
    /// <summary>
    /// Gets or sets the selected portfolio ID, or null if no portfolio is selected.
    /// </summary>
    public int? SelectedPortfolioId { get; set; }
}
