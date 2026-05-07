namespace TradyStrat.Features.AiSuggestion;

public interface ISnapshotFactory
{
    Task<AiSnapshot> CreateAsync(DateOnly asOf, CancellationToken ct);
}
