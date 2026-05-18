using Ardalis.Specification;
using TradyStrat.Domain;

namespace TradyStrat.Application.AiSuggestion.Specifications;

public sealed class LatestSuggestionSpec : Specification<Suggestion>
{
    public LatestSuggestionSpec(int instrumentId)
    {
        Query.Where(s => s.InstrumentId == instrumentId)
             .OrderByDescending(s => s.ForDate)
             .Take(1);
    }
}
