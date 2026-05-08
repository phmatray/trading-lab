using System.Globalization;
using Microsoft.AspNetCore.Components;
using TradyStrat.Common.Domain;

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

    private static string TruncateRationale(string rationale, int maxChars)
    {
        if (string.IsNullOrEmpty(rationale)) return "";
        if (rationale.Length <= maxChars) return rationale;
        var slice = rationale[..maxChars];
        var lastDot = slice.LastIndexOfAny(['.', '!', '?']);
        return lastDot > maxChars / 2
            ? slice[..(lastDot + 1)]
            : slice + "…";
    }
}
