using FakeItEasy;
using Microsoft.Extensions.Caching.Memory;
using Shouldly;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.ValueObjects;
using TradingStrat.Web.Services;
using Xunit;

namespace TradingStrat.ComponentTests.Services;

public class TickerListCacheServiceTests : IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly IHistoricalDataPort _historicalDataPort;
    private readonly TickerListCacheService _cacheService;

    public TickerListCacheServiceTests()
    {
        MemoryCacheOptions cacheOptions = new() { SizeLimit = 100 };
        _cache = new MemoryCache(cacheOptions);
        _historicalDataPort = A.Fake<IHistoricalDataPort>();
        _cacheService = new TickerListCacheService(_cache, _historicalDataPort);
    }

    public void Dispose()
    {
        _cache.Dispose();
    }

    [Fact]
    public async Task GetTickersAsync_OnFirstCall_FetchesFromDatabase()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        List<TickerSummary> summaries = CreateTestSummaries();

        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(timeFrame))
            .Returns(summaries);

        // Act
        List<string> tickers = await _cacheService.GetTickersAsync(timeFrame);

        // Assert
        tickers.ShouldNotBeEmpty();
        tickers.Count.ShouldBe(3);
        tickers.ShouldBe(new[] { "AAPL", "GOOGL", "MSFT" }); // Sorted alphabetically

        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(timeFrame))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetTickersAsync_OnSecondCall_ReturnsCachedResult()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        List<TickerSummary> summaries = CreateTestSummaries();

        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(timeFrame))
            .Returns(summaries);

        // Act - Call twice
        List<string> tickers1 = await _cacheService.GetTickersAsync(timeFrame);
        List<string> tickers2 = await _cacheService.GetTickersAsync(timeFrame);

        // Assert
        tickers1.ShouldBe(tickers2);

        // Database should only be queried once
        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(timeFrame))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetTickersAsync_WithDifferentTimeFrames_CachesSeparately()
    {
        // Arrange
        TimeFrame d1 = new() { Unit = TimeFrameUnit.D1 };
        TimeFrame h1 = new() { Unit = TimeFrameUnit.H1 };

        List<TickerSummary> d1Summaries = CreateTestSummaries();
        List<TickerSummary> h1Summaries = new()
        {
            new("TSLA", null, 100, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31))
        };

        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(d1))
            .Returns(d1Summaries);
        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(h1))
            .Returns(h1Summaries);

        // Act
        List<string> d1Tickers = await _cacheService.GetTickersAsync(d1);
        List<string> h1Tickers = await _cacheService.GetTickersAsync(h1);

        // Assert
        d1Tickers.Count.ShouldBe(3);
        h1Tickers.Count.ShouldBe(1);
        h1Tickers[0].ShouldBe("TSLA");

        // Both should be queried once
        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(d1))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(h1))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetTickerSummariesAsync_OnFirstCall_FetchesFromDatabase()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        List<TickerSummary> expectedSummaries = CreateTestSummaries();

        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(timeFrame))
            .Returns(expectedSummaries);

        // Act
        List<TickerSummary> summaries = await _cacheService.GetTickerSummariesAsync(timeFrame);

        // Assert
        summaries.ShouldBe(expectedSummaries);
        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(timeFrame))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetTickerSummariesAsync_OnSecondCall_ReturnsCachedResult()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        List<TickerSummary> expectedSummaries = CreateTestSummaries();

        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(timeFrame))
            .Returns(expectedSummaries);

        // Act - Call twice
        List<TickerSummary> summaries1 = await _cacheService.GetTickerSummariesAsync(timeFrame);
        List<TickerSummary> summaries2 = await _cacheService.GetTickerSummariesAsync(timeFrame);

        // Assert
        summaries1.ShouldBe(summaries2);
        summaries1.ShouldBeSameAs(summaries2); // Same instance from cache

        // Database should only be queried once
        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(timeFrame))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task InvalidateCache_WithSpecificTimeFrame_RemovesThatTimeFrameOnly()
    {
        // Arrange
        TimeFrame d1 = new() { Unit = TimeFrameUnit.D1 };
        TimeFrame h1 = new() { Unit = TimeFrameUnit.H1 };

        List<TickerSummary> d1Summaries = CreateTestSummaries();
        List<TickerSummary> h1Summaries = CreateTestSummaries();

        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(d1))
            .Returns(d1Summaries);
        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(h1))
            .Returns(h1Summaries);

        // Act - Cache both, then invalidate D1 only
        await _cacheService.GetTickersAsync(d1);
        await _cacheService.GetTickersAsync(h1);

        _cacheService.InvalidateCache(d1);

        await _cacheService.GetTickersAsync(d1); // Should fetch again
        await _cacheService.GetTickersAsync(h1); // Should use cache

        // Assert
        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(d1))
            .MustHaveHappened(2, Times.Exactly); // Called twice (before and after invalidation)
        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(h1))
            .MustHaveHappenedOnceExactly(); // Called once (still cached)
    }

    [Fact]
    public async Task InvalidateCache_WithNullTimeFrame_RemovesAllTimeFrames()
    {
        // Arrange
        TimeFrame d1 = new() { Unit = TimeFrameUnit.D1 };
        TimeFrame h1 = new() { Unit = TimeFrameUnit.H1 };

        List<TickerSummary> summaries = CreateTestSummaries();

        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(A<TimeFrame>._))
            .Returns(summaries);

        // Act - Cache both, then invalidate all
        await _cacheService.GetTickersAsync(d1);
        await _cacheService.GetTickersAsync(h1);

        _cacheService.InvalidateCache(); // Invalidate all

        await _cacheService.GetTickersAsync(d1);
        await _cacheService.GetTickersAsync(h1);

        // Assert - Both should be fetched twice
        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(d1))
            .MustHaveHappened(2, Times.Exactly);
        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(h1))
            .MustHaveHappened(2, Times.Exactly);
    }

    [Fact]
    public async Task GetTickerCountAsync_ReturnsCachedTickerCount()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        List<TickerSummary> summaries = CreateTestSummaries();

        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(timeFrame))
            .Returns(summaries);

        // Act
        int count = await _cacheService.GetTickerCountAsync(timeFrame);

        // Assert
        count.ShouldBe(3);
        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(timeFrame))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetTickersAsync_ReturnsTickersSortedAlphabetically()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };

        // Create summaries in non-alphabetical order
        List<TickerSummary> summaries = new()
        {
            new("MSFT", null, 250, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31)),
            new("AAPL", null, 250, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31)),
            new("GOOGL", null, 200, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31))
        };

        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(timeFrame))
            .Returns(summaries);

        // Act
        List<string> tickers = await _cacheService.GetTickersAsync(timeFrame);

        // Assert - Should be sorted
        tickers[0].ShouldBe("AAPL");
        tickers[1].ShouldBe("GOOGL");
        tickers[2].ShouldBe("MSFT");
    }

    [Fact]
    public async Task GetTickersAsync_WithEmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };

        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(timeFrame))
            .Returns(new List<TickerSummary>());

        // Act
        List<string> tickers = await _cacheService.GetTickersAsync(timeFrame);

        // Assert
        tickers.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetTickerSummariesAsync_PreservesSummaryMetadata()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        DateTime oldestDate = new(2023, 1, 1);
        DateTime latestDate = new(2023, 12, 31);

        List<TickerSummary> expectedSummaries = new()
        {
            new("AAPL", "US0378331005", 250, oldestDate, latestDate)
        };

        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(timeFrame))
            .Returns(expectedSummaries);

        // Act
        List<TickerSummary> summaries = await _cacheService.GetTickerSummariesAsync(timeFrame);

        // Assert
        summaries[0].Ticker.ShouldBe("AAPL");
        summaries[0].ISIN.ShouldBe("US0378331005");
        summaries[0].RecordCount.ShouldBe(250);
        summaries[0].OldestDate.ShouldBe(oldestDate);
        summaries[0].LatestDate.ShouldBe(latestDate);
    }

    private List<TickerSummary> CreateTestSummaries()
    {
        return new List<TickerSummary>
        {
            new("AAPL", "US0378331005", 250, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31)),
            new("MSFT", "US5949181045", 250, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31)),
            new("GOOGL", "US02079K3059", 200, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31))
        };
    }
}
