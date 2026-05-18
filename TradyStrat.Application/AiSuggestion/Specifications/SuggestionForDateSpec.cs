using Ardalis.Specification;
using TradyStrat.Domain;

namespace TradyStrat.Application.AiSuggestion.Specifications;

public sealed class SuggestionForDateSpec : Specification<Suggestion>
{
    public SuggestionForDateSpec(DateOnly date, int instrumentId)
    {
        Query.Where(s => s.ForDate == date && s.InstrumentId == instrumentId)
             .Take(1);
    }
}
