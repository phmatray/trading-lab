using Ardalis.Specification;
using TradyStrat.Domain;

namespace TradyStrat.Application.AiSuggestion.Specifications;

public sealed class SuggestionsInRangeSpec : Specification<Suggestion>
{
    public SuggestionsInRangeSpec(DateOnly fromInclusive, DateOnly toInclusive, int instrumentId)
    {
        Query.Where(s => s.ForDate >= fromInclusive
                      && s.ForDate <= toInclusive
                      && s.InstrumentId == instrumentId)
             .OrderBy(s => s.ForDate);
    }
}
