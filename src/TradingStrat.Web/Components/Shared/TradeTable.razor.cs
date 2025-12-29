using Microsoft.AspNetCore.Components;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Web.Components.Shared;

public partial class TradeTable : ComponentBase
{
    [Parameter]
    public List<Trade>? Trades { get; set; }

    [Parameter]
    public int MaxDisplayTrades { get; set; } = 20;

    private bool ShowAll { get; set; }

    private IEnumerable<Trade> DisplayedTrades =>
        ShowAll ? (Trades ?? new List<Trade>()) : (Trades?.Take(MaxDisplayTrades) ?? new List<Trade>());

    private void ToggleShowAll()
    {
        ShowAll = !ShowAll;
    }

    private string GetProfitLossClass(Trade trade)
    {
        string baseClasses = "text-sm text-right whitespace-nowrap";
        string colorClass = trade.ProfitLoss > 0 ? "metric-positive" : "metric-negative";
        return $"{baseClasses} {colorClass}";
    }
}
