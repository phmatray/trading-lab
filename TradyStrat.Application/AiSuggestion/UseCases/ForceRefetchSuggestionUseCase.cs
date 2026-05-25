using TradyStrat.Application.AiSuggestion.Snapshot;
using TradyStrat.Application.Settings;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Instruments;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared.Market;
using TradyStrat.Domain.Suggestions;

namespace TradyStrat.Application.AiSuggestion.UseCases;

public sealed class ForceRefetchSuggestionUseCase(
    ISuggestionRepository suggestions,
    IAiSnapshotService snapshotService,
    IAiClient ai,
    IClock clock,
    IInstrumentRepository instruments,
    IDomainEventDispatcher dispatcher,
    ILogger<ForceRefetchSuggestionUseCase> log)
    : UseCaseBase<ForceRefetchSuggestionInput, Suggestion>(log)
{
    protected override async Task<Suggestion> ExecuteCore(
        ForceRefetchSuggestionInput input, CancellationToken ct)
    {
        var iid = new InstrumentId(input.InstrumentId);
        var instrument = await instruments.GetAsync(iid, ct)
            ?? throw new InstrumentNotFoundException(
                $"Instrument id {input.InstrumentId} not registered.");

        var today = clock.TodayInExchangeTzFor(instrument.Ticker);

        // The delete+insert pair must run as a single critical section against
        // any concurrent GetTodaysSuggestion or ForceRefetch — otherwise two
        // concurrent rerun clicks both pass the existence check, then both try
        // to INSERT and the second hits the UQ(ForDate, InstrumentId).
        var gate = SuggestionGatePlumbing.For(today, input.InstrumentId);
        await gate.WaitAsync(ct);
        try
        {
            var existing = await suggestions.GetForAsync(iid, today, ct);
            if (existing is not null) await suggestions.RemoveAsync(existing, ct);

            var snap = await snapshotService.CreateAsync(input.InstrumentId, today, ct);
            var response = await ai.AskAsync(snap, ct);
            var fresh = SuggestionBuilder.FromAiResponse(response, snap, clock.UtcNow());
            var events = await suggestions.AddAsync(fresh, ct);
            await dispatcher.DispatchAsync(events, ct);
            return fresh;
        }
        finally
        {
            gate.Release();
        }
    }
}
