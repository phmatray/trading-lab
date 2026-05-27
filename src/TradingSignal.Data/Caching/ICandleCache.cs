using TradingSignal.Core;

namespace TradingSignal.Data.Caching;

public interface ICandleCache
{
    Task<IReadOnlyList<Candle>?> TryReadAsync(
        string symbol,
        TimeSpan interval,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken ct);

    Task WriteAsync(
        string symbol,
        TimeSpan interval,
        IReadOnlyList<Candle> candles,
        CancellationToken ct);
}

public sealed class NullCandleCache : ICandleCache
{
    public static readonly NullCandleCache Instance = new();

    private NullCandleCache() { }

    public Task<IReadOnlyList<Candle>?> TryReadAsync(string symbol, TimeSpan interval, DateTime startUtc, DateTime endUtc, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<Candle>?>(null);

    public Task WriteAsync(string symbol, TimeSpan interval, IReadOnlyList<Candle> candles, CancellationToken ct)
        => Task.CompletedTask;
}
