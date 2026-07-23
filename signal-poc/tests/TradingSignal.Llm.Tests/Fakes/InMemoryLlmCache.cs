using TradingSignal.Core;
using TradingSignal.Llm.Caching;

namespace TradingSignal.Llm.Tests.Fakes;

internal sealed class InMemoryLlmCache : ILlmResponseCache
{
    public Dictionary<string, RawSignal> Store { get; } = new();
    public int GetCount { get; private set; }
    public int SetCount { get; private set; }

    public Task<RawSignal?> TryGetAsync(string key, CancellationToken ct)
    {
        GetCount++;
        return Task.FromResult(Store.TryGetValue(key, out RawSignal? v) ? v : null);
    }

    public Task SetAsync(string key, RawSignal signal, CancellationToken ct)
    {
        SetCount++;
        Store[key] = signal;
        return Task.CompletedTask;
    }
}
