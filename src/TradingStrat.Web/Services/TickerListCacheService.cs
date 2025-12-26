using Microsoft.Extensions.Caching.Memory;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Web.Services;

/// <summary>
/// In-memory cache for ticker lists with 5-minute TTL.
/// Reduces database queries for UI dropdown lists and autocomplete.
/// </summary>
public class TickerListCacheService : ITickerListCacheService
{
    private readonly IMemoryCache _cache;
    private readonly IHistoricalDataPort _historicalDataPort;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    public TickerListCacheService(
        IMemoryCache cache,
        IHistoricalDataPort historicalDataPort)
    {
        _cache = cache;
        _historicalDataPort = historicalDataPort;
    }

    public async Task<List<string>> GetTickersAsync(TimeFrame timeFrame)
    {
        string cacheKey = GetTickersCacheKey(timeFrame);

        if (_cache.TryGetValue(cacheKey, out List<string>? cachedTickers) && cachedTickers != null)
        {
            return cachedTickers;
        }

        // Cache miss - fetch from database
        List<TickerSummary> summaries = await _historicalDataPort.GetAllTickerSummariesAsync(timeFrame);
        List<string> tickers = summaries.Select(s => s.Ticker).OrderBy(t => t).ToList();

        // Store in cache
        MemoryCacheEntryOptions cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(_cacheDuration)
            .SetSize(1);

        _cache.Set(cacheKey, tickers, cacheOptions);

        return tickers;
    }

    public async Task<List<TickerSummary>> GetTickerSummariesAsync(TimeFrame timeFrame)
    {
        string cacheKey = GetSummariesCacheKey(timeFrame);

        if (_cache.TryGetValue(cacheKey, out List<TickerSummary>? cachedSummaries) && cachedSummaries != null)
        {
            return cachedSummaries;
        }

        // Cache miss - fetch from database
        List<TickerSummary> summaries = await _historicalDataPort.GetAllTickerSummariesAsync(timeFrame);

        // Store in cache
        MemoryCacheEntryOptions cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(_cacheDuration)
            .SetSize(1);

        _cache.Set(cacheKey, summaries, cacheOptions);

        return summaries;
    }

    public void InvalidateCache(TimeFrame? timeFrame = null)
    {
        if (timeFrame == null)
        {
            // Invalidate all timeframes
            foreach (TimeFrameUnit unit in Enum.GetValues<TimeFrameUnit>())
            {
                TimeFrame tf = new() { Unit = unit };
                _cache.Remove(GetTickersCacheKey(tf));
                _cache.Remove(GetSummariesCacheKey(tf));
            }
        }
        else
        {
            // Invalidate specific timeframe
            _cache.Remove(GetTickersCacheKey(timeFrame));
            _cache.Remove(GetSummariesCacheKey(timeFrame));
        }
    }

    public async Task<int> GetTickerCountAsync(TimeFrame timeFrame)
    {
        List<string> tickers = await GetTickersAsync(timeFrame);
        return tickers.Count;
    }

    private static string GetTickersCacheKey(TimeFrame timeFrame)
    {
        return $"Tickers_{timeFrame.Unit}";
    }

    private static string GetSummariesCacheKey(TimeFrame timeFrame)
    {
        return $"TickerSummaries_{timeFrame.Unit}";
    }
}
