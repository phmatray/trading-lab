using TradyStrat.Application.AiSuggestion;
using TradyStrat.Application.AiSuggestion.Snapshot;
using TradyStrat.Domain;

namespace TradyStrat.Tests.AiSuggestion.UseCases;

public sealed class StubAiClient(Suggestion suggestion) : IAiClient
{
    public Task<Suggestion> AskAsync(AiSnapshot snapshot, CancellationToken ct)
        => Task.FromResult(suggestion);
}
