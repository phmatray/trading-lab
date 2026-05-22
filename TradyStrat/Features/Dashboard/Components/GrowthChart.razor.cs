using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TradyStrat.Domain;
using TradyStrat.Domain.Portfolio;

namespace TradyStrat.Features.Dashboard.Components;

public partial class GrowthChart : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = null!;

    [Parameter, EditorRequired] public IReadOnlyList<GrowthPoint> Points { get; set; } = null!;
    [Parameter, EditorRequired] public Goal Goal { get; set; } = null!;
    [Parameter, EditorRequired] public PortfolioSnapshot Portfolio { get; set; } = null!;
    [Parameter] public decimal? FocusTickerEur { get; set; }
    [Parameter] public IReadOnlyList<CapitalEvent> Events { get; set; } = [];

    private ElementReference _svgRef;
    private IJSObjectReference? _module;

    /// <summary>
    /// Per-instance DOM id prefix. JS resolves all chart sub-elements via these
    /// ids — using a unique prefix per component instance keeps the page safe
    /// if a chart is ever rendered twice (defensive, not a current scenario).
    /// </summary>
    private readonly string _id = $"gc-{Guid.NewGuid():N}";

    private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

    private DateOnly? EndDate =>
        Goal.HasDeadline && Points.Count > 0 && Goal.TargetDate > Points[0].Date ? Goal.TargetDate : null;

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
        // Visible X-axis spans first-trade-date → axisEnd (goal date if set,
        // otherwise last data date). Data arrays only span first-trade → today.
        var axisStart = Points.Count > 0 ? Points[0].Date : DateOnly.FromDateTime(DateTime.UtcNow);
        var axisEnd   = EndDate ?? (Points.Count > 0 ? Points[^1].Date : axisStart);

        // For the required-CAGR plan curve we need a *positive* starting capital;
        // the GrowthSeriesBuilder injects a synthetic 0 at firstDate-1 to draw the
        // rise from baseline. Find the first strictly-positive point to use as V0
        // for the compound-growth formula. Falls back to 1 if everything is zero.
        decimal startCapital = 1m;
        DateOnly startDate = axisStart;
        foreach (var p in Points)
        {
            if (p.Value.Amount > 0m) { startCapital = p.Value.Amount; startDate = p.Date; break; }
        }

        return new
        {
            id              = _id,
            dates           = Points.Select(g => g.Date.ToString("o", CultureInfo.InvariantCulture)).ToArray(),
            capital         = Points.Select(g => (double)g.Value.Amount).ToArray(),
            position        = Points.Select(_ => (double)Portfolio.Shares).ToArray(),
            focusTickerEur  = Points.Select(_ => (double)(FocusTickerEur ?? 0m)).ToArray(),
            targetEur       = (double)Goal.Target.Amount,
            targetDate      = Goal.HasDeadline ? Goal.TargetDate.ToString("o", CultureInfo.InvariantCulture) : null,
            axisStartDate   = axisStart.ToString("o", CultureInfo.InvariantCulture),
            axisEndDate     = axisEnd.ToString("o", CultureInfo.InvariantCulture),
            startCapitalEur = (double)startCapital,
            startDate       = startDate.ToString("o", CultureInfo.InvariantCulture),
            goalLabel       = BuildGoalLabel(),
            yLabel75        = $"€{(Goal.Target.Amount * 0.75m).ToString("N0", FrFr)}",
            yLabel50        = $"€{(Goal.Target.Amount * 0.50m).ToString("N0", FrFr)}",
            yLabel25        = $"€{(Goal.Target.Amount * 0.25m).ToString("N0", FrFr)}",
            events          = Events.Select(e => new
            {
                date    = e.Date.ToString("o", CultureInfo.InvariantCulture),
                romanId = e.RomanId.Value,
            }).ToArray(),
        };
    }

    private string BuildGoalLabel() => Goal.HasDeadline
        ? $"€{Goal.Target.Amount.ToString("N0", FrFr)} by {Goal.TargetDate.ToString("MMM yyyy", CultureInfo.InvariantCulture)} — goal"
        : $"€{Goal.Target.Amount.ToString("N0", FrFr)} — goal";

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
