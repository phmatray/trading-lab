using TradyStrat.Features.AiSuggestion;
using TradyStrat.Features.AiSuggestion.Backfill;
using TradyStrat.Features.AiSuggestion.CallDiff;
using TradyStrat.Features.AiSuggestion.Snapshot;

namespace TradyStrat.Tests.AiSuggestion.UseCases;

public sealed class StubSnapshotFactory(AiSnapshot snapshot) : ISnapshotFactory
{
    public Task<AiSnapshot> CreateAsync(DateOnly asOf, CancellationToken ct) => Task.FromResult(snapshot);
}
