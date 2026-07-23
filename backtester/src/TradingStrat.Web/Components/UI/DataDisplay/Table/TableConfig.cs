namespace TradingStrat.Web.Components.UI.DataDisplay.Table;

/// <summary>
/// Configuration for table styling passed via CascadingValue.
/// </summary>
public sealed record TableConfig(
    bool Bleed,
    bool Dense,
    bool Grid,
    bool Striped
);
