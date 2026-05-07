using Ardalis.Specification;
using TradyStrat.Application.Abstractions;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.AiSuggestion.UseCases;

public sealed class BackfillSuggestionsUseCase(
    IRepositoryBase<Suggestion> repo,
    ISnapshotFactory snapshotFactory,
    IAiClient ai,
    ILogger<BackfillSuggestionsUseCase> log)
    : UseCaseBase<DateOnly, Suggestion>(log)
{
    protected override async Task<Suggestion> ExecuteCore(DateOnly asOf, CancellationToken ct)
    {
        var snapshot   = await snapshotFactory.CreateAsync(asOf, ct);   // snapshot.Today == asOf
        var suggestion = await ai.AskAsync(snapshot, ct);                // sets ForDate from snapshot.Today
        await repo.AddAsync(suggestion, ct);
        return suggestion;
    }
}
