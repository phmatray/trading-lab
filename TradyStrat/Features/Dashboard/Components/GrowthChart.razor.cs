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

    /// <summary>Y-axis labels keyed by their viewBox y coordinate.</summary>
    /// <remarks>
    /// Anchored at three of the four interior grid-line y values (40, 100, 160).
    /// The values shown are 75 / 50 / 25 % of <see cref="GoalConfig.TargetEur"/>;
    /// the bottom (€0) is implied by the axis baseline and the top is already
    /// anchored by the existing "€X — goal" callout, so we don't double-label
    /// either end.
    /// </remarks>
    private IReadOnlyList<(double Y, string Text)> YAxisLabels
    {
        get
        {
            var t = Goal.TargetEur;
            return
            [
                (40,  $"€{(t * 0.75m).ToString("N0", FrFr)}"),
                (100, $"€{(t * 0.50m).ToString("N0", FrFr)}"),
                (160, $"€{(t * 0.25m).ToString("N0", FrFr)}"),
            ];
        }
    }

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

    private object BuildPayload()
    {
        // The visible X-axis spans first-trade-date → axisEnd (goal date if set,
        // otherwise last data date). Data arrays only span first-trade → today.
        // The JS uses these explicit bounds for hover-date math.
        var axisStart = Points.Count > 0 ? Points[0].Date : DateOnly.FromDateTime(DateTime.UtcNow);
        var axisEnd   = EndDate ?? (Points.Count > 0 ? Points[^1].Date : axisStart);

        return new
        {
            dates          = Points.Select(g => g.Date.ToString("o", CultureInfo.InvariantCulture)).ToArray(),
            capital        = Points.Select(g => (double)g.ValueEur).ToArray(),
            position       = Points.Select(_ => (double)Portfolio.Shares).ToArray(),
            focusTickerEur = Points.Select(_ => (double)(FocusTickerEur ?? 0m)).ToArray(),
            targetEur      = (double)Goal.TargetEur,
            targetDate     = Goal.TargetDate?.ToString("o", CultureInfo.InvariantCulture),
            axisStartDate  = axisStart.ToString("o", CultureInfo.InvariantCulture),
            axisEndDate    = axisEnd.ToString("o", CultureInfo.InvariantCulture),
        };
    }

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
