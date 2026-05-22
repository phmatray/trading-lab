using TradyStrat.Domain.Suggestions;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using TradyStrat.Application.AiSuggestion;
using TradyStrat.Application.AiSuggestion.Backfill;
using TradyStrat.Application.Dashboard;
using TradyStrat.Domain;

namespace TradyStrat.Features.Dashboard.Components;

public partial class TodaysCallCard : ComponentBase, IDisposable
{
    [Parameter] public SuggestionState? State { get; set; }
    [Parameter] public DateOnly Today { get; set; }
    [Parameter] public string CallAsOfRelative { get; set; } = "";
    [Parameter] public bool Historical { get; set; }
    [Parameter] public string FocusLabel { get; set; } = "CON3";
    [Parameter] public EventCallback OnLogTrade { get; set; }
    [Parameter] public EventCallback OnRerun { get; set; }
    [Parameter] public EventCallback OnRetry { get; set; }

    [Parameter, EditorRequired] public BackfillStatus BackfillStatus { get; set; } = BackfillStatus.Idle.Instance;

    [Inject] private ISuggestionBackfillCoordinator Coordinator { get; set; } = null!;
    [Inject] private ILogger<TodaysCallCard> Log { get; set; } = null!;

    private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

    private string? _backfillLabel;
    private bool _disposed;

    private Suggestion? ReadySug => State is SuggestionState.Ready r ? r.Suggestion : null;

    private string Verb => SuggestionActionDisplay.Verb(ReadySug?.Action);

    private string VerbStem => SuggestionActionDisplay.Stem(ReadySug?.Action);

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s.AsSpan(0, max - 1).ToString() + "…";

    protected override void OnInitialized()
    {
        Coordinator.StatusChanged += OnBackfillStatus;
        UpdateBackfillLabel(Coordinator.Status);
    }

    private void OnBackfillStatus(BackfillStatus status)
    {
        if (_disposed) return;
        _ = InvokeAsync(() =>
        {
            if (_disposed) return;
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
        _disposed = true;
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
