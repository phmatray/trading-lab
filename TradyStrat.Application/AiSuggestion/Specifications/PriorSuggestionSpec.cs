using Ardalis.Specification;
using TradyStrat.Domain;

namespace TradyStrat.Application.AiSuggestion.Specifications;

public sealed class PriorSuggestionSpec : Specification<Suggestion>
{
    public PriorSuggestionSpec(DateOnly beforeExclusive, int instrumentId)
    {
        Query.Where(s => s.ForDate < beforeExclusive && s.InstrumentId == instrumentId)
             .OrderByDescending(s => s.ForDate)
             .Take(1);
    }
}
