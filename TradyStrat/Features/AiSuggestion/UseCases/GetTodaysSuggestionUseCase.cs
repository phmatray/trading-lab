using Ardalis.Specification;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Common.Time;
using TradyStrat.Common.UseCases;
using TradyStrat.Features.AiSuggestion.Snapshot;
using TradyStrat.Features.AiSuggestion.Specifications;

namespace TradyStrat.Features.AiSuggestion.UseCases;

public sealed class GetTodaysSuggestionUseCase(
    IRepositoryBase<Suggestion> repo,
    ISnapshotFactory snapshotFactory,
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

        var existing = await repo.FirstOrDefaultAsync(
            new SuggestionForDateSpec(today, instrument.Id), ct);
        if (existing is not null) return existing;

        var snap  = await snapshotFactory.CreateAsync(instrument.Id, today, ct);
        var fresh = await ai.AskAsync(snap, ct);
        await repo.AddAsync(fresh, ct);
        return fresh;
    }
}
