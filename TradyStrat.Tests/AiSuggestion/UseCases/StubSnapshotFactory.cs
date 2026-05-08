using TradyStrat.Features.AiSuggestion.Snapshot;

namespace TradyStrat.Tests.AiSuggestion.UseCases;

public sealed class StubSnapshotFactory(AiSnapshot snapshot) : ISnapshotFactory
{
    public Task<AiSnapshot> CreateAsync(int instrumentId, DateOnly asOf, CancellationToken ct)
        => Task.FromResult(snapshot);
}
