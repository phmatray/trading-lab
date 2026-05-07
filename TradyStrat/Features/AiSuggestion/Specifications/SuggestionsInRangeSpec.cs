using Ardalis.Specification;
using TradyStrat.Common.Domain;

namespace TradyStrat.Features.AiSuggestion.Specifications;

public sealed class SuggestionsInRangeSpec : Specification<Suggestion>
{
    public SuggestionsInRangeSpec(DateOnly fromInclusive, DateOnly toInclusive)
        => Query.Where(s => s.ForDate >= fromInclusive && s.ForDate <= toInclusive)
                .OrderBy(s => s.ForDate);
}
