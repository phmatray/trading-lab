using TradyStrat.Application.AiSuggestion.Snapshot;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Suggestions;

namespace TradyStrat.Application.AiSuggestion.UseCases;

public sealed class BackfillSuggestionsUseCase(
    ISuggestionRepository suggestions,
    IAiSnapshotService snapshotService,
    IAiClient ai,
    IClock clock,
    IDomainEventDispatcher dispatcher,
    ILogger<BackfillSuggestionsUseCase> log)
    : UseCaseBase<BackfillSuggestionsInput, Suggestion>(log)
{
    protected override async Task<Suggestion> ExecuteCore(
        BackfillSuggestionsInput input, CancellationToken ct)
    {
        var snapshot = await snapshotService.CreateAsync(input.InstrumentId, input.Date, ct);
        var response = await ai.AskAsync(snapshot, ct);
        var suggestion = SuggestionBuilder.FromAiResponse(response, snapshot, clock.UtcNow());
        var events = await suggestions.AddAsync(suggestion, ct);
        await dispatcher.DispatchAsync(events, ct);
        return suggestion;
    }
}
