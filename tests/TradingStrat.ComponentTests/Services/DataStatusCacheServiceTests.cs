using FakeItEasy;
using Microsoft.Extensions.Caching.Memory;
using Shouldly;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Domain.Common;
using TradingStrat.Web.Services;
using Xunit;

using AllDataStatusResult = TradingStrat.Application.Ports.Inbound.AllDataStatusResult;
using DataStatusQuery = TradingStrat.Application.Ports.Inbound.DataStatusQuery;

namespace TradingStrat.ComponentTests.Services;

public class DataStatusCacheServiceTests : IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly IGetAllDataStatusUseCase _dataStatusUseCase;
    private readonly DataStatusCacheService _cacheService;

    public DataStatusCacheServiceTests()
    {
        MemoryCacheOptions cacheOptions = new() { SizeLimit = 100 };
        _cache = new MemoryCache(cacheOptions);
        _dataStatusUseCase = A.Fake<IGetAllDataStatusUseCase>();
        _cacheService = new DataStatusCacheService(_cache, _dataStatusUseCase);
    }

    public void Dispose()
    {
        _cache.Dispose();
    }

    [Fact]
    public async Task GetOrFetchDataStatusAsync_OnFirstCall_FetchesFromUseCase()
    {
        // Arrange
        DataStatusQuery query = new();
        AllDataStatusResult expectedResult = CreateTestResult();

        A.CallTo(() => _dataStatusUseCase.ExecuteAsync(query))
            .Returns(Result<AllDataStatusResult>.Success(expectedResult));

        // Act
        Result<AllDataStatusResult> result = await _cacheService.GetOrFetchDataStatusAsync(query);

        // Assert
        result.Value.ShouldBe(expectedResult);
        A.CallTo(() => _dataStatusUseCase.ExecuteAsync(query))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetOrFetchDataStatusAsync_OnSecondCall_ReturnsCachedResult()
    {
        // Arrange
        DataStatusQuery query = new();
        AllDataStatusResult expectedResult = CreateTestResult();

        A.CallTo(() => _dataStatusUseCase.ExecuteAsync(query))
            .Returns(Result<AllDataStatusResult>.Success(expectedResult));

        // Act - Call twice
        Result<AllDataStatusResult> result1 = await _cacheService.GetOrFetchDataStatusAsync(query);
        Result<AllDataStatusResult> result2 = await _cacheService.GetOrFetchDataStatusAsync(query);

        // Assert
        result1.Value.ShouldBe(expectedResult);
        result2.Value.ShouldBe(expectedResult);
        result1.Value.ShouldBeSameAs(result2.Value); // Same instance from cache

        // Use case should only be called once
        A.CallTo(() => _dataStatusUseCase.ExecuteAsync(query))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetOrFetchDataStatusAsync_WithDifferentQueries_CachesSeparately()
    {
        // Arrange
        DataStatusQuery query1 = new(PageNumber: 1);
        DataStatusQuery query2 = new(PageNumber: 2);

        AllDataStatusResult result1 = CreateTestResult(page: 1);
        AllDataStatusResult result2 = CreateTestResult(page: 2);

        A.CallTo(() => _dataStatusUseCase.ExecuteAsync(query1))
            .Returns(Result<AllDataStatusResult>.Success(result1));
        A.CallTo(() => _dataStatusUseCase.ExecuteAsync(query2))
            .Returns(Result<AllDataStatusResult>.Success(result2));

        // Act
        Result<AllDataStatusResult> cached1 = await _cacheService.GetOrFetchDataStatusAsync(query1);
        Result<AllDataStatusResult> cached2 = await _cacheService.GetOrFetchDataStatusAsync(query2);

        // Assert
        cached1.Value.CurrentPage.ShouldBe(1);
        cached2.Value.CurrentPage.ShouldBe(2);

        // Both queries should have been executed once
        A.CallTo(() => _dataStatusUseCase.ExecuteAsync(query1))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _dataStatusUseCase.ExecuteAsync(query2))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetOrFetchDataStatusAsync_WithNullQuery_UsesDefaultQuery()
    {
        // Arrange
        AllDataStatusResult expectedResult = CreateTestResult();

        A.CallTo(() => _dataStatusUseCase.ExecuteAsync(A<DataStatusQuery>._))
            .Returns(Result<AllDataStatusResult>.Success(expectedResult));

        // Act
        Result<AllDataStatusResult> result = await _cacheService.GetOrFetchDataStatusAsync(null);

        // Assert
        result.Value.ShouldBe(expectedResult);
        A.CallTo(() => _dataStatusUseCase.ExecuteAsync(A<DataStatusQuery>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task InvalidateCache_UpdatesLastInvalidationTime()
    {
        // Arrange
        DataStatusQuery query = new();
        AllDataStatusResult result = CreateTestResult();

        A.CallTo(() => _dataStatusUseCase.ExecuteAsync(query))
            .Returns(Result<AllDataStatusResult>.Success(result));

        await _cacheService.GetOrFetchDataStatusAsync(query);

        // Act
        _cacheService.InvalidateCache();

        // Assert
        CacheStatistics stats = _cacheService.GetStatistics();
        stats.LastInvalidation.ShouldNotBeNull();
        stats.LastInvalidation.Value.ShouldBeGreaterThan(DateTime.UtcNow.AddSeconds(-1));
    }

    [Fact]
    public async Task GetStatistics_TracksCacheHitsCorrectly()
    {
        // Arrange
        DataStatusQuery query = new();
        AllDataStatusResult result = CreateTestResult();

        A.CallTo(() => _dataStatusUseCase.ExecuteAsync(query))
            .Returns(Result<AllDataStatusResult>.Success(result));

        // Act - 1 miss, 2 hits
        await _cacheService.GetOrFetchDataStatusAsync(query); // Cache miss
        await _cacheService.GetOrFetchDataStatusAsync(query); // Cache hit
        await _cacheService.GetOrFetchDataStatusAsync(query); // Cache hit

        // Assert
        CacheStatistics stats = _cacheService.GetStatistics();
        stats.TotalRequests.ShouldBe(3);
        stats.CacheHits.ShouldBe(2);
        stats.CacheMisses.ShouldBe(1);
        stats.HitRate.ShouldBe(66.67); // 2/3 * 100 = 66.67%
    }

    [Fact]
    public async Task GetStatistics_TracksCacheMissesCorrectly()
    {
        // Arrange
        DataStatusQuery query1 = new(PageNumber: 1);
        DataStatusQuery query2 = new(PageNumber: 2);
        DataStatusQuery query3 = new(PageNumber: 3);

        A.CallTo(() => _dataStatusUseCase.ExecuteAsync(A<DataStatusQuery>._))
            .Returns(Result<AllDataStatusResult>.Success(CreateTestResult()));

        // Act - 3 different queries = 3 misses
        await _cacheService.GetOrFetchDataStatusAsync(query1);
        await _cacheService.GetOrFetchDataStatusAsync(query2);
        await _cacheService.GetOrFetchDataStatusAsync(query3);

        // Assert
        CacheStatistics stats = _cacheService.GetStatistics();
        stats.TotalRequests.ShouldBe(3);
        stats.CacheHits.ShouldBe(0);
        stats.CacheMisses.ShouldBe(3);
        stats.HitRate.ShouldBe(0);
    }

    [Fact]
    public Task GetStatistics_WithZeroRequests_ReturnsZeroHitRate()
    {
        // Act
        CacheStatistics stats = _cacheService.GetStatistics();

        // Assert
        stats.TotalRequests.ShouldBe(0);
        stats.CacheHits.ShouldBe(0);
        stats.CacheMisses.ShouldBe(0);
        stats.HitRate.ShouldBe(0);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetOrFetchDataStatusAsync_WithSameQueryParameters_GeneratesSameCacheKey()
    {
        // Arrange
        DataStatusQuery query1 = new(PageNumber: 1, PageSize: 25);
        DataStatusQuery query2 = new(PageNumber: 1, PageSize: 25);

        AllDataStatusResult result = CreateTestResult();

        A.CallTo(() => _dataStatusUseCase.ExecuteAsync(A<DataStatusQuery>._))
            .Returns(Result<AllDataStatusResult>.Success(result));

        // Act
        await _cacheService.GetOrFetchDataStatusAsync(query1);
        await _cacheService.GetOrFetchDataStatusAsync(query2);

        // Assert - Should only call use case once (cache hit on second call)
        A.CallTo(() => _dataStatusUseCase.ExecuteAsync(A<DataStatusQuery>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetOrFetchDataStatusAsync_WithSearchFilter_CachesSeparately()
    {
        // Arrange
        DataStatusQuery queryWithSearch = new(SearchTicker: "AAPL");
        DataStatusQuery queryWithoutSearch = new();

        AllDataStatusResult resultWithSearch = CreateTestResult();
        AllDataStatusResult resultWithoutSearch = CreateTestResult();

        A.CallTo(() => _dataStatusUseCase.ExecuteAsync(queryWithSearch))
            .Returns(Result<AllDataStatusResult>.Success(resultWithSearch));
        A.CallTo(() => _dataStatusUseCase.ExecuteAsync(queryWithoutSearch))
            .Returns(Result<AllDataStatusResult>.Success(resultWithoutSearch));

        // Act
        await _cacheService.GetOrFetchDataStatusAsync(queryWithSearch);
        await _cacheService.GetOrFetchDataStatusAsync(queryWithoutSearch);

        // Assert - Both queries should be executed (different cache keys)
        A.CallTo(() => _dataStatusUseCase.ExecuteAsync(queryWithSearch))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _dataStatusUseCase.ExecuteAsync(queryWithoutSearch))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetStatistics_AfterInvalidation_RetainsStats()
    {
        // Arrange
        DataStatusQuery query = new();
        AllDataStatusResult result = CreateTestResult();

        A.CallTo(() => _dataStatusUseCase.ExecuteAsync(query))
            .Returns(Result<AllDataStatusResult>.Success(result));

        await _cacheService.GetOrFetchDataStatusAsync(query);
        await _cacheService.GetOrFetchDataStatusAsync(query);

        // Act
        _cacheService.InvalidateCache();
        CacheStatistics stats = _cacheService.GetStatistics();

        // Assert - Stats should be retained after invalidation
        stats.TotalRequests.ShouldBe(2);
        stats.CacheHits.ShouldBe(1);
        stats.CacheMisses.ShouldBe(1);
    }

    private AllDataStatusResult CreateTestResult(int page = 1)
    {
        return new AllDataStatusResult(
            TotalTickers: 30,
            TotalRecords: 1000,
            AverageCoveragePercentage: 95.5m,
            TickerStatuses: new List<TickerDataStatus>(),
            TotalPages: 2,
            CurrentPage: page,
            PageSize: 25);
    }
}
