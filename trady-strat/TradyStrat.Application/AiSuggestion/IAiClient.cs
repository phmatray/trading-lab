using TradyStrat.Application.AiSuggestion.Snapshot;

namespace TradyStrat.Application.AiSuggestion;

public interface IAiClient
{
    Task<AiResponse> AskAsync(AiSnapshot snapshot, CancellationToken ct);
}
