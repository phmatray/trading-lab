using System.Globalization;
using Microsoft.AspNetCore.Components;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Dashboard.Components;

public partial class GrowthChart : ComponentBase
{
    [Parameter, EditorRequired] public IReadOnlyList<GrowthPoint> Points { get; set; } = null!;
    [Parameter, EditorRequired] public GoalConfig Goal { get; set; } = null!;

    private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

    private DateOnly? EndDate =>
        Goal.TargetDate is { } d && Points.Count > 0 && d > Points[0].Date ? d : null;

    private string LinePath => PathBuilder.Line(Points, 1200, 220, Goal.TargetEur, EndDate);
    private string AreaPath => PathBuilder.Area(Points, 1200, 220, Goal.TargetEur, EndDate);

    private string GoalLabel => Goal.TargetDate is { } td
        ? $"€{Goal.TargetEur.ToString("N0", FrFr)} by {td.ToString("MMM yyyy", CultureInfo.InvariantCulture)} — goal"
        : $"€{Goal.TargetEur.ToString("N0", FrFr)} — goal";
}
