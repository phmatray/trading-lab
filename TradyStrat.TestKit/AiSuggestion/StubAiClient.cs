using TradyStrat.Application.AiSuggestion;
using TradyStrat.Application.AiSuggestion.Snapshot;

namespace TradyStrat.TestKit.AiSuggestion;

public sealed class StubAiClient(AiResponse response) : IAiClient
{
    public Task<AiResponse> AskAsync(AiSnapshot snapshot, CancellationToken ct)
        => Task.FromResult(response);
}
