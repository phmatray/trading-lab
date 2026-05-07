using System.Globalization;
using Microsoft.AspNetCore.Components;
using TradyStrat.Features.AiSuggestion;
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

    // Used as a data-attribute on the drop element so CSS can size the
    // longer verbs ("Acquire.") slightly tighter without a measure script.
    private string VerbStem => Sug.Action switch
    {
        SuggestionAction.Acquire => "acquire",
        SuggestionAction.Hold    => "hold",
        SuggestionAction.Trim    => "trim",
        SuggestionAction.Wait    => "wait",
        _ => "none"
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

}

internal static partial class TodaysCallCardLog
{
    [LoggerMessage(Level = LogLevel.Warning,
        Message = "TodaysCallCard StatusChanged callback threw")]
    public static partial void StatusChangedCallbackFailed(ILogger logger, Exception ex);
}
