using Microsoft.AspNetCore.Components;

namespace TradingStrat.Web.Components.Shared;

/// <summary>
/// Reusable statistics card for dashboard displays.
/// </summary>
public partial class DashboardStatsCard : ComponentBase
{
    #region Parameters

    /// <summary>
    /// Card title text.
    /// </summary>
    [Parameter]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Primary value to display.
    /// </summary>
    [Parameter]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Optional subtitle text.
    /// </summary>
    [Parameter]
    public string? Subtitle { get; set; }

    /// <summary>
    /// Icon type to display (layers, chart, wallet, database).
    /// </summary>
    [Parameter]
    public string Icon { get; set; } = "chart";

    /// <summary>
    /// Callback invoked when the card is clicked.
    /// </summary>
    [Parameter]
    public EventCallback OnClick { get; set; }

    #endregion

    #region Event Handlers

    private async Task HandleClick()
    {
        if (OnClick.HasDelegate)
        {
            await OnClick.InvokeAsync();
        }
    }

    #endregion
}
