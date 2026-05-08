namespace TradyStrat.Features.AiSuggestion.Snapshot;

public interface ISnapshotFactory
{
    Task<AiSnapshot> CreateAsync(int instrumentId, DateOnly asOf, CancellationToken ct);
}
