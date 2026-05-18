using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Application.UseCases;
using TradyStrat.Features.Settings.UseCases;

namespace TradyStrat.Features.AiSuggestion.UseCases;

/// <summary>
/// Saga (swallow-and-continue): aggregates per-Held-instrument AI calls into
/// one batch. Each per-ticker call is an independent unit of work; a failing
/// call (typed <see cref="TradyStrat.Domain.Exceptions.TradyStratException"/>)
/// is logged at Warning and excluded from the result, but does NOT abort the
/// remaining calls.
///
/// Distinct from <c>SuggestionBackfillCoordinator</c>'s first-fail-stop Saga
/// (Phase 2 Task 1), which is a user-initiated repair operation where the
/// failing date is the diagnostic. The daily fan-out is best-effort and
/// should never block other tickers' calls — keep the policies distinct.
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
                    new GetTodaysSuggestionInput(inst.Id), ct);
                results.Add(s);
            }
            catch (TradyStratException ex)
            {
                LogPerTickerFailure(log, ex, inst.Ticker);
                // Failure isolation — one ticker's call shouldn't take down the others.
            }
        }
        return results;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "AI call failed for {Ticker}")]
    private static partial void LogPerTickerFailure(ILogger logger, Exception ex, string ticker);
}
