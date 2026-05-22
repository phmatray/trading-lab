using TradyStrat.Domain;
using TradyStrat.Domain.Suggestions;
using TradyStrat.Application.AiSuggestion.Snapshot;

namespace TradyStrat.Application.AiSuggestion;

public interface IAiClient
{
    Task<Suggestion> AskAsync(AiSnapshot snapshot, CancellationToken ct);
}
