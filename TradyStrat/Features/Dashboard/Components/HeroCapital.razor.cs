using System.Globalization;
using Microsoft.AspNetCore.Components;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Dashboard.Components;

public partial class HeroCapital : ComponentBase
{
    [Parameter, EditorRequired] public PortfolioSnapshot Snap { get; set; } = null!;
    [Parameter, EditorRequired] public GoalConfig Goal { get; set; } = null!;
    [Parameter, EditorRequired] public DateOnly Today { get; set; }

    private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

    private decimal Pct => Snap.ProgressPct;

    // Cost basis of currently-held lots = principal still in market.
    private decimal CostBasisEur => Snap.Shares * Snap.AvgCostEur;
    private decimal Goal100 => Goal.TargetEur <= 0m ? 1m : Goal.TargetEur;

    // All percentages are vs. goal so the bar adds up to ProgressPct.
    private decimal CostBasisPct  => Clamp01(CostBasisEur / Goal100 * 100m);
    private decimal RealizedPct   => Clamp01(Math.Max(0m, Snap.RealizedPnLEur) / Goal100 * 100m);
    private decimal UnrealizedAbsPct => Clamp01(Math.Abs(Snap.UnrealizedPnLEur) / Goal100 * 100m);
    private decimal CurrentPct    => Clamp01(Snap.CurrentValueEur / Goal100 * 100m);

    // When unrealized < 0, principal segment must NOT extend past current —
    // the "loss" hashed segment fills the gap from current back to cost.
    private decimal PrincipalShownPct =>
        Snap.UnrealizedPnLEur < 0m
            ? Math.Max(0m, CostBasisPct - UnrealizedAbsPct)
            : CostBasisPct;

    private static decimal Clamp01(decimal pct) => Math.Max(0m, Math.Min(100m, pct));
    private static string Fmt(decimal pct) => pct.ToString("F2", CultureInfo.InvariantCulture);

    private static string FormatSigned(decimal v)
        => $"{(v >= 0 ? "+€" : "−€")}{Math.Abs(v).ToString("N0", FrFr)}";

    private string AriaSummary =>
        $"Progress {Pct.ToString("F1", FrFr)}% — own capital €{CostBasisEur.ToString("N0", FrFr)}, " +
        $"unrealized {FormatSigned(Snap.UnrealizedPnLEur)}, realized {FormatSigned(Snap.RealizedPnLEur)}";

    private string? DaysLeft(DateOnly target)
    {
        var days = target.DayNumber - Today.DayNumber;
        if (days < 0) return "past due";
        if (days == 0) return "today";
        return $"{days.ToString("N0", FrFr)} days left";
    }
}
