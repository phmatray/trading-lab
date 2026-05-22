using TradyStrat.Domain;
using TradyStrat.Domain.Suggestions;
using TradyStrat.Application.AiSuggestion.Snapshot;
using TradyStrat.Domain.Suggestions;

namespace TradyStrat.Application.AiSuggestion;

public interface IAiClient
{
    Task<Suggestion> AskAsync(AiSnapshot snapshot, CancellationToken ct);
}
