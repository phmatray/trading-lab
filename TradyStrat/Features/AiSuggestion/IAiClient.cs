using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.AiSuggestion;

public interface IAiClient
{
    Task<Suggestion> AskAsync(AiSnapshot snapshot, CancellationToken ct);
}
