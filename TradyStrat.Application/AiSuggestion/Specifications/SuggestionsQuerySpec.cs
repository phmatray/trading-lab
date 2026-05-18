using Ardalis.Specification;
using TradyStrat.Domain;

namespace TradyStrat.Application.AiSuggestion.Specifications;

public sealed class SuggestionsQuerySpec : Specification<Suggestion>
{
    public SuggestionsQuerySpec(int instrumentId, DateOnly from, DateOnly to, SuggestionAction? action, int limit)
    {
        Query
            .Where(s => s.InstrumentId == instrumentId
                     && s.ForDate >= from
                     && s.ForDate <= to)
            .OrderByDescending(s => s.ForDate)
            .Take(limit);

        if (action.HasValue)
            Query.Where(s => s.Action == action.Value);
    }
}
