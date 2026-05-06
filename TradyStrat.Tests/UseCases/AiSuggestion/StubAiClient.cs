using TradyStrat.Features.AiSuggestion;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Tests.UseCases.AiSuggestion;

public sealed class StubAiClient(Suggestion suggestion) : IAiClient
{
    public Task<Suggestion> AskAsync(AiSnapshot snapshot, CancellationToken ct)
        => Task.FromResult(suggestion);
}
