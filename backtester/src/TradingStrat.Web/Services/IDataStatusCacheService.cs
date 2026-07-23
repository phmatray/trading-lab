using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Domain.Common;

namespace TradingStrat.Web.Services;

/// <summary>
/// Service for caching data status results to improve UI responsiveness.
/// Uses in-memory caching with 5-minute TTL and automatic invalidation.
/// </summary>
public interface IDataStatusCacheService
{
    /// <summary>
    /// Gets data status result from cache or executes query if not cached.
    /// </summary>
    /// <param name="query">Query parameters for filtering, sorting, and pagination.</param>
    /// <returns>Cached or freshly-queried data status result.</returns>
    Task<Result<AllDataStatusResult>> GetOrFetchDataStatusAsync(DataStatusQuery? query = null);

    /// <summary>
    /// Invalidates the entire data status cache.
    /// Call this after bulk data operations, deletions, or imports.
    /// </summary>
    void InvalidateCache();

    /// <summary>
    /// Gets cache statistics for monitoring and debugging.
    /// </summary>
    /// <returns>Cache hit rate, entry count, and last invalidation time.</returns>
    CacheStatistics GetStatistics();
}

/// <summary>
/// Cache statistics for monitoring cache effectiveness.
/// </summary>
public sealed record CacheStatistics(
    int TotalRequests,
    int CacheHits,
    int CacheMisses,
    double HitRate,
    int EntryCount,
    DateTime? LastInvalidation);
