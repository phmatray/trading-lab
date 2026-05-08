using Ardalis.Specification;
using TradyStrat.Common.Domain;
using TradyStrat.Common.UseCases;
using TradyStrat.Features.AiSuggestion.Snapshot;

namespace TradyStrat.Features.AiSuggestion.UseCases;

public sealed class BackfillSuggestionsUseCase(
    IRepositoryBase<Suggestion> repo,
    ISnapshotFactory snapshotFactory,
    IAiClient ai,
    ILogger<BackfillSuggestionsUseCase> log)
    : UseCaseBase<BackfillSuggestionsInput, Suggestion>(log)
{
    protected override async Task<Suggestion> ExecuteCore(
        BackfillSuggestionsInput input, CancellationToken ct)
    {
        var snapshot   = await snapshotFactory.CreateAsync(input.InstrumentId, input.Date, ct);
        var suggestion = await ai.AskAsync(snapshot, ct);
        await repo.AddAsync(suggestion, ct);
        return suggestion;
    }
}
