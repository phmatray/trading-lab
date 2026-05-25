using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Suggestions;

namespace TradyStrat.Application.AiSuggestion.UseCases;

/// <summary>
/// Saga (swallow-and-continue): aggregates per-Held-instrument AI calls into
/// one batch. A typed <see cref="TradyStratException"/> on any per-ticker call
/// is logged at Warning and excluded from the result, but does NOT abort the
/// remaining calls. Contrast with <c>SuggestionBackfillCoordinator</c>'s
/// first-fail-stop Saga (user-initiated repair, where the failing date is the
/// diagnostic).
/// </summary>
public sealed partial class GetAllTodaysSuggestionsUseCase(
    GetTodaysSuggestionUseCase singleTicker,
    ListInstrumentsUseCase listInstruments,
    ILogger<GetAllTodaysSuggestionsUseCase> log)
    : UseCaseBase<Unit, IReadOnlyList<Suggestion>>(log)
{
    protected override async Task<IReadOnlyList<Suggestion>> ExecuteCore(
        Unit _, CancellationToken ct)
    {
        var all  = await listInstruments.ExecuteAsync(Unit.Value, ct);
        var held = all.Where(i => i.Kind == InstrumentKind.Held).ToList();

        var results = new List<Suggestion>(held.Count);
        foreach (var inst in held)
        {
            try
            {
                var s = await singleTicker.ExecuteAsync(
                    new GetTodaysSuggestionInput(inst.Id.Value), ct);
                results.Add(s);
            }
            catch (TradyStratException ex)
            {
                LogPerTickerFailure(log, ex, inst.Ticker);
            }
        }
        return results;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "AI call failed for {Ticker}")]
    private static partial void LogPerTickerFailure(ILogger logger, Exception ex, string ticker);
}
