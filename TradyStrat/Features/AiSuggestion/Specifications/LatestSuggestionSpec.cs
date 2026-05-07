using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.AiSuggestion.Specifications;

public sealed class LatestSuggestionSpec : Specification<Suggestion>
{
    public LatestSuggestionSpec()
    {
        Query.OrderByDescending(s => s.ForDate).Take(1);
    }
}
