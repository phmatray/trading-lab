using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;
using TradingStrat.Infrastructure.Persistence.EfCore;

namespace TradingStrat.Infrastructure.Tests.Persistence;

/// <summary>
/// Tests for bulk operations and delete functionality in HistoricalDataRepository.
/// Uses SQLite in-memory database for fast, isolated tests.
/// </summary>
public class HistoricalDataRepositoryBulkTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TradingContext _context;
    private readonly HistoricalDataRepository _repository;

    public HistoricalDataRepositoryBulkTests()
    {
        // Create in-memory SQLite connection
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Configure DbContext with in-memory database
        DbContextOptions<TradingContext> options = new DbContextOptionsBuilder<TradingContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new TradingContext(options);
        _context.Database.EnsureCreated();

        _repository = new HistoricalDataRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    #region GetDataSummariesAsync Tests

    [Fact]
    public async Task GetDataSummariesAsync_WithMultipleTickers_ReturnsAllSummaries()
    {
        // Arrange
        var timeFrame = new TimeFrame { Unit = TimeFrameUnit.D1 };

        // Seed data for multiple tickers
        await _repository.SaveHistoricalDataAsync("AAPL", "US0378331005", timeFrame, new[]
        {
            new HistoricalPrice { Ticker = "AAPL", DateTime = DateTime.Today.AddDays(-2), Close = 150m },
            new HistoricalPrice { Ticker = "AAPL", DateTime = DateTime.Today.AddDays(-1), Close = 155m }
        });

        await _repository.SaveHistoricalDataAsync("MSFT", "US5949181045", timeFrame, new[]
        {
            new HistoricalPrice { Ticker = "MSFT", DateTime = DateTime.Today.AddDays(-3), Close = 300m },
            new HistoricalPrice { Ticker = "MSFT", DateTime = DateTime.Today.AddDays(-2), Close = 305m },
            new HistoricalPrice { Ticker = "MSFT", DateTime = DateTime.Today.AddDays(-1), Close = 310m }
        });

        await _repository.SaveHistoricalDataAsync("GOOGL", "US02079K3059", timeFrame, new[]
        {
            new HistoricalPrice { Ticker = "GOOGL", DateTime = DateTime.Today.AddDays(-1), Close = 2800m }
        });

        string[] tickers = new[] { "AAPL", "MSFT", "GOOGL" };

        // Act
        Dictionary<string, DataSummaryResult> summaries = await _repository.GetDataSummariesAsync(tickers, timeFrame);

        // Assert
        summaries.Count.ShouldBe(3);
        summaries.ShouldContainKey("AAPL");
        summaries.ShouldContainKey("MSFT");
        summaries.ShouldContainKey("GOOGL");

        summaries["AAPL"].TotalRecords.ShouldBe(2);
        summaries["AAPL"].ISIN.ShouldBe("US0378331005");

        summaries["MSFT"].TotalRecords.ShouldBe(3);
        summaries["MSFT"].ISIN.ShouldBe("US5949181045");

        summaries["GOOGL"].TotalRecords.ShouldBe(1);
        summaries["GOOGL"].ISIN.ShouldBe("US02079K3059");
    }

    [Fact]
    public async Task GetDataSummariesAsync_WithNonExistentTickers_ReturnsEmptySummaries()
    {
        // Arrange
        var timeFrame = new TimeFrame { Unit = TimeFrameUnit.D1 };
        string[] tickers = new[] { "NONEXISTENT", "FAKETIC" };

        // Act
        Dictionary<string, DataSummaryResult> summaries = await _repository.GetDataSummariesAsync(tickers, timeFrame);

        // Assert
        summaries.Count.ShouldBe(2);
        summaries["NONEXISTENT"].TotalRecords.ShouldBe(0);
        summaries["FAKETIC"].TotalRecords.ShouldBe(0);
    }

    #endregion

    #region BulkSaveHistoricalDataAsync Tests

    [Fact]
    public async Task BulkSaveHistoricalDataAsync_WithMultipleTickers_SavesAllData()
    {
        // Arrange
        var timeFrame = new TimeFrame { Unit = TimeFrameUnit.D1 };
        var tickerDataMap = new Dictionary<string, (string? isin, IEnumerable<HistoricalPrice> data)>
        {
            ["AAPL"] = ("US0378331005", new[]
            {
                new HistoricalPrice { Ticker = "AAPL", DateTime = DateTime.Today.AddDays(-2), Close = 150m },
                new HistoricalPrice { Ticker = "AAPL", DateTime = DateTime.Today.AddDays(-1), Close = 155m }
            }),
            ["MSFT"] = ("US5949181045", new[]
            {
                new HistoricalPrice { Ticker = "MSFT", DateTime = DateTime.Today.AddDays(-2), Close = 300m },
                new HistoricalPrice { Ticker = "MSFT", DateTime = DateTime.Today.AddDays(-1), Close = 305m }
            }),
            ["GOOGL"] = ("US02079K3059", new[]
            {
                new HistoricalPrice { Ticker = "GOOGL", DateTime = DateTime.Today.AddDays(-1), Close = 2800m }
            })
        };

        // Act
        await _repository.BulkSaveHistoricalDataAsync(tickerDataMap, timeFrame);

        // Assert
        List<HistoricalPrice> aaplData = await _repository.GetHistoricalDataAsync("AAPL", timeFrame);
        aaplData.Count.ShouldBe(2);

        List<HistoricalPrice> msftData = await _repository.GetHistoricalDataAsync("MSFT", timeFrame);
        msftData.Count.ShouldBe(2);

        List<HistoricalPrice> googlData = await _repository.GetHistoricalDataAsync("GOOGL", timeFrame);
        googlData.Count.ShouldBe(1);
    }

    [Fact]
    public async Task BulkSaveHistoricalDataAsync_ReportsProgress()
    {
        // Arrange
        var timeFrame = new TimeFrame { Unit = TimeFrameUnit.D1 };
        var tickerDataMap = new Dictionary<string, (string? isin, IEnumerable<HistoricalPrice> data)>
        {
            ["AAPL"] = (null, new[] { new HistoricalPrice { Ticker = "AAPL", DateTime = DateTime.Today, Close = 150m } }),
            ["MSFT"] = (null, new[] { new HistoricalPrice { Ticker = "MSFT", DateTime = DateTime.Today, Close = 300m } }),
            ["GOOGL"] = (null, new[] { new HistoricalPrice { Ticker = "GOOGL", DateTime = DateTime.Today, Close = 2800m } })
        };

        var progressReports = new List<BulkSaveProgress>();
        var progress = new Progress<BulkSaveProgress>(p => progressReports.Add(p));

        // Act
        await _repository.BulkSaveHistoricalDataAsync(tickerDataMap, timeFrame, progress);

        // Assert
        progressReports.Count.ShouldBeGreaterThan(0);
        progressReports.Last().TotalTickers.ShouldBe(3);
        progressReports.Last().CompletedTickers.ShouldBe(3);
        progressReports.Last().TotalRecordsSaved.ShouldBe(3);
    }

    [Fact]
    public async Task BulkSaveHistoricalDataAsync_WithDuplicates_FiltersDuplicates()
    {
        // Arrange
        var timeFrame = new TimeFrame { Unit = TimeFrameUnit.D1 };
        DateTime date = DateTime.Today.AddDays(-1);

        // First save
        var firstBatch = new Dictionary<string, (string? isin, IEnumerable<HistoricalPrice> data)>
        {
            ["AAPL"] = (null, new[]
            {
                new HistoricalPrice { Ticker = "AAPL", DateTime = date, Close = 150m }
            })
        };
        await _repository.BulkSaveHistoricalDataAsync(firstBatch, timeFrame);

        // Second save with duplicate
        var secondBatch = new Dictionary<string, (string? isin, IEnumerable<HistoricalPrice> data)>
        {
            ["AAPL"] = (null, new[]
            {
                new HistoricalPrice { Ticker = "AAPL", DateTime = date, Close = 155m }, // Duplicate date
                new HistoricalPrice { Ticker = "AAPL", DateTime = DateTime.Today, Close = 160m } // New date
            })
        };

        // Act
        await _repository.BulkSaveHistoricalDataAsync(secondBatch, timeFrame);

        // Assert
        List<HistoricalPrice> data = await _repository.GetHistoricalDataAsync("AAPL", timeFrame);
        data.Count.ShouldBe(2); // Should only have 2 records, not 3
        data[0].Close.ShouldBe(150m); // Original duplicate preserved
        data[1].Close.ShouldBe(160m); // New record added
    }

    #endregion

    #region DeleteTickerDataAsync Tests

    [Fact]
    public async Task DeleteTickerDataAsync_WithTimeFrame_DeletesOnlySpecificTimeFrame()
    {
        // Arrange
        var d1TimeFrame = new TimeFrame { Unit = TimeFrameUnit.D1 };
        var h1TimeFrame = new TimeFrame { Unit = TimeFrameUnit.H1 };

        await _repository.SaveHistoricalDataAsync("AAPL", null, d1TimeFrame, new[]
        {
            new HistoricalPrice { Ticker = "AAPL", DateTime = DateTime.Today.AddDays(-1), Close = 150m },
            new HistoricalPrice { Ticker = "AAPL", DateTime = DateTime.Today, Close = 155m }
        });

        await _repository.SaveHistoricalDataAsync("AAPL", null, h1TimeFrame, new[]
        {
            new HistoricalPrice { Ticker = "AAPL", DateTime = DateTime.Today.AddHours(-2), Close = 151m },
            new HistoricalPrice { Ticker = "AAPL", DateTime = DateTime.Today.AddHours(-1), Close = 153m }
        });

        // Act
        int deletedCount = await _repository.DeleteTickerDataAsync("AAPL", d1TimeFrame);

        // Assert
        deletedCount.ShouldBe(2);

        // D1 data should be deleted
        List<HistoricalPrice> d1Data = await _repository.GetHistoricalDataAsync("AAPL", d1TimeFrame);
        d1Data.ShouldBeEmpty();

        // H1 data should still exist
        List<HistoricalPrice> h1Data = await _repository.GetHistoricalDataAsync("AAPL", h1TimeFrame);
        h1Data.Count.ShouldBe(2);
    }

    [Fact]
    public async Task DeleteTickerDataAsync_WithoutTimeFrame_DeletesAllTimeFrames()
    {
        // Arrange
        var d1TimeFrame = new TimeFrame { Unit = TimeFrameUnit.D1 };
        var h1TimeFrame = new TimeFrame { Unit = TimeFrameUnit.H1 };

        await _repository.SaveHistoricalDataAsync("AAPL", null, d1TimeFrame, new[]
        {
            new HistoricalPrice { Ticker = "AAPL", DateTime = DateTime.Today, Close = 150m }
        });

        await _repository.SaveHistoricalDataAsync("AAPL", null, h1TimeFrame, new[]
        {
            new HistoricalPrice { Ticker = "AAPL", DateTime = DateTime.Today, Close = 151m }
        });

        // Act
        int deletedCount = await _repository.DeleteTickerDataAsync("AAPL");

        // Assert
        deletedCount.ShouldBe(2);

        List<HistoricalPrice> d1Data = await _repository.GetHistoricalDataAsync("AAPL", d1TimeFrame);
        d1Data.ShouldBeEmpty();

        List<HistoricalPrice> h1Data = await _repository.GetHistoricalDataAsync("AAPL", h1TimeFrame);
        h1Data.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteTickerDataAsync_WithNonExistentTicker_ReturnsZero()
    {
        // Arrange
        var timeFrame = new TimeFrame { Unit = TimeFrameUnit.D1 };

        // Act
        int deletedCount = await _repository.DeleteTickerDataAsync("NONEXISTENT", timeFrame);

        // Assert
        deletedCount.ShouldBe(0);
    }

    #endregion

    #region DeleteDateRangeAsync Tests

    [Fact]
    public async Task DeleteDateRangeAsync_WithValidRange_DeletesOnlyRecordsInRange()
    {
        // Arrange
        var timeFrame = new TimeFrame { Unit = TimeFrameUnit.D1 };
        DateTime baseDate = DateTime.Today;

        await _repository.SaveHistoricalDataAsync("AAPL", null, timeFrame, new[]
        {
            new HistoricalPrice { Ticker = "AAPL", DateTime = baseDate.AddDays(-5), Close = 140m },
            new HistoricalPrice { Ticker = "AAPL", DateTime = baseDate.AddDays(-4), Close = 145m },
            new HistoricalPrice { Ticker = "AAPL", DateTime = baseDate.AddDays(-3), Close = 150m },
            new HistoricalPrice { Ticker = "AAPL", DateTime = baseDate.AddDays(-2), Close = 155m },
            new HistoricalPrice { Ticker = "AAPL", DateTime = baseDate.AddDays(-1), Close = 160m }
        });

        DateTime startDate = baseDate.AddDays(-4);
        DateTime endDate = baseDate.AddDays(-2);

        // Act
        int deletedCount = await _repository.DeleteDateRangeAsync("AAPL", timeFrame, startDate, endDate);

        // Assert
        deletedCount.ShouldBe(3); // Records from -4, -3, -2 days

        List<HistoricalPrice> remainingData = await _repository.GetHistoricalDataAsync("AAPL", timeFrame);
        remainingData.Count.ShouldBe(2); // Records from -5 and -1 days
        remainingData[0].DateTime.ShouldBe(baseDate.AddDays(-5));
        remainingData[1].DateTime.ShouldBe(baseDate.AddDays(-1));
    }

    [Fact]
    public async Task DeleteDateRangeAsync_WithEmptyRange_ReturnsZero()
    {
        // Arrange
        var timeFrame = new TimeFrame { Unit = TimeFrameUnit.D1 };

        await _repository.SaveHistoricalDataAsync("AAPL", null, timeFrame, new[]
        {
            new HistoricalPrice { Ticker = "AAPL", DateTime = DateTime.Today.AddDays(-1), Close = 150m }
        });

        // Date range with no data
        DateTime startDate = DateTime.Today.AddDays(-10);
        DateTime endDate = DateTime.Today.AddDays(-9);

        // Act
        int deletedCount = await _repository.DeleteDateRangeAsync("AAPL", timeFrame, startDate, endDate);

        // Assert
        deletedCount.ShouldBe(0);

        List<HistoricalPrice> data = await _repository.GetHistoricalDataAsync("AAPL", timeFrame);
        data.Count.ShouldBe(1); // Original data preserved
    }

    [Fact]
    public async Task DeleteDateRangeAsync_WithNonExistentTicker_ReturnsZero()
    {
        // Arrange
        var timeFrame = new TimeFrame { Unit = TimeFrameUnit.D1 };
        DateTime startDate = DateTime.Today.AddDays(-5);
        DateTime endDate = DateTime.Today;

        // Act
        int deletedCount = await _repository.DeleteDateRangeAsync("NONEXISTENT", timeFrame, startDate, endDate);

        // Assert
        deletedCount.ShouldBe(0);
    }

    #endregion

    #region GetAllTickerSummariesAsync Tests

    [Fact]
    public async Task GetAllTickerSummariesAsync_WithMultipleTickers_ReturnsAllSummaries()
    {
        // Arrange
        var timeFrame = new TimeFrame { Unit = TimeFrameUnit.D1 };

        await _repository.SaveHistoricalDataAsync("AAPL", "US0378331005", timeFrame, new[]
        {
            new HistoricalPrice { Ticker = "AAPL", DateTime = DateTime.Today.AddDays(-10), Close = 140m },
            new HistoricalPrice { Ticker = "AAPL", DateTime = DateTime.Today, Close = 160m }
        });

        await _repository.SaveHistoricalDataAsync("MSFT", "US5949181045", timeFrame, new[]
        {
            new HistoricalPrice { Ticker = "AAPL", DateTime = DateTime.Today.AddDays(-5), Close = 300m },
            new HistoricalPrice { Ticker = "AAPL", DateTime = DateTime.Today.AddDays(-1), Close = 310m }
        });

        await _repository.SaveHistoricalDataAsync("GOOGL", null, timeFrame, new[]
        {
            new HistoricalPrice { Ticker = "AAPL", DateTime = DateTime.Today, Close = 2800m }
        });

        // Act
        List<TickerSummary> summaries = await _repository.GetAllTickerSummariesAsync(timeFrame);

        // Assert
        summaries.Count.ShouldBe(3);
        summaries.ShouldAllBe(s => s.RecordCount > 0);

        TickerSummary aaplSummary = summaries.First(s => s.Ticker == "AAPL");
        aaplSummary.RecordCount.ShouldBe(2);
        aaplSummary.ISIN.ShouldBe("US0378331005");
        aaplSummary.OldestDate.ShouldBe(DateTime.Today.AddDays(-10));
        aaplSummary.LatestDate.ShouldBe(DateTime.Today);

        TickerSummary msftSummary = summaries.First(s => s.Ticker == "MSFT");
        msftSummary.RecordCount.ShouldBe(2);
        msftSummary.ISIN.ShouldBe("US5949181045");

        TickerSummary googlSummary = summaries.First(s => s.Ticker == "GOOGL");
        googlSummary.RecordCount.ShouldBe(1);
        googlSummary.ISIN.ShouldBeNull();
    }

    [Fact]
    public async Task GetAllTickerSummariesAsync_FiltersByTimeFrame()
    {
        // Arrange
        var d1TimeFrame = new TimeFrame { Unit = TimeFrameUnit.D1 };
        var h1TimeFrame = new TimeFrame { Unit = TimeFrameUnit.H1 };

        await _repository.SaveHistoricalDataAsync("AAPL", null, d1TimeFrame, new[]
        {
            new HistoricalPrice { Ticker = "AAPL", DateTime = DateTime.Today, Close = 150m }
        });

        await _repository.SaveHistoricalDataAsync("MSFT", null, h1TimeFrame, new[]
        {
            new HistoricalPrice { Ticker = "AAPL", DateTime = DateTime.Today, Close = 300m }
        });

        // Act
        List<TickerSummary> d1Summaries = await _repository.GetAllTickerSummariesAsync(d1TimeFrame);
        List<TickerSummary> h1Summaries = await _repository.GetAllTickerSummariesAsync(h1TimeFrame);

        // Assert
        d1Summaries.Count.ShouldBe(1);
        d1Summaries[0].Ticker.ShouldBe("AAPL");

        h1Summaries.Count.ShouldBe(1);
        h1Summaries[0].Ticker.ShouldBe("MSFT");
    }

    [Fact]
    public async Task GetAllTickerSummariesAsync_WithNoData_ReturnsEmptyList()
    {
        // Arrange
        var timeFrame = new TimeFrame { Unit = TimeFrameUnit.D1 };

        // Act
        List<TickerSummary> summaries = await _repository.GetAllTickerSummariesAsync(timeFrame);

        // Assert
        summaries.ShouldBeEmpty();
    }

    #endregion

    #region GetDatabaseLastModifiedAsync Tests

    [Fact]
    public async Task GetDatabaseLastModifiedAsync_WithData_ReturnsLatestCreatedAt()
    {
        // Arrange
        var timeFrame = new TimeFrame { Unit = TimeFrameUnit.D1 };

        await _repository.SaveHistoricalDataAsync("AAPL", null, timeFrame, new[]
        {
            new HistoricalPrice { Ticker = "AAPL", DateTime = DateTime.Today.AddDays(-1), Close = 150m }
        });

        await Task.Delay(10); // Small delay to ensure different CreatedAt timestamps

        await _repository.SaveHistoricalDataAsync("MSFT", null, timeFrame, new[]
        {
            new HistoricalPrice { Ticker = "AAPL", DateTime = DateTime.Today, Close = 300m }
        });

        // Act
        DateTime? lastModified = await _repository.GetDatabaseLastModifiedAsync();

        // Assert
        lastModified.ShouldNotBeNull();
        lastModified.Value.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task GetDatabaseLastModifiedAsync_WithNoData_ReturnsNull()
    {
        // Act
        DateTime? lastModified = await _repository.GetDatabaseLastModifiedAsync();

        // Assert
        lastModified.ShouldBeNull();
    }

    #endregion
}
