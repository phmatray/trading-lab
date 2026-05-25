using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;
using TradyStrat.Domain.Suggestions;

namespace TradyStrat.Application.AiSuggestion;

public interface ISuggestionRepository
{
    Task<Suggestion?> GetForAsync(InstrumentId instrumentId, DateOnly date, CancellationToken ct);
    Task<IReadOnlyList<Suggestion>> ListForAsync(InstrumentId instrumentId, DateRange range, CancellationToken ct);
    Task<Suggestion?> LatestForAsync(InstrumentId instrumentId, CancellationToken ct);
    Task<IReadOnlyList<Suggestion>> QueryAsync(InstrumentId? instrumentId, DateRange? range, SuggestionAction? action, int take, CancellationToken ct);
    Task<Suggestion?> PriorToAsync(InstrumentId instrumentId, DateOnly before, CancellationToken ct);
    Task<IReadOnlyList<Suggestion>> RecentForAsync(InstrumentId instrumentId, DateOnly asOf, int count, CancellationToken ct);

    /// <summary>Persists the new Suggestion and returns its drained domain events.</summary>
    Task<IReadOnlyList<IDomainEvent>> AddAsync(Suggestion suggestion, CancellationToken ct);

    /// <summary>Removes the suggestion. Phase 7 has no SuggestionRemoved event; returns Task.</summary>
    Task RemoveAsync(Suggestion suggestion, CancellationToken ct);
}
