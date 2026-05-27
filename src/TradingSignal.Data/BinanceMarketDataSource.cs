using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TradingSignal.Core;
using TradingSignal.Core.Abstractions;
using TradingSignal.Data.Binance;
using TradingSignal.Data.Caching;
using TradingSignal.Data.Validation;

namespace TradingSignal.Data;

public sealed partial class BinanceMarketDataSource(
    IKlineFetcher fetcher,
    ICandleCache cache,
    ILogger<BinanceMarketDataSource>? logger = null,
    TimeSpan? interPageDelay = null,
    int pageLimit = 1000)
    : IMarketDataSource
{
    private readonly ILogger<BinanceMarketDataSource> _logger = logger ?? NullLogger<BinanceMarketDataSource>.Instance;
    private readonly TimeSpan _interPageDelay = interPageDelay ?? TimeSpan.FromMilliseconds(250);

    public async Task<IReadOnlyList<Candle>> GetCandlesAsync(
        string symbol, TimeSpan interval, DateTime startUtc, DateTime endUtc, CancellationToken ct)
    {
        if (endUtc <= startUtc) throw new ArgumentException("endUtc must be after startUtc");

        var cached = await cache.TryReadAsync(symbol, interval, startUtc, endUtc, ct).ConfigureAwait(false);
        if (cached is not null)
        {
            LogCacheHit(_logger, symbol, interval, startUtc, endUtc, cached.Count);
            return cached;
        }

        var fetched = await FetchAllPagesAsync(symbol, interval, startUtc, endUtc, ct).ConfigureAwait(false);
        MarketDataValidator.Validate(fetched, interval, _logger);
        await cache.WriteAsync(symbol, interval, fetched, ct).ConfigureAwait(false);

        return fetched;
    }

    private async Task<IReadOnlyList<Candle>> FetchAllPagesAsync(
        string symbol, TimeSpan interval, DateTime startUtc, DateTime endUtc, CancellationToken ct)
    {
        var result = new List<Candle>();
        var cursor = startUtc;
        var pages = 0;

        while (cursor < endUtc)
        {
            var page = await fetcher.FetchPageAsync(symbol, interval, cursor, endUtc, pageLimit, ct)
                .ConfigureAwait(false);
            pages++;

            if (page.Count == 0)
            {
                LogEmptyPage(_logger, cursor);
                break;
            }

            foreach (var c in page)
            {
                if (result.Count == 0 || c.OpenTimeUtc > result[^1].OpenTimeUtc)
                    result.Add(c);
            }

            var lastTime = page[^1].OpenTimeUtc;
            if (lastTime <= cursor - interval)
            {
                // Defensive: server returned data older than our cursor — bail rather than loop.
                LogPaginationStalled(_logger, cursor, lastTime);
                break;
            }

            cursor = lastTime + interval;
            if (page.Count < pageLimit) break;
            if (_interPageDelay > TimeSpan.Zero && cursor < endUtc)
                await Task.Delay(_interPageDelay, ct).ConfigureAwait(false);
        }

        LogFetched(_logger, result.Count, symbol, interval, pages);
        return result;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Cache hit for {Symbol} {Interval} {Start}..{End} ({Count} candles)")]
    private static partial void LogCacheHit(ILogger logger, string symbol, TimeSpan interval, DateTime start, DateTime end, int count);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Empty page at {Cursor} — stopping pagination")]
    private static partial void LogEmptyPage(ILogger logger, DateTime cursor);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Pagination not advancing (cursor={Cursor}, lastTime={Last}) — stopping")]
    private static partial void LogPaginationStalled(ILogger logger, DateTime cursor, DateTime last);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Fetched {Count} candles for {Symbol} {Interval} across {Pages} pages")]
    private static partial void LogFetched(ILogger logger, int count, string symbol, TimeSpan interval, int pages);
}
