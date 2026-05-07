namespace TradyStrat.Features.AiSuggestion.Snapshot;

public interface ISnapshotFactory
{
    Task<AiSnapshot> CreateAsync(DateOnly asOf, CancellationToken ct);
}
