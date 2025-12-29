using Shouldly;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Tests.Services;

/// <summary>
/// Tests for DataCoverageService domain service.
/// Verifies gap detection, coverage calculation, and data freshness logic.
/// </summary>
public class DataCoverageServiceTests
{
    private readonly DataCoverageService _service;

    public DataCoverageServiceTests()
    {
        _service = new DataCoverageService();
    }

    #region DetectGaps Tests

    [Fact]
    public void DetectGaps_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        List<HistoricalPrice> prices = [];

        // Act
        List<DateGap> result = _service.DetectGaps(prices);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void DetectGaps_WithConsecutiveDays_ReturnsNoGaps()
    {
        // Arrange
        var prices = new List<HistoricalPrice>
        {
            new() { Ticker = "TEST", DateTime = new DateTime(2024, 1, 1), Close = 100m },
            new() { Ticker = "TEST", DateTime = new DateTime(2024, 1, 2), Close = 101m },
            new() { Ticker = "TEST", DateTime = new DateTime(2024, 1, 3), Close = 102m }
        };

        // Act
        List<DateGap> result = _service.DetectGaps(prices);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void DetectGaps_WithSmallGap_ReturnsNoGaps()
    {
        // Arrange - 3 day gap is considered acceptable (weekends)
        var prices = new List<HistoricalPrice>
        {
            new() { Ticker = "TEST", DateTime = new DateTime(2024, 1, 1), Close = 100m },
            new() { Ticker = "TEST", DateTime = new DateTime(2024, 1, 5), Close = 101m } // 3 days between (2,3,4)
        };

        // Act
        List<DateGap> result = _service.DetectGaps(prices);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void DetectGaps_WithLargeGap_ReturnsGap()
    {
        // Arrange - 5 day gap exceeds threshold
        var prices = new List<HistoricalPrice>
        {
            new() { Ticker = "TEST", DateTime = new DateTime(2024, 1, 1), Close = 100m },
            new() { Ticker = "TEST", DateTime = new DateTime(2024, 1, 7), Close = 101m } // 5 days between (2,3,4,5,6)
        };

        // Act
        List<DateGap> result = _service.DetectGaps(prices);

        // Assert
        result.Count.ShouldBe(1);
        result[0].StartDate.ShouldBe(new DateTime(2024, 1, 2));
        result[0].EndDate.ShouldBe(new DateTime(2024, 1, 6));
        result[0].DaysMissing.ShouldBe(5);
    }

    [Fact]
    public void DetectGaps_WithMultipleGaps_ReturnsAllGaps()
    {
        // Arrange
        var prices = new List<HistoricalPrice>
        {
            new() { Ticker = "TEST", DateTime = new DateTime(2024, 1, 1), Close = 100m },
            new() { Ticker = "TEST", DateTime = new DateTime(2024, 1, 10), Close = 101m }, // Gap 1: 8 days
            new() { Ticker = "TEST", DateTime = new DateTime(2024, 1, 11), Close = 102m },
            new() { Ticker = "TEST", DateTime = new DateTime(2024, 1, 20), Close = 103m }  // Gap 2: 8 days
        };

        // Act
        List<DateGap> result = _service.DetectGaps(prices);

        // Assert
        result.Count.ShouldBe(2);
        result[0].DaysMissing.ShouldBe(8);
        result[1].DaysMissing.ShouldBe(8);
    }

    [Fact]
    public void DetectGaps_WithUnsortedData_SortsBeforeDetecting()
    {
        // Arrange - dates out of order
        var prices = new List<HistoricalPrice>
        {
            new() { Ticker = "TEST", DateTime = new DateTime(2024, 1, 10), Close = 101m },
            new() { Ticker = "TEST", DateTime = new DateTime(2024, 1, 1), Close = 100m },
            new() { Ticker = "TEST", DateTime = new DateTime(2024, 1, 20), Close = 102m }
        };

        // Act
        List<DateGap> result = _service.DetectGaps(prices);

        // Assert
        result.Count.ShouldBe(2);
        result[0].StartDate.ShouldBe(new DateTime(2024, 1, 2));
        result[1].StartDate.ShouldBe(new DateTime(2024, 1, 11));
    }

    #endregion

    #region CalculateCoverage Tests

    [Fact]
    public void CalculateCoverage_WithNullDates_ReturnsZero()
    {
        // Act
        decimal result = _service.CalculateCoverage(100, null, null);

        // Assert
        result.ShouldBe(0m);
    }

    [Fact]
    public void CalculateCoverage_WithNullOldestDate_ReturnsZero()
    {
        // Act
        decimal result = _service.CalculateCoverage(100, null, DateTime.Today);

        // Assert
        result.ShouldBe(0m);
    }

    [Fact]
    public void CalculateCoverage_WithNullLatestDate_ReturnsZero()
    {
        // Act
        decimal result = _service.CalculateCoverage(100, DateTime.Today.AddDays(-10), null);

        // Assert
        result.ShouldBe(0m);
    }

    [Fact]
    public void CalculateCoverage_WithPerfectCoverage_Returns100()
    {
        // Arrange
        DateTime oldest = new DateTime(2024, 1, 1);
        DateTime latest = new DateTime(2024, 1, 10);
        int daysCovered = 10; // 10 days of data for 10 day range

        // Act
        decimal result = _service.CalculateCoverage(daysCovered, oldest, latest);

        // Assert
        result.ShouldBe(100m);
    }

    [Fact]
    public void CalculateCoverage_WithPartialCoverage_ReturnsCorrectPercentage()
    {
        // Arrange
        DateTime oldest = new DateTime(2024, 1, 1);
        DateTime latest = new DateTime(2024, 1, 10);
        int daysCovered = 5; // 5 days of data for 10 day range

        // Act
        decimal result = _service.CalculateCoverage(daysCovered, oldest, latest);

        // Assert
        result.ShouldBe(50m);
    }

    [Fact]
    public void CalculateCoverage_WithSingleDay_Returns100()
    {
        // Arrange
        DateTime sameDay = new DateTime(2024, 1, 1);
        int daysCovered = 1;

        // Act
        decimal result = _service.CalculateCoverage(daysCovered, sameDay, sameDay);

        // Assert
        result.ShouldBe(100m);
    }

    [Fact]
    public void CalculateCoverage_WithZeroDaysCovered_ReturnsZero()
    {
        // Arrange
        DateTime oldest = new DateTime(2024, 1, 1);
        DateTime latest = new DateTime(2024, 1, 10);
        int daysCovered = 0;

        // Act
        decimal result = _service.CalculateCoverage(daysCovered, oldest, latest);

        // Assert
        result.ShouldBe(0m);
    }

    #endregion

    #region CalculateDataCoveragePercentage Tests

    [Fact]
    public void CalculateDataCoveragePercentage_WithEmptyList_ReturnsZero()
    {
        // Arrange
        var summaries = new List<TickerSummary>();

        // Act
        decimal result = _service.CalculateDataCoveragePercentage(summaries);

        // Assert
        result.ShouldBe(0m);
    }

    [Fact]
    public void CalculateDataCoveragePercentage_WithAllRecentData_Returns100()
    {
        // Arrange
        var summaries = new List<TickerSummary>
        {
            new("AAPL", "US0378331005", 100, DateTime.Today.AddDays(-365), DateTime.Today),
            new("MSFT", "US5949181045", 100, DateTime.Today.AddDays(-365), DateTime.Today.AddDays(-1)),
            new("GOOGL", "US02079K3059", 100, DateTime.Today.AddDays(-365), DateTime.Today.AddDays(-3))
        };

        // Act
        decimal result = _service.CalculateDataCoveragePercentage(summaries);

        // Assert
        result.ShouldBe(100m);
    }

    [Fact]
    public void CalculateDataCoveragePercentage_WithSomeStaleData_ReturnsCorrectPercentage()
    {
        // Arrange
        var summaries = new List<TickerSummary>
        {
            new("AAPL", "US0378331005", 100, DateTime.Today.AddDays(-365), DateTime.Today), // Recent
            new("MSFT", "US5949181045", 100, DateTime.Today.AddDays(-365), DateTime.Today.AddDays(-10)), // Stale
            new("GOOGL", "US02079K3059", 100, DateTime.Today.AddDays(-365), DateTime.Today.AddDays(-5)), // Recent
            new("AMZN", "US0231351067", 100, DateTime.Today.AddDays(-365), DateTime.Today.AddDays(-30)) // Stale
        };

        // Act
        decimal result = _service.CalculateDataCoveragePercentage(summaries);

        // Assert
        result.ShouldBe(50m); // 2 out of 4 have recent data
    }

    [Fact]
    public void CalculateDataCoveragePercentage_WithNullLatestDates_ExcludesFromCount()
    {
        // Arrange
        var summaries = new List<TickerSummary>
        {
            new("AAPL", "US0378331005", 100, DateTime.Today.AddDays(-365), DateTime.Today),
            new("MSFT", "US5949181045", 0, null, null), // No data
            new("GOOGL", "US02079K3059", 100, DateTime.Today.AddDays(-365), DateTime.Today)
        };

        // Act
        decimal result = _service.CalculateDataCoveragePercentage(summaries);

        // Assert
        result.ShouldBe(66.66666666666666666666666667m); // 2 out of 3
    }

    [Fact]
    public void CalculateDataCoveragePercentage_WithAllStaleData_ReturnsZero()
    {
        // Arrange
        var summaries = new List<TickerSummary>
        {
            new("AAPL", "US0378331005", 100, DateTime.Today.AddDays(-365), DateTime.Today.AddDays(-30)),
            new("MSFT", "US5949181045", 100, DateTime.Today.AddDays(-365), DateTime.Today.AddDays(-60)),
            new("GOOGL", "US02079K3059", 100, DateTime.Today.AddDays(-365), DateTime.Today.AddDays(-90))
        };

        // Act
        decimal result = _service.CalculateDataCoveragePercentage(summaries);

        // Assert
        result.ShouldBe(0m);
    }

    [Fact]
    public void CalculateDataCoveragePercentage_WithExactly7DaysOld_IncludesInCount()
    {
        // Arrange - 7 days ago should still be considered recent
        var summaries = new List<TickerSummary>
        {
            new("AAPL", "US0378331005", 100, DateTime.Today.AddDays(-365), DateTime.Today.AddDays(-7))
        };

        // Act
        decimal result = _service.CalculateDataCoveragePercentage(summaries);

        // Assert
        result.ShouldBe(100m);
    }

    [Fact]
    public void CalculateDataCoveragePercentage_With8DaysOld_ExcludesFromCount()
    {
        // Arrange - 8 days ago is too old
        var summaries = new List<TickerSummary>
        {
            new("AAPL", "US0378331005", 100, DateTime.Today.AddDays(-365), DateTime.Today.AddDays(-8))
        };

        // Act
        decimal result = _service.CalculateDataCoveragePercentage(summaries);

        // Assert
        result.ShouldBe(0m);
    }

    #endregion
}
