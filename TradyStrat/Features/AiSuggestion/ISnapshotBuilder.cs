namespace TradyStrat.Features.AiSuggestion;

public interface ISnapshotBuilder
{
    Task<AiSnapshot> BuildAsync(CancellationToken ct);
}
