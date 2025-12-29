using Microsoft.AspNetCore.Components;

namespace TradingStrat.Web.Components.Shared;

/// <summary>
/// Strategy card component for displaying custom/user-created trading strategies.
/// Used in the Strategy Library page to show custom strategies with full management options.
/// </summary>
public partial class CustomStrategyCard : ComponentBase
{
    /// <summary>
    /// Strategy name
    /// </summary>
    [Parameter, EditorRequired]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Strategy description (optional)
    /// </summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>
    /// Strategy category (e.g., "Momentum", "Custom")
    /// </summary>
    [Parameter, EditorRequired]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Strategy author/creator
    /// </summary>
    [Parameter, EditorRequired]
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    [Parameter, EditorRequired]
    public DateTime LastUpdatedAt { get; set; }

    /// <summary>
    /// Number of entry rules defined
    /// </summary>
    [Parameter, EditorRequired]
    public int EntryRulesCount { get; set; }

    /// <summary>
    /// Number of exit rules defined
    /// </summary>
    [Parameter, EditorRequired]
    public int ExitRulesCount { get; set; }

    /// <summary>
    /// Number of times this strategy has been used
    /// </summary>
    [Parameter]
    public int TimesUsed { get; set; }

    /// <summary>
    /// Last backtest return percentage (optional)
    /// </summary>
    [Parameter]
    public decimal? LastBacktestReturn { get; set; }

    /// <summary>
    /// Callback invoked when Edit button is clicked
    /// </summary>
    [Parameter]
    public EventCallback OnEdit { get; set; }

    /// <summary>
    /// Callback invoked when Clone button is clicked
    /// </summary>
    [Parameter]
    public EventCallback OnClone { get; set; }

    /// <summary>
    /// Callback invoked when Optimize button is clicked
    /// </summary>
    [Parameter]
    public EventCallback OnOptimize { get; set; }

    /// <summary>
    /// Callback invoked when Test button is clicked
    /// </summary>
    [Parameter]
    public EventCallback OnTest { get; set; }

    /// <summary>
    /// Callback invoked when Delete button is clicked
    /// </summary>
    [Parameter]
    public EventCallback OnDelete { get; set; }
}
