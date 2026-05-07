using TradyStrat.Features.AiSuggestion;
using TradyStrat.Features.AiSuggestion.Snapshot;
using TradyStrat.Common.Domain;

namespace TradyStrat.Tests.AiSuggestion.UseCases;

public sealed class StubAiClient(Suggestion suggestion) : IAiClient
{
    public Task<Suggestion> AskAsync(AiSnapshot snapshot, CancellationToken ct)
        => Task.FromResult(suggestion);
}
