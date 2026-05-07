using Ardalis.Specification;
using TradyStrat.Common.Domain;

namespace TradyStrat.Features.AiSuggestion.Specifications;

public sealed class PriorSuggestionSpec : Specification<Suggestion>
{
    public PriorSuggestionSpec(DateOnly beforeExclusive)
        => Query.Where(s => s.ForDate < beforeExclusive)
                .OrderByDescending(s => s.ForDate)
                .Take(1);
}
