namespace TradingStrat.Web.Components.UI.DataDisplay.Table;

/// <summary>
/// Configuration for table row link behavior passed via CascadingValue.
/// </summary>
public sealed record TableRowConfig(
    string? Href,
    string? Target,
    string? Title
);
