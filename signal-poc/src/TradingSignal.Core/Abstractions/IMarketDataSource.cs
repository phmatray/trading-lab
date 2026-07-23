namespace TradingSignal.Core.Abstractions;

public interface IMarketDataSource
{
    Task<IReadOnlyList<Candle>> GetCandlesAsync(
        string symbol,
        TimeSpan interval,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken ct);
}
