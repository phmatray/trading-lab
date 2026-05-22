using TradyStrat.Application.UseCases;
using TradyStrat.Domain.Shared;
using TradyStrat.Domain.Suggestions.Services;

namespace TradyStrat.Application.AiSuggestion.UseCases;

public sealed class QuerySuggestionsUseCase(
    ISuggestionRepository suggestions,
    ForwardReturnCalculator forwardReturn,
    ICorrectnessRule correctness,
    ILogger<QuerySuggestionsUseCase> logger)
    : UseCaseBase<QuerySuggestionsInput, QuerySuggestionsOutput>(logger)
{
    protected override async Task<QuerySuggestionsOutput> ExecuteCore(QuerySuggestionsInput input, CancellationToken ct)
    {
        var rows = await suggestions.QueryAsync(
            instrumentId: new InstrumentId(input.InstrumentId),
            range:        new DateRange(input.From, input.To),
            action:       input.Action,
            take:         input.Limit,
            ct:           ct);

        var items = new List<QueriedSuggestion>(rows.Count);
        foreach (var s in rows)
        {
            var fwd     = await forwardReturn.ComputeAsync(s, ct);
            var correct = fwd.HasValue ? correctness.Evaluate(s.Action, fwd.Value) : (bool?)null;
            items.Add(new QueriedSuggestion(
                s.ForDate,
                s.Action,
                s.Conviction.Value,
                s.Rationale,
                s.Fingerprint.EnvelopeHash,
                s.Fingerprint.PromptVersionHash,
                fwd,
                correct));
        }

        return new QuerySuggestionsOutput(items);
    }
}
