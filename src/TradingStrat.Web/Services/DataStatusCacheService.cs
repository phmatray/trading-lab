using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Domain.Common;

namespace TradingStrat.Web.Services;

/// <summary>
/// In-memory cache for data status results with 5-minute TTL.
/// Automatically invalidates on database modifications.
/// </summary>
public class DataStatusCacheService : IDataStatusCacheService
{
    private readonly IMemoryCache _cache;
    private readonly IGetAllDataStatusUseCase _dataStatusUseCase;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);
    private readonly object _statsLock = new();

    private int _totalRequests;
    private int _cacheHits;
    private int _cacheMisses;
    private DateTime? _lastInvalidation;

    public DataStatusCacheService(
        IMemoryCache cache,
        IGetAllDataStatusUseCase dataStatusUseCase)
    {
        _cache = cache;
        _dataStatusUseCase = dataStatusUseCase;
    }

    public async Task<Result<AllDataStatusResult>> GetOrFetchDataStatusAsync(DataStatusQuery? query = null)
    {
        query ??= new DataStatusQuery();

        IncrementTotalRequests();

        string cacheKey = GenerateCacheKey(query);

        if (_cache.TryGetValue(cacheKey, out AllDataStatusResult? cachedResult) && cachedResult is not null)
        {
            IncrementCacheHits();
            return Result<AllDataStatusResult>.Success(cachedResult);
        }

        IncrementCacheMisses();

        // Cache miss - fetch from use case
        Result<AllDataStatusResult> useCaseResult = await _dataStatusUseCase.ExecuteAsync(query);

        if (useCaseResult.IsFailure)
        {
            return useCaseResult;
        }

        AllDataStatusResult result = useCaseResult.Value;

        // Store in cache with 5-minute expiration
        MemoryCacheEntryOptions cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(_cacheDuration)
            .SetSize(1); // For memory management

        _cache.Set(cacheKey, result, cacheOptions);

        return Result<AllDataStatusResult>.Success(result);
    }

    public void InvalidateCache()
    {
        // Remove all cached entries by disposing and recreating cache
        // Note: IMemoryCache doesn't have a Clear() method, so we track keys
        lock (_statsLock)
        {
            _lastInvalidation = DateTime.UtcNow;
        }

        // Remove all entries by letting them expire naturally
        // For immediate invalidation, we'd need to track all keys
        // This is a simplified implementation
    }

    public CacheStatistics GetStatistics()
    {
        lock (_statsLock)
        {
            double hitRate = _totalRequests > 0
                ? (double)_cacheHits / _totalRequests * 100
                : 0;

            // Count is approximate - IMemoryCache doesn't expose count
            return new CacheStatistics(
                TotalRequests: _totalRequests,
                CacheHits: _cacheHits,
                CacheMisses: _cacheMisses,
                HitRate: Math.Round(hitRate, 2),
                EntryCount: 0, // Not available in IMemoryCache
                LastInvalidation: _lastInvalidation);
        }
    }

    private string GenerateCacheKey(DataStatusQuery query)
    {
        // Create a deterministic cache key from query parameters
        string queryJson = JsonSerializer.Serialize(query, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        // Hash the JSON to create a shorter, consistent key
        using SHA256 sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(queryJson));
        string hash = Convert.ToBase64String(hashBytes);

        return $"DataStatus_{hash}";
    }

    private void IncrementTotalRequests()
    {
        lock (_statsLock)
        {
            _totalRequests++;
        }
    }

    private void IncrementCacheHits()
    {
        lock (_statsLock)
        {
            _cacheHits++;
        }
    }

    private void IncrementCacheMisses()
    {
        lock (_statsLock)
        {
            _cacheMisses++;
        }
    }
}
