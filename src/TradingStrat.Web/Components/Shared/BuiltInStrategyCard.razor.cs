using Microsoft.AspNetCore.Components;

namespace TradingStrat.Web.Components.Shared;

/// <summary>
/// Strategy card component for displaying built-in trading strategies.
/// Used in the Strategy Library page to show available strategies.
/// </summary>
public partial class BuiltInStrategyCard : ComponentBase
{
    /// <summary>
    /// Strategy name
    /// </summary>
    [Parameter, EditorRequired]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Strategy description
    /// </summary>
    [Parameter, EditorRequired]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Strategy category (e.g., "Momentum", "Trend Following")
    /// </summary>
    [Parameter, EditorRequired]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Strategy key/identifier used for backtesting
    /// </summary>
    [Parameter, EditorRequired]
    public string StrategyKey { get; set; } = string.Empty;

    /// <summary>
    /// Default parameter values for the strategy
    /// </summary>
    [Parameter]
    public Dictionary<string, object> DefaultParameters { get; set; } = new();

    /// <summary>
    /// Callback invoked when the "Run Backtest" button is clicked
    /// </summary>
    [Parameter]
    public EventCallback OnRunBacktest { get; set; }
}
