using Ardalis.Specification;
using TradyStrat.Common.Domain;

namespace TradyStrat.Features.AiSuggestion.Specifications;

public sealed class SuggestionForDateSpec : Specification<Suggestion>
{
    public SuggestionForDateSpec(DateOnly date) => Query.Where(s => s.ForDate == date);
}
