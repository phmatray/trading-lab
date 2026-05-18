using Ardalis.Specification;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Application.UseCases;
using TradyStrat.Application.AiSuggestion.Snapshot;
using TradyStrat.Application.AiSuggestion.Specifications;

namespace TradyStrat.Application.AiSuggestion.UseCases;

public sealed class GetTodaysSuggestionUseCase(
    IRepositoryBase<Suggestion> repo,
    IAiSnapshotService snapshotService,
    IAiClient ai,
    IClock clock,
    IReadRepositoryBase<Instrument> instruments,
    ILogger<GetTodaysSuggestionUseCase> log)
    : UseCaseBase<GetTodaysSuggestionInput, Suggestion>(log)
{
    protected override async Task<Suggestion> ExecuteCore(
        GetTodaysSuggestionInput input, CancellationToken ct)
    {
        var instrument = await instruments.GetByIdAsync(input.InstrumentId, ct)
            ?? throw new InstrumentNotFoundException(
                $"Instrument id {input.InstrumentId} not registered.");

        var today = clock.TodayInExchangeTzFor(instrument.Ticker);

        // Fast path: row already there, no gate needed.
        var existing = await repo.FirstOrDefaultAsync(
            new SuggestionForDateSpec(today, instrument.Id), ct);
        if (existing is not null) return existing;

        // Slow path: serialize concurrent writers via the shared gate, then
        // re-check inside the gate (a peer may have inserted while we waited).
        // The gate is process-wide; with N held instruments, GetAll's sequential
        // loop trivially serializes anyway, so the gate's value is for
        // inter-request races (two browser tabs, refresh during a re-run).
        await SuggestionGate.Instance.WaitAsync(ct);
        try
        {
            existing = await repo.FirstOrDefaultAsync(
                new SuggestionForDateSpec(today, instrument.Id), ct);
            if (existing is not null) return existing;

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
