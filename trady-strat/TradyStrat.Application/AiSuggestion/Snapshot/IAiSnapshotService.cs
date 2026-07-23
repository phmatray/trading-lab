namespace TradyStrat.Application.AiSuggestion.Snapshot;

public interface IAiSnapshotService
{
    Task<AiSnapshot> CreateAsync(int instrumentId, DateOnly asOf, CancellationToken ct);
}
