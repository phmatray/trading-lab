using TradyStrat.Domain.Suggestions;
using TradyStrat.Application.AiSuggestion;
using TradyStrat.Application.AiSuggestion.Snapshot;
using TradyStrat.Domain;

namespace TradyStrat.TestKit.AiSuggestion;

public sealed class StubAiClient(Suggestion suggestion) : IAiClient
{
    public Task<Suggestion> AskAsync(AiSnapshot snapshot, CancellationToken ct)
        => Task.FromResult(suggestion);
}
