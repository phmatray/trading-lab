using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Dashboard.Components;

public partial class GrowthChart : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = null!;

    [Parameter, EditorRequired] public IReadOnlyList<GrowthPoint> Points { get; set; } = null!;
    [Parameter, EditorRequired] public GoalConfig Goal { get; set; } = null!;
    [Parameter, EditorRequired] public PortfolioSnapshot Portfolio { get; set; } = null!;
    [Parameter] public decimal? FocusTickerEur { get; set; }

    private ElementReference _svgRef;
    private IJSObjectReference? _module;

    private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

    private DateOnly? EndDate =>
        Goal.TargetDate is { } d && Points.Count > 0 && d > Points[0].Date ? d : null;

    private string LinePath => PathBuilder.Line(Points, 1200, 220, Goal.TargetEur, EndDate);
    private string AreaPath => PathBuilder.Area(Points, 1200, 220, Goal.TargetEur, EndDate);

    private string GoalLabel => Goal.TargetDate is { } td
        ? $"€{Goal.TargetEur.ToString("N0", FrFr)} by {td.ToString("MMM yyyy", CultureInfo.InvariantCulture)} — goal"
        : $"€{Goal.TargetEur.ToString("N0", FrFr)} — goal";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        try
        {
            _module = await JS.InvokeAsync<IJSObjectReference>("import", "./js/growth-chart.js");
            await _module.InvokeVoidAsync("init", _svgRef, BuildPayload(), "fr-FR");
        }
        catch (JSException) { /* graceful degradation: chart still renders without crosshair */ }
    }

    private object BuildPayload() => new
    {
        dates          = Points.Select(g => g.Date.ToString("o", CultureInfo.InvariantCulture)).ToArray(),
        capital        = Points.Select(g => (double)g.ValueEur).ToArray(),
        position       = Points.Select(_ => (double)Portfolio.Shares).ToArray(),
        focusTickerEur = Points.Select(_ => (double)(FocusTickerEur ?? 0m)).ToArray(),
        targetEur      = (double)Goal.TargetEur,
        targetDate     = Goal.TargetDate?.ToString("o", CultureInfo.InvariantCulture),
    };

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        if (_module is null) return;
        try
        {
            await _module.InvokeVoidAsync("dispose", _svgRef);
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
    }
}
