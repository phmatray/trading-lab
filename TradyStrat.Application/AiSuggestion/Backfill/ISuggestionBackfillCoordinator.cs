namespace TradyStrat.Application.AiSuggestion.Backfill;

public interface ISuggestionBackfillCoordinator
{
    BackfillStatus Status { get; }

    /// <summary>
    /// Raised when the backfill state transitions. <strong>Invoked on a background thread</strong> —
    /// Blazor consumers must marshal via <c>ComponentBase.InvokeAsync</c> before calling
    /// <c>StateHasChanged</c> or other UI-bound work.
    /// </summary>
    event Action<BackfillStatus>? StatusChanged;

    Task EnsureBackfilledAsync(DateOnly fromExclusive, DateOnly toInclusive, CancellationToken ct);
}
