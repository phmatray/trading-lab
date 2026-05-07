using System.Globalization;
using Microsoft.AspNetCore.Components;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Dashboard.Components;

public partial class PortfolioRail : ComponentBase
{
    [Parameter, EditorRequired] public PortfolioSnapshot Snap { get; set; } = null!;
    [Parameter, EditorRequired] public IReadOnlyList<TickerView> Tickers { get; set; } = null!;

    private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

    private static string PnL(decimal pnl, decimal value)
    {
        if (value == 0m) return "—";
        var pct = pnl / (value - pnl) * 100m;
        return $"{(pct >= 0 ? "+" : "")}{pct.ToString("F1", FrFr)} %";
    }

    private static string FormatPrimary(TickerView t) => t.Currency switch
    {
        "EUR" => $"€{t.Price.ToString("N2", FrFr)}",
        "USD" => $"${t.Price.ToString("N2", FrFr)}",
        _     => t.Price.ToString("N2", FrFr)
    };

    private static string FormatDelta(decimal pct)
        => $"{(pct >= 0 ? "+" : "")}{pct.ToString("F1", FrFr)}%";
}
