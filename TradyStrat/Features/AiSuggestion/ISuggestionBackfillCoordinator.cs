namespace TradyStrat.Features.AiSuggestion;

public interface ISuggestionBackfillCoordinator
{
    BackfillStatus Status { get; }
    event Action<BackfillStatus>? StatusChanged;
    Task EnsureBackfilledAsync(DateOnly fromExclusive, DateOnly toInclusive, CancellationToken ct);
}
