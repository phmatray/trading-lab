using Ardalis.Specification;
using TradyStrat.Domain.Suggestions;
using TradyStrat.Domain;
using TradyStrat.Domain.Suggestions;

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
