using TradyStrat.Features.AiSuggestion;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Tests.UseCases.AiSuggestion;

public sealed class StubSnapshotBuilder(AiSnapshot snapshot) : ISnapshotBuilder
{
    public Task<AiSnapshot> BuildAsync(CancellationToken ct) => Task.FromResult(snapshot);
}
