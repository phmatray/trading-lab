using Binance.Net.Clients;
using Binance.Net.Interfaces;
using TradingSignal.Core;

namespace TradingSignal.Data.Binance;

public sealed class BinanceKlineFetcher : IKlineFetcher, IDisposable
{
    private readonly BinanceRestClient _client = new();

    public async Task<IReadOnlyList<Candle>> FetchPageAsync(
        string symbol,
        TimeSpan interval,
        DateTime startUtc,
        DateTime endUtc,
        int limit,
        CancellationToken ct)
    {
        var klineInterval = BinanceIntervalMapper.ToKlineInterval(interval);
        var result = await _client.SpotApi.ExchangeData.GetKlinesAsync(
            symbol,
            klineInterval,
            startUtc,
            endUtc,
            limit,
            ct).ConfigureAwait(false);

        if (!result.Success || result.Data is null)
        {
            throw new InvalidOperationException(
                $"Binance kline request failed: {result.Error?.Message ?? "no data"}");
        }

        return result.Data.Select(ToCandle).ToList();
    }

    private static Candle ToCandle(IBinanceKline k) => new(
        k.OpenTime,
        k.OpenPrice,
        k.HighPrice,
        k.LowPrice,
        k.ClosePrice,
        k.Volume);

    public void Dispose() => _client.Dispose();
}
