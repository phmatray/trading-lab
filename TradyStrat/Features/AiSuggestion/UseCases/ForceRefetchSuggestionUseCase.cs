using Ardalis.Specification;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Application.UseCases;
using TradyStrat.Features.AiSuggestion.Snapshot;
using TradyStrat.Features.AiSuggestion.Specifications;

namespace TradyStrat.Features.AiSuggestion.UseCases;

public sealed class ForceRefetchSuggestionUseCase(
    IRepositoryBase<Suggestion> repo,
    IAiSnapshotService snapshotService,
    IAiClient ai,
    IClock clock,
    IReadRepositoryBase<Instrument> instruments,
    ILogger<ForceRefetchSuggestionUseCase> log)
    : UseCaseBase<ForceRefetchSuggestionInput, Suggestion>(log)
{
    protected override async Task<Suggestion> ExecuteCore(
        ForceRefetchSuggestionInput input, CancellationToken ct)
    {
        var instrument = await instruments.GetByIdAsync(input.InstrumentId, ct)
            ?? throw new InstrumentNotFoundException(
                $"Instrument id {input.InstrumentId} not registered.");

        var today = clock.TodayInExchangeTzFor(instrument.Ticker);

        // Acquire the shared gate so the delete+insert pair runs as a single
        // critical section against any concurrent GetTodaysSuggestion or
        // ForceRefetch — otherwise two concurrent rerun clicks both pass
        // the existence check, then both try to INSERT and the second hits
        // the UQ(ForDate, InstrumentId) constraint.
        await SuggestionGate.Instance.WaitAsync(ct);
        try
        {
            var existing = await repo.FirstOrDefaultAsync(
                new SuggestionForDateSpec(today, instrument.Id), ct);
            if (existing is not null) await repo.DeleteAsync(existing, ct);

            var snap  = await snapshotService.CreateAsync(instrument.Id, today, ct);
            var fresh = await ai.AskAsync(snap, ct);
            await repo.AddAsync(fresh, ct);
            return fresh;
        }
        finally
        {
            SuggestionGate.Instance.Release();
        }
    }
}
