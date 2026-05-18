using TradyStrat.Application.Dashboard;
using Microsoft.AspNetCore.Components;
using TradyStrat.Domain;
using TradyStrat.Application.Formatting;

namespace TradyStrat.Features.Dashboard.Components;

public partial class PortfolioRail : ComponentBase
{
    [Parameter, EditorRequired] public PortfolioSnapshot Snap { get; set; } = null!;
    [Parameter, EditorRequired] public IReadOnlyList<TickerView> Tickers { get; set; } = null!;

    private HashSet<string>? _heldTickers;

    protected override void OnParametersSet()
        => _heldTickers = Snap.Positions.Select(p => p.Ticker).ToHashSet();

    private bool IsHeld(TickerView t) => _heldTickers!.Contains(t.Ticker);

    private static string PnL(decimal pnl, decimal value)
    {
        if (value == 0m) return "—";
        var pct = pnl / (value - pnl) * 100m;
        var sign = pct < 0m ? "−" : "+";
        return sign + NumberFormat.Pct(Math.Abs(pct));
    }

    private static string FormatPrimary(TickerView t) => t.Currency switch
    {
        "EUR" => NumberFormat.Price(t.Price, "€"),
        "USD" => NumberFormat.Price(t.Price, "$"),
        _     => NumberFormat.Price(t.Price, ""),
    };

    // Day-move chip displays an absolute-value percent; the ↑ / ↓ glyph in
    // the markup conveys direction and the .delta.dn class conveys colour.
    private static string FormatDelta(decimal pct) => NumberFormat.Pct(Math.Abs(pct));

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
