using Ardalis.Specification;
using TradyStrat.Domain.Suggestions;
using TradyStrat.Domain;
using TradyStrat.Domain.Suggestions;

namespace TradyStrat.Application.AiSuggestion.Specifications;

/// <summary>
/// The most recent <paramref name="take"/> suggestions for an instrument with
/// ForDate strictly before <paramref name="before"/>. Ordered descending by
/// ForDate so the LINQ <c>.Take(N)</c> returns the latest N rows; the consumer
/// re-orders ascending for the prompt JSON.
/// </summary>
public sealed class RecentSuggestionsForInstrumentSpec : Specification<Suggestion>
{
    public RecentSuggestionsForInstrumentSpec(int instrumentId, DateOnly before, int take)
    {
        Query
            .Where(s => s.InstrumentId == instrumentId && s.ForDate < before)
            .OrderByDescending(s => s.ForDate)
            .Take(take);
    }
}
