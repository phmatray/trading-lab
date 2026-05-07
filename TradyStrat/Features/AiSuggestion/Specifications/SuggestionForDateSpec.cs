using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.AiSuggestion.Specifications;

public sealed class SuggestionForDateSpec : Specification<Suggestion>
{
    public SuggestionForDateSpec(DateOnly date) => Query.Where(s => s.ForDate == date);
}
