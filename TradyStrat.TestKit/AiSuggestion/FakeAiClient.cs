using System.Collections.Concurrent;
using TradyStrat.Application.AiSuggestion;
using TradyStrat.Application.AiSuggestion.Snapshot;

namespace TradyStrat.TestKit.AiSuggestion;

/// <summary>
/// Test double for <see cref="IAiClient"/> with per-instrument scripting.
/// Records peak observed concurrent calls so callers can assert
/// SemaphoreSlim caps. Unknown instrument ids throw — tests must
/// configure every instrument they invoke through.
/// </summary>
public sealed class FakeAiClient : IAiClient
{
    private readonly ConcurrentDictionary<int, Func<CancellationToken, Task<AiResponse>>> _byInstrument = new();
    private int _inFlight;
    private int _maxObserved;

    public int MaxObservedConcurrency => Volatile.Read(ref _maxObserved);

    public void ConfigureFor(int instrumentId, AiResponse result)
        => _byInstrument[instrumentId] = _ => Task.FromResult(result);

    public void ConfigureFor(int instrumentId, AiResponse result, TimeSpan delay)
        => _byInstrument[instrumentId] = async ct =>
        {
            await Task.Delay(delay, ct);
            return result;
        };

    public void ConfigureFailureFor(int instrumentId, Exception error)
        => _byInstrument[instrumentId] = _ => Task.FromException<AiResponse>(error);

    public void ConfigureFailureFor(int instrumentId, Exception error, TimeSpan delay)
        => _byInstrument[instrumentId] = async ct =>
        {
            await Task.Delay(delay, ct);
            throw error;
        };

    public async Task<AiResponse> AskAsync(AiSnapshot snapshot, CancellationToken ct)
    {
        var current = Interlocked.Increment(ref _inFlight);
        try
        {
            int observed;
            do { observed = _maxObserved; }
            while (current > observed &&
                   Interlocked.CompareExchange(ref _maxObserved, current, observed) != observed);

            if (!_byInstrument.TryGetValue(snapshot.InstrumentId, out var handler))
                throw new InvalidOperationException(
                    $"FakeAiClient: no configuration for InstrumentId {snapshot.InstrumentId}.");

            return await handler(ct);
        }
        finally
        {
            Interlocked.Decrement(ref _inFlight);
        }
    }
}
