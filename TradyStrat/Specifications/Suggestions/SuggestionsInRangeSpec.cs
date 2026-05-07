using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Specifications.Suggestions;

public sealed class SuggestionsInRangeSpec : Specification<Suggestion>
{
    public SuggestionsInRangeSpec(DateOnly fromInclusive, DateOnly toInclusive)
        => Query.Where(s => s.ForDate >= fromInclusive && s.ForDate <= toInclusive)
                .OrderBy(s => s.ForDate);
}
