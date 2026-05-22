using TradyStrat.Domain.Suggestions.Services;
using TradyStrat.Domain.Suggestions;
using Ardalis.Specification;
using TradyStrat.Domain.Suggestions;
using TradyStrat.Application.AiSuggestion.Specifications;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;

namespace TradyStrat.Application.AiSuggestion.UseCases;

public sealed class QuerySuggestionsUseCase(
    IReadRepositoryBase<Suggestion> suggestions,
    ForwardReturnCalculator forwardReturn,
    ICorrectnessRule correctness,
    ILogger<QuerySuggestionsUseCase> logger)
    : UseCaseBase<QuerySuggestionsInput, QuerySuggestionsOutput>(logger)
{
    protected override async Task<QuerySuggestionsOutput> ExecuteCore(QuerySuggestionsInput input, CancellationToken ct)
    {
        var rows = await suggestions.ListAsync(
            new SuggestionsQuerySpec(input.InstrumentId, input.From, input.To, input.Action, input.Limit), ct);

        var items = new List<QueriedSuggestion>(rows.Count);
        foreach (var s in rows)
        {
            var fwd     = await forwardReturn.ComputeAsync(s, ct);
            var correct = fwd.HasValue ? correctness.Evaluate(s.Action, fwd.Value) : (bool?)null;
            items.Add(new QueriedSuggestion(
                s.ForDate,
                s.Action,
                s.Conviction,
                s.Rationale,
                s.EnvelopeHash,
                s.PromptVersionHash,
                fwd,
                correct));
        }

        return new QuerySuggestionsOutput(items);
    }
}
