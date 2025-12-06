using Microsoft.EntityFrameworkCore;
using Shouldly;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Entities;
using TradingStrat.Infrastructure.Persistence.EfCore;

namespace TradingStrat.Infrastructure.Tests.Persistence;

/// <summary>
/// Integration tests for HistoricalDataRepository using SQLite in-memory database.
/// These tests verify actual database operations.
/// </summary>
public class HistoricalDataRepositoryTests : IDisposable
{
    private readonly TradingContext _context;
    private readonly HistoricalDataRepository _repository;

    public HistoricalDataRepositoryTests()
    {
        DbContextOptions<TradingContext> options = new DbContextOptionsBuilder<TradingContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _context = new TradingContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _repository = new HistoricalDataRepository(_context);
    }

    [Fact]
    public async Task SaveHistoricalDataAsync_WithNewData_ShouldSaveToDatabase()
    {
        // Arrange
        var data = new List<HistoricalPrice>
        {
            new()
            {
                Ticker = "TEST",
                DateTime = new DateTime(2024, 1, 1),
                Open = 100m,
                High = 105m,
                Low = 95m,
                Close = 102m,
                AdjustedClose = 102m,
                Volume = 1000000
            },
            new()
            {
                Ticker = "TEST",
                DateTime = new DateTime(2024, 1, 2),
                Open = 102m,
                High = 107m,
                Low = 101m,
                Close = 105m,
                AdjustedClose = 105m,
                Volume = 1200000
            }
        };

        // Act
        await _repository.SaveHistoricalDataAsync("TEST", "TEST_ISIN", data);

        // Assert
        List<HistoricalPrice> savedData = await _repository.GetHistoricalDataAsync("TEST");
        savedData.Count.ShouldBe(2);
        foreach (HistoricalPrice p in savedData)
        {
            p.Ticker.ShouldBe("TEST");
            p.ISIN.ShouldBe("TEST_ISIN");
        }
    }

    [Fact]
    public async Task SaveHistoricalDataAsync_WithDuplicates_ShouldNotInsertDuplicates()
    {
        // Arrange
        var initialData = new List<HistoricalPrice>
        {
            new()
            {
                Ticker = "TEST",
                DateTime = new DateTime(2024, 1, 1),
                Close = 100m
            }
        };

        await _repository.SaveHistoricalDataAsync("TEST", null, initialData);

        var duplicateData = new List<HistoricalPrice>
        {
            new()
            {
                Ticker = "TEST",
                DateTime = new DateTime(2024, 1, 1),  // Same date
                Close = 101m  // Different value
            },
            new()
            {
                Ticker = "TEST",
                DateTime = new DateTime(2024, 1, 2),
                Close = 102m
            }
        };

        // Act
        await _repository.SaveHistoricalDataAsync("TEST", null, duplicateData);

        // Assert
        List<HistoricalPrice> savedData = await _repository.GetHistoricalDataAsync("TEST");
        savedData.Count.ShouldBe(2);  // Should have 2 records (1 original + 1 new)

        // Original record should remain unchanged
        HistoricalPrice jan1Record = savedData.First(p => p.DateTime == new DateTime(2024, 1, 1));
        jan1Record.Close.ShouldBe(100m);
    }

    [Fact]
    public async Task GetLatestDataDateAsync_WhenDataExists_ShouldReturnLatestDate()
    {
        // Arrange
        var data = new List<HistoricalPrice>
        {
            new() { Ticker = "TEST", DateTime = new DateTime(2024, 1, 1), Close = 100m },
            new() { Ticker = "TEST", DateTime = new DateTime(2024, 1, 3), Close = 102m },
            new() { Ticker = "TEST", DateTime = new DateTime(2024, 1, 2), Close = 101m }
        };

        await _repository.SaveHistoricalDataAsync("TEST", null, data);

        // Act
        DateTime? latestDate = await _repository.GetLatestDataDateAsync("TEST");

        // Assert
        latestDate.ShouldBe(new DateTime(2024, 1, 3));
    }

    [Fact]
    public async Task GetLatestDataDateAsync_WhenNoData_ShouldReturnNull()
    {
        // Act
        DateTime? latestDate = await _repository.GetLatestDataDateAsync("NODATA");

        // Assert
        latestDate.ShouldBeNull();
    }

    [Fact]
    public async Task GetHistoricalDataAsync_WithDateRange_ShouldFilterCorrectly()
    {
        // Arrange
        var data = new List<HistoricalPrice>();
        for (int i = 1; i <= 10; i++)
        {
            data.Add(new HistoricalPrice
            {
                Ticker = "TEST",
                DateTime = new DateTime(2024, 1, i),
                Close = 100m + i
            });
        }

        await _repository.SaveHistoricalDataAsync("TEST", null, data);

        // Act
        List<HistoricalPrice> filtered = await _repository.GetHistoricalDataAsync(
            "TEST",
            new DateTime(2024, 1, 3),
            new DateTime(2024, 1, 7));

        // Assert
        filtered.Count.ShouldBe(5);
        filtered.First().DateTime.ShouldBe(new DateTime(2024, 1, 3));
        filtered.Last().DateTime.ShouldBe(new DateTime(2024, 1, 7));
    }

    [Fact]
    public async Task GetDataSummaryAsync_WhenDataExists_ShouldReturnCorrectSummary()
    {
        // Arrange
        var data = new List<HistoricalPrice>
        {
            new()
            {
                Ticker = "TEST",
                DateTime = new DateTime(2024, 1, 1),
                High = 110m,
                Low = 95m,
                Close = 100m
            },
            new()
            {
                Ticker = "TEST",
                DateTime = new DateTime(2024, 1, 2),
                High = 115m,
                Low = 100m,
                Close = 105m
            },
            new()
            {
                Ticker = "TEST",
                DateTime = new DateTime(2024, 1, 3),
                High = 108m,
                Low = 98m,
                Close = 102m
            }
        };

        await _repository.SaveHistoricalDataAsync("TEST", "TEST_ISIN", data);

        // Act
        DataSummaryResult summary = await _repository.GetDataSummaryAsync("TEST");

        // Assert
        summary.Ticker.ShouldBe("TEST");
        summary.ISIN.ShouldBe("TEST_ISIN");
        summary.TotalRecords.ShouldBe(3);
        summary.OldestDate.ShouldBe(new DateTime(2024, 1, 1));
        summary.LatestDate.ShouldBe(new DateTime(2024, 1, 3));
        summary.MinPrice.ShouldBe(95m);
        summary.MaxPrice.ShouldBe(115m);
        summary.LatestClose.ShouldBe(102m);
    }

    [Fact]
    public async Task GetDataSummaryAsync_WhenNoData_ShouldReturnEmptySummary()
    {
        // Act
        DataSummaryResult summary = await _repository.GetDataSummaryAsync("NODATA");

        // Assert
        summary.Ticker.ShouldBe("NODATA");
        summary.TotalRecords.ShouldBe(0);
        summary.OldestDate.ShouldBeNull();
        summary.LatestDate.ShouldBeNull();
    }

    [Fact]
    public async Task SaveAndRetrieve_MultipleTickers_ShouldKeepSeparate()
    {
        // Arrange
        var ticker1Data = new List<HistoricalPrice>
        {
            new() { Ticker = "TICKER1", DateTime = new DateTime(2024, 1, 1), Close = 100m }
        };

        var ticker2Data = new List<HistoricalPrice>
        {
            new() { Ticker = "TICKER2", DateTime = new DateTime(2024, 1, 1), Close = 200m }
        };

        // Act
        await _repository.SaveHistoricalDataAsync("TICKER1", null, ticker1Data);
        await _repository.SaveHistoricalDataAsync("TICKER2", null, ticker2Data);

        // Assert
        List<HistoricalPrice> ticker1Retrieved = await _repository.GetHistoricalDataAsync("TICKER1");
        List<HistoricalPrice> ticker2Retrieved = await _repository.GetHistoricalDataAsync("TICKER2");

        ticker1Retrieved.Count.ShouldBe(1);
        ticker1Retrieved[0].Close.ShouldBe(100m);

        ticker2Retrieved.Count.ShouldBe(1);
        ticker2Retrieved[0].Close.ShouldBe(200m);
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }
}
