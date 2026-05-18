using Ardalis.Specification;
using TradyStrat.Domain;
using TradyStrat.Application.UseCases;
using TradyStrat.Application.AiSuggestion.Snapshot;

namespace TradyStrat.Application.AiSuggestion.UseCases;

public sealed class BackfillSuggestionsUseCase(
    IRepositoryBase<Suggestion> repo,
    IAiSnapshotService snapshotService,
    IAiClient ai,
    ILogger<BackfillSuggestionsUseCase> log)
    : UseCaseBase<BackfillSuggestionsInput, Suggestion>(log)
{
    protected override async Task<Suggestion> ExecuteCore(
        BackfillSuggestionsInput input, CancellationToken ct)
    {
        var snapshot   = await snapshotService.CreateAsync(input.InstrumentId, input.Date, ct);
        var suggestion = await ai.AskAsync(snapshot, ct);
        await repo.AddAsync(suggestion, ct);
        return suggestion;
    }
}
