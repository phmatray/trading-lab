using System.Collections.Concurrent;

namespace TradyStrat.Application.AiSuggestion.UseCases;

/// <summary>
/// Per-(date, instrumentId) mutex serializing today's-suggestion writes.
///
/// The UQ(ForDate, InstrumentId) constraint means two concurrent inserts
/// for the same (date, instrument) pair race. The gate forces the second
/// writer to wait, then re-check; if the first inserted, the second sees
/// the existing row instead of calling the AI a second time.
///
/// Different (date, instrument) keys do not block each other — that's the
/// whole point of partitioning: the dashboard fans out per held instrument
/// and we want those calls to run truly in parallel.
///
/// Static because use cases are scoped per request, but the lock must be
/// shared across all scopes. Entries are not reclaimed; growth is bounded
/// by (held instruments) × (days), which is negligible for a single-user app.
/// </summary>
internal static class SuggestionGate
{
    private static readonly ConcurrentDictionary<(DateOnly Date, int InstrumentId), SemaphoreSlim> Gates = new();

    public static SemaphoreSlim For(DateOnly date, int instrumentId)
        => Gates.GetOrAdd((date, instrumentId), _ => new SemaphoreSlim(1, 1));
}
