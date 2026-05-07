using System.Globalization;
using Microsoft.AspNetCore.Components;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Features.Indicators;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Dashboard.Components;

public partial class TodaysCallCard : ComponentBase, IDisposable
{
    [Parameter, EditorRequired] public Suggestion Sug { get; set; } = null!;
    [Parameter] public DateOnly Today { get; set; }
    [Parameter] public string CallAsOfRelative { get; set; } = "";
    [Parameter] public EventCallback OnLogTrade { get; set; }
    [Parameter] public EventCallback OnRerun { get; set; }

    [Parameter, EditorRequired] public CallDiff CallDiff { get; set; } = CallDiff.None;

    [Parameter, EditorRequired]
    public IReadOnlyDictionary<(string Ticker, IndicatorKind Kind), IndicatorSeries> IndicatorHistories { get; set; }
        = new Dictionary<(string, IndicatorKind), IndicatorSeries>();

    [Parameter, EditorRequired] public BackfillStatus BackfillStatus { get; set; } = BackfillStatus.Idle.Instance;

    [Inject] private ISuggestionBackfillCoordinator Coordinator { get; set; } = null!;
    [Inject] private ILogger<TodaysCallCard> Log { get; set; } = null!;

    private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

    private string? _backfillLabel;
    private bool _disposed;

    private bool HasDiff =>
        !ReferenceEquals(CallDiff, global::TradyStrat.Features.AiSuggestion.CallDiff.None) &&
        !string.IsNullOrEmpty(CallDiff.SummaryParagraph);

    private string Verb => Sug.Action switch
    {
        SuggestionAction.Acquire => "Acquire.",
        SuggestionAction.Hold    => "Hold.",
        SuggestionAction.Trim    => "Trim.",
        SuggestionAction.Wait    => "Wait.",
        _ => "—"
    };

    protected override void OnInitialized()
    {
        Coordinator.StatusChanged += OnBackfillStatus;
        UpdateBackfillLabel(Coordinator.Status);  // read fresh after subscribe to close late-attach window
    }

    private void OnBackfillStatus(BackfillStatus status)
    {
        if (_disposed) return;
        _ = InvokeAsync(() =>
        {
            if (_disposed) return;       // double-check inside the queued continuation
            try
            {
                UpdateBackfillLabel(status);
                StateHasChanged();
            }
            catch (Exception ex)
            {
                TodaysCallCardLog.StatusChangedCallbackFailed(Log, ex);
            }
        });
    }

    private void UpdateBackfillLabel(BackfillStatus status)
    {
        _backfillLabel = status switch
        {
            BackfillStatus.Running r => $"backfilling {r.Total - r.Remaining + 1} of {r.Total} — {r.CurrentDate:dd MMM}",
            BackfillStatus.Failed f  => $"stopped at {f.FailedAt:dd MMM} — {f.Reason}",
            _ => null,
        };
    }

    public void Dispose()
    {
        _disposed = true;                          // set before unsubscribe so in-flight continuations short-circuit
        Coordinator.StatusChanged -= OnBackfillStatus;
        GC.SuppressFinalize(this);
    }

    private static string RomanLowercase(int i) =>
        i switch
        {
            1 => "i", 2 => "ii", 3 => "iii", 4 => "iv", 5 => "v",
            6 => "vi", 7 => "vii", 8 => "viii", 9 => "ix", 10 => "x",
            _ => i.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };

    private static string RenderSparklineSvg(IndicatorSeries s)
    {
        if (s.Values.Count < 2) return "";
        decimal min = s.Values.Min(), max = s.Values.Max();
        if (s.ThresholdHi is { } hi) { if (hi < min) min = hi; if (hi > max) max = hi; }
        if (s.ThresholdLo is { } lo) { if (lo < min) min = lo; if (lo > max) max = lo; }
        var range = max - min;
        if (range == 0m) range = 1m;

        const int W = 60, H = 14;
        var pts = new System.Text.StringBuilder();
        for (int i = 0; i < s.Values.Count; i++)
        {
            var x = (double)i * W / (s.Values.Count - 1);
            var y = H - (double)((s.Values[i] - min) / range) * H;
            if (i > 0) pts.Append(' ');
            pts.Append(string.Create(System.Globalization.CultureInfo.InvariantCulture,
                $"{x:0.##},{y:0.##}"));
        }

        var thresholdLines = new System.Text.StringBuilder();
        void DrawThreshold(decimal? t)
        {
            if (t is null) return;
            var ty = H - (double)((t.Value - min) / range) * H;
            var tyStr = ty.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            thresholdLines.Append(
                "<line x1=\"0\" y1=\"" + tyStr + "\" " +
                "x2=\"" + W.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\" y2=\"" + tyStr + "\" " +
                "stroke=\"rgba(196,154,86,0.25)\" stroke-dasharray=\"2 2\" />");
        }
        DrawThreshold(s.ThresholdHi);
        DrawThreshold(s.ThresholdLo);

        return $"<svg viewBox=\"0 0 {W} {H}\" width=\"{W}\" height=\"{H}\">" +
               thresholdLines +
               $"<polyline points=\"{pts}\" fill=\"none\" stroke=\"#c49a56\" stroke-width=\"1.2\" />" +
               "</svg>";
    }
}

internal static partial class TodaysCallCardLog
{
    [LoggerMessage(Level = LogLevel.Warning,
        Message = "TodaysCallCard StatusChanged callback threw")]
    public static partial void StatusChangedCallbackFailed(ILogger logger, Exception ex);
}
