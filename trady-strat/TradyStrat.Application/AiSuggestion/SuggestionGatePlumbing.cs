using System.Collections.Concurrent;

namespace TradyStrat.Application.AiSuggestion;

/// <summary>
/// Per-(date, instrumentId) mutex serializing today's-suggestion writes.
/// The pure decision (should we fetch?) lives in
/// TradyStrat.Domain.Suggestions.Services.SuggestionGate. This class is the
/// orchestration plumbing — a semaphore keyed by (date, instrumentId) that
/// ensures concurrent dashboard fans-out don't race on the same row.
///
/// Static because use cases are scoped per request, but the lock must be
/// shared across all scopes. Entries are not reclaimed; growth is bounded
/// by (held instruments) × (days), which is negligible for a single-user app.
/// </summary>
internal static class SuggestionGatePlumbing
{
    private static readonly ConcurrentDictionary<(DateOnly Date, int InstrumentId), SemaphoreSlim> Gates = new();

    public static SemaphoreSlim For(DateOnly date, int instrumentId)
        => Gates.GetOrAdd((date, instrumentId), _ => new SemaphoreSlim(1, 1));
}
