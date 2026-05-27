using TradingSignal.Core;

namespace TradingSignal.Data.Binance;

public interface IKlineFetcher
{
    Task<IReadOnlyList<Candle>> FetchPageAsync(
        string symbol,
        TimeSpan interval,
        DateTime startUtc,
        DateTime endUtc,
        int limit,
        CancellationToken ct);
}
