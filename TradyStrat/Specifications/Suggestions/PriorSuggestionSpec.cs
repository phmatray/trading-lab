using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Specifications.Suggestions;

public sealed class PriorSuggestionSpec : Specification<Suggestion>
{
    public PriorSuggestionSpec(DateOnly beforeExclusive)
        => Query.Where(s => s.ForDate < beforeExclusive)
                .OrderByDescending(s => s.ForDate)
                .Take(1);
}
