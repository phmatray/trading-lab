using TradyStrat.Features.AiSuggestion;

namespace TradyStrat.Tests.UseCases.AiSuggestion;

public sealed class StubSnapshotFactory(AiSnapshot snapshot) : ISnapshotFactory
{
    public Task<AiSnapshot> CreateAsync(DateOnly asOf, CancellationToken ct) => Task.FromResult(snapshot);
}
