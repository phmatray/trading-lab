using TradingSignal.Core;

namespace TradingSignal.Llm.Caching;

public interface ILlmResponseCache
{
    Task<RawSignal?> TryGetAsync(string key, CancellationToken ct);

    Task SetAsync(string key, RawSignal signal, CancellationToken ct);
}

public sealed class NullLlmResponseCache : ILlmResponseCache
{
    public static readonly NullLlmResponseCache Instance = new();

    private NullLlmResponseCache() { }

    public Task<RawSignal?> TryGetAsync(string key, CancellationToken ct) => Task.FromResult<RawSignal?>(null);

    public Task SetAsync(string key, RawSignal signal, CancellationToken ct) => Task.CompletedTask;
}
