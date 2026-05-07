using TradyStrat.Common.Domain;
using TradyStrat.Features.AiSuggestion.Backfill;
using TradyStrat.Features.AiSuggestion.CallDiff;
using TradyStrat.Features.AiSuggestion.Snapshot;

namespace TradyStrat.Features.AiSuggestion;

public interface IAiClient
{
    Task<Suggestion> AskAsync(AiSnapshot snapshot, CancellationToken ct);
}
