using TradyStrat.Application.AiSuggestion.Snapshot;

namespace TradyStrat.TestKit.AiSuggestion;

public sealed class StubSnapshotFactory(AiSnapshot snapshot) : IAiSnapshotService
{
    public Task<AiSnapshot> CreateAsync(int instrumentId, DateOnly asOf, CancellationToken ct)
        => Task.FromResult(snapshot);
}
