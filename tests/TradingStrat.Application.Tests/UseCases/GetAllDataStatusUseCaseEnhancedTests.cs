using FakeItEasy;
using Shouldly;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.UseCases;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.ValueObjects;
using DataStatusFilter = TradingStrat.Application.Ports.Inbound.DataStatusFilter;
using DataStatusQuery = TradingStrat.Application.Ports.Inbound.DataStatusQuery;
using SortColumn = TradingStrat.Application.Ports.Inbound.SortColumn;
using SortDirection = TradingStrat.Application.Ports.Inbound.SortDirection;

namespace TradingStrat.Application.Tests.UseCases;

public class GetAllDataStatusUseCaseEnhancedTests
{
    private readonly IHistoricalDataPort _historicalDataPort;
    private readonly GetAllDataStatusUseCase _useCase;

    public GetAllDataStatusUseCaseEnhancedTests()
    {
        _historicalDataPort = A.Fake<IHistoricalDataPort>();
        var dataCoverageService = new DataCoverageService();
        _useCase = new GetAllDataStatusUseCase(_historicalDataPort, dataCoverageService);
    }

    [Fact]
    public async Task ExecuteAsync_WithDefaultQuery_ReturnsAllTickersWithPagination()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        List<TickerSummary> summaries = CreateTestSummaries(30);

        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(timeFrame))
            .Returns(summaries);

        SetupHistoricalDataForSummaries(summaries, timeFrame);

        // Act (default query: page 1, 25 items per page)
        Result<AllDataStatusResult> result = await _useCase.ExecuteAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Value.TotalTickers.ShouldBe(30);
        result.Value.TickerStatuses.Count.ShouldBe(25); // First page, 25 items
        result.Value.TotalPages.ShouldBe(2); // 30 tickers / 25 per page = 2 pages
        result.Value.CurrentPage.ShouldBe(1);
        result.Value.PageSize.ShouldBe(25);
    }

    [Fact]
    public async Task ExecuteAsync_WithPageSize10_ReturnsTenRecords()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        List<TickerSummary> summaries = CreateTestSummaries(30);
        DataStatusQuery query = new(PageSize: 10);

        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(timeFrame))
            .Returns(summaries);

        SetupHistoricalDataForSummaries(summaries, timeFrame);

        // Act
        Result<AllDataStatusResult> result = await _useCase.ExecuteAsync(query);

        // Assert
        result.Value.TickerStatuses.Count.ShouldBe(10);
        result.Value.TotalPages.ShouldBe(3); // 30 / 10 = 3 pages
        result.Value.PageSize.ShouldBe(10);
    }

    [Fact]
    public async Task ExecuteAsync_WithPage2_ReturnsSecondPageRecords()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        List<TickerSummary> summaries = CreateTestSummaries(30);
        DataStatusQuery query = new(PageNumber: 2, PageSize: 10);

        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(timeFrame))
            .Returns(summaries);

        SetupHistoricalDataForSummaries(summaries, timeFrame);

        // Act
        Result<AllDataStatusResult> result = await _useCase.ExecuteAsync(query);

        // Assert
        result.Value.TickerStatuses.Count.ShouldBe(10);
        result.Value.CurrentPage.ShouldBe(2);
        result.Value.TotalPages.ShouldBe(3);
    }

    [Fact]
    public async Task ExecuteAsync_WithSearchTicker_FiltersResults()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        List<TickerSummary> summaries = new()
        {
            new("AAPL", "US0378331005", 250, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31)),
            new("MSFT", "US5949181045", 250, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31)),
            new("GOOGL", "US02079K3059", 200, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31)),
            new("AMZN", "US0231351067", 230, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31))
        };

        DataStatusQuery query = new(SearchTicker: "AA"); // Should match AAPL

        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(timeFrame))
            .Returns(summaries);

        SetupHistoricalDataForSummaries(summaries, timeFrame);

        // Act
        Result<AllDataStatusResult> result = await _useCase.ExecuteAsync(query);

        // Assert
        result.Value.TotalTickers.ShouldBe(1);
        result.Value.TickerStatuses.Count.ShouldBe(1);
        result.Value.TickerStatuses[0].Ticker.ShouldBe("AAPL");
    }

    [Fact]
    public async Task ExecuteAsync_WithStatusFilterComplete_ReturnsOnlyCompleteTickers()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };

        // Create summaries with different coverage levels
        // Complete: 350 records in 365 days = 95.9% coverage (>=95%)
        TickerSummary completeTicker = new(
            "COMPLETE",
            null,
            RecordCount: 350, // 350/365 = 95.9% coverage (>=95%)
            new DateTime(2023, 1, 1),
            new DateTime(2023, 12, 31));

        // Partial: 320 records in 365 days = 87.7% coverage (80-95%)
        TickerSummary partialTicker = new(
            "PARTIAL",
            null,
            RecordCount: 320, // 320/365 = 87.7% coverage (80-95%)
            new DateTime(2023, 1, 1),
            new DateTime(2023, 12, 31));

        // Gaps: 100 records in 365 days = 27.4% coverage (<80%)
        TickerSummary gappyTicker = new(
            "GAPPY",
            null,
            RecordCount: 100, // 100/365 = 27.4% coverage (<80%)
            new DateTime(2023, 1, 1),
            new DateTime(2023, 12, 31));

        List<TickerSummary> summaries = new() { completeTicker, partialTicker, gappyTicker };
        DataStatusQuery query = new(StatusFilter: DataStatusFilter.Complete);

        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(timeFrame))
            .Returns(summaries);

        SetupHistoricalDataForSummaries(summaries, timeFrame);

        // Act
        Result<AllDataStatusResult> result = await _useCase.ExecuteAsync(query);

        // Assert
        result.Value.TotalTickers.ShouldBe(1);
        result.Value.TickerStatuses.Count.ShouldBe(1);
        result.Value.TickerStatuses[0].Ticker.ShouldBe("COMPLETE");
        result.Value.TickerStatuses[0].CoveragePercentage.ShouldBeGreaterThanOrEqualTo(95m);
    }

    [Fact]
    public async Task ExecuteAsync_WithStatusFilterPartial_ReturnsOnlyPartialTickers()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };

        TickerSummary completeTicker = new(
            "COMPLETE",
            null,
            RecordCount: 350, // 350/365 = 95.9% coverage (>=95%)
            new DateTime(2023, 1, 1),
            new DateTime(2023, 12, 31));

        TickerSummary partialTicker = new(
            "PARTIAL",
            null,
            RecordCount: 320, // 320/365 = 87.7% coverage (80-95%)
            new DateTime(2023, 1, 1),
            new DateTime(2023, 12, 31));

        TickerSummary gappyTicker = new(
            "GAPPY",
            null,
            RecordCount: 100, // 100/365 = 27.4% coverage (<80%)
            new DateTime(2023, 1, 1),
            new DateTime(2023, 12, 31));

        List<TickerSummary> summaries = new() { completeTicker, partialTicker, gappyTicker };
        DataStatusQuery query = new(StatusFilter: DataStatusFilter.Partial);

        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(timeFrame))
            .Returns(summaries);

        SetupHistoricalDataForSummaries(summaries, timeFrame);

        // Act
        Result<AllDataStatusResult> result = await _useCase.ExecuteAsync(query);

        // Assert
        result.Value.TotalTickers.ShouldBe(1);
        result.Value.TickerStatuses.Count.ShouldBe(1);
        result.Value.TickerStatuses[0].Ticker.ShouldBe("PARTIAL");
        result.Value.TickerStatuses[0].CoveragePercentage.ShouldBeGreaterThanOrEqualTo(80m);
        result.Value.TickerStatuses[0].CoveragePercentage.ShouldBeLessThan(95m);
    }

    [Fact]
    public async Task ExecuteAsync_WithStatusFilterWithGaps_ReturnsOnlyGappyTickers()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };

        TickerSummary completeTicker = new(
            "COMPLETE",
            null,
            RecordCount: 350, // 350/365 = 95.9% coverage (>=95%)
            new DateTime(2023, 1, 1),
            new DateTime(2023, 12, 31));

        TickerSummary gappyTicker = new(
            "GAPPY",
            null,
            RecordCount: 100, // 100/365 = 27.4% coverage (<80%)
            new DateTime(2023, 1, 1),
            new DateTime(2023, 12, 31));

        List<TickerSummary> summaries = new() { completeTicker, gappyTicker };
        DataStatusQuery query = new(StatusFilter: DataStatusFilter.WithGaps);

        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(timeFrame))
            .Returns(summaries);

        SetupHistoricalDataForSummaries(summaries, timeFrame);

        // Act
        Result<AllDataStatusResult> result = await _useCase.ExecuteAsync(query);

        // Assert
        result.Value.TotalTickers.ShouldBe(1);
        result.Value.TickerStatuses.Count.ShouldBe(1);
        result.Value.TickerStatuses[0].Ticker.ShouldBe("GAPPY");
        result.Value.TickerStatuses[0].CoveragePercentage.ShouldBeLessThan(80m);
    }

    [Fact]
    public async Task ExecuteAsync_WithMinCoverageFilter_FiltersCorrectly()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };

        TickerSummary highCoverage = new(
            "HIGH",
            null,
            RecordCount: 330, // 330/365 = 90.4% coverage (>=90%)
            new DateTime(2023, 1, 1),
            new DateTime(2023, 12, 31));

        TickerSummary lowCoverage = new(
            "LOW",
            null,
            RecordCount: 100, // 100/365 = 27.4% coverage
            new DateTime(2023, 1, 1),
            new DateTime(2023, 12, 31));

        List<TickerSummary> summaries = new() { highCoverage, lowCoverage };
        DataStatusQuery query = new(MinCoverage: 90m); // Only high coverage

        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(timeFrame))
            .Returns(summaries);

        SetupHistoricalDataForSummaries(summaries, timeFrame);

        // Act
        Result<AllDataStatusResult> result = await _useCase.ExecuteAsync(query);

        // Assert
        result.Value.TotalTickers.ShouldBe(1);
        result.Value.TickerStatuses[0].Ticker.ShouldBe("HIGH");
        result.Value.TickerStatuses[0].CoveragePercentage.ShouldBeGreaterThanOrEqualTo(90m);
    }

    [Fact]
    public async Task ExecuteAsync_WithMaxCoverageFilter_FiltersCorrectly()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };

        TickerSummary highCoverage = new(
            "HIGH",
            null,
            RecordCount: 250,
            new DateTime(2023, 1, 1),
            new DateTime(2023, 12, 31));

        TickerSummary lowCoverage = new(
            "LOW",
            null,
            RecordCount: 100,
            new DateTime(2023, 1, 1),
            new DateTime(2023, 12, 31));

        List<TickerSummary> summaries = new() { highCoverage, lowCoverage };
        DataStatusQuery query = new(MaxCoverage: 50m); // Only low coverage

        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(timeFrame))
            .Returns(summaries);

        SetupHistoricalDataForSummaries(summaries, timeFrame);

        // Act
        Result<AllDataStatusResult> result = await _useCase.ExecuteAsync(query);

        // Assert
        result.Value.TotalTickers.ShouldBe(1);
        result.Value.TickerStatuses[0].Ticker.ShouldBe("LOW");
        result.Value.TickerStatuses[0].CoveragePercentage.ShouldBeLessThanOrEqualTo(50m);
    }

    [Fact]
    public async Task ExecuteAsync_SortByTicker_SortsAlphabetically()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        List<TickerSummary> summaries = new()
        {
            new("MSFT", null, 200, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31)),
            new("AAPL", null, 200, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31)),
            new("GOOGL", null, 200, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31))
        };

        DataStatusQuery query = new(SortBy: SortColumn.Ticker, SortDirection: SortDirection.Ascending);

        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(timeFrame))
            .Returns(summaries);

        SetupHistoricalDataForSummaries(summaries, timeFrame);

        // Act
        Result<AllDataStatusResult> result = await _useCase.ExecuteAsync(query);

        // Assert
        result.Value.TickerStatuses[0].Ticker.ShouldBe("AAPL");
        result.Value.TickerStatuses[1].Ticker.ShouldBe("GOOGL");
        result.Value.TickerStatuses[2].Ticker.ShouldBe("MSFT");
    }

    [Fact]
    public async Task ExecuteAsync_SortByRecordCount_SortsCorrectly()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        List<TickerSummary> summaries = new()
        {
            new("AAPL", null, 100, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31)),
            new("MSFT", null, 250, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31)),
            new("GOOGL", null, 150, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31))
        };

        DataStatusQuery query = new(SortBy: SortColumn.RecordCount, SortDirection: SortDirection.Descending);

        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(timeFrame))
            .Returns(summaries);

        SetupHistoricalDataForSummaries(summaries, timeFrame);

        // Act
        Result<AllDataStatusResult> result = await _useCase.ExecuteAsync(query);

        // Assert
        result.Value.TickerStatuses[0].RecordCount.ShouldBe(250); // MSFT
        result.Value.TickerStatuses[1].RecordCount.ShouldBe(150); // GOOGL
        result.Value.TickerStatuses[2].RecordCount.ShouldBe(100); // AAPL
    }

    [Fact]
    public async Task ExecuteAsync_SortByCoverage_SortsCorrectly()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        List<TickerSummary> summaries = new()
        {
            new("HIGH", null, 250, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31)),
            new("LOW", null, 100, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31)),
            new("MEDIUM", null, 180, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31))
        };

        DataStatusQuery query = new(SortBy: SortColumn.Coverage, SortDirection: SortDirection.Ascending);

        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(timeFrame))
            .Returns(summaries);

        SetupHistoricalDataForSummaries(summaries, timeFrame);

        // Act
        Result<AllDataStatusResult> result = await _useCase.ExecuteAsync(query);

        // Assert
        result.Value.TickerStatuses[0].Ticker.ShouldBe("LOW");
        result.Value.TickerStatuses[1].Ticker.ShouldBe("MEDIUM");
        result.Value.TickerStatuses[2].Ticker.ShouldBe("HIGH");
    }

    [Fact]
    public async Task ExecuteAsync_WithCombinedFiltersAndSorting_AppliesBothCorrectly()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        List<TickerSummary> summaries = new()
        {
            new("AAPL", null, 350, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31)),  // Complete: 350/365 = 95.9%
            new("MSFT", null, 320, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31)),  // Partial: 320/365 = 87.7%
            new("GOOGL", null, 347, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31)), // Complete: 347/365 = 95.1%
            new("AMZN", null, 100, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31))   // Gaps: 100/365 = 27.4%
        };

        DataStatusQuery query = new(
            StatusFilter: DataStatusFilter.Complete,
            SortBy: SortColumn.RecordCount,
            SortDirection: SortDirection.Descending);

        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(timeFrame))
            .Returns(summaries);

        SetupHistoricalDataForSummaries(summaries, timeFrame);

        // Act
        Result<AllDataStatusResult> result = await _useCase.ExecuteAsync(query);

        // Assert
        result.Value.TotalTickers.ShouldBe(2); // Only AAPL and GOOGL (complete tickers)
        result.Value.TickerStatuses[0].Ticker.ShouldBe("AAPL"); // 250 records
        result.Value.TickerStatuses[1].Ticker.ShouldBe("GOOGL"); // 240 records
    }

    [Fact]
    public async Task ExecuteAsync_WithNoData_ReturnsEmptyResult()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(timeFrame))
            .Returns(new List<TickerSummary>());

        // Act
        Result<AllDataStatusResult> result = await _useCase.ExecuteAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Value.TotalTickers.ShouldBe(0);
        result.Value.TotalRecords.ShouldBe(0);
        result.Value.AverageCoveragePercentage.ShouldBe(0m);
        result.Value.TickerStatuses.ShouldBeEmpty();
        result.Value.TotalPages.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_CalculatesAverageCoverageCorrectly()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        List<TickerSummary> summaries = new()
        {
            new("TICKER1", null, 250, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31)), // ~97.8%
            new("TICKER2", null, 200, new DateTime(2023, 1, 1), new DateTime(2023, 12, 31))  // ~78.3%
        };

        A.CallTo(() => _historicalDataPort.GetAllTickerSummariesAsync(timeFrame))
            .Returns(summaries);

        SetupHistoricalDataForSummaries(summaries, timeFrame);

        // Act
        Result<AllDataStatusResult> result = await _useCase.ExecuteAsync();

        // Assert
        result.Value.AverageCoveragePercentage.ShouldBeGreaterThan(0m);
        result.Value.AverageCoveragePercentage.ShouldBeLessThan(100m);
    }

    private List<TickerSummary> CreateTestSummaries(int count)
    {
        List<TickerSummary> summaries = new();
        for (int i = 0; i < count; i++)
        {
            summaries.Add(new TickerSummary(
                $"TICKER{i:D3}",
                null,
                200 + i,
                new DateTime(2023, 1, 1),
                new DateTime(2023, 12, 31)));
        }
        return summaries;
    }

    private void SetupHistoricalDataForSummaries(List<TickerSummary> summaries, TimeFrame timeFrame)
    {
        foreach (TickerSummary summary in summaries)
        {
            DataSummaryResult dataSummary = new(
                summary.Ticker,
                summary.ISIN,
                summary.RecordCount,
                0, // NewRecords
                summary.OldestDate,
                summary.LatestDate,
                95m, // MinPrice
                105m, // MaxPrice
                102m); // LatestClose

            A.CallTo(() => _historicalDataPort.GetDataSummaryAsync(summary.Ticker, timeFrame))
                .Returns(dataSummary);

            List<HistoricalPrice> prices = CreatePricesForSummary(summary);

            A.CallTo(() => _historicalDataPort.GetHistoricalDataAsync(summary.Ticker, timeFrame))
                .Returns(prices);
        }
    }

    private List<HistoricalPrice> CreatePricesForSummary(TickerSummary summary)
    {
        List<HistoricalPrice> prices = new();
        if (summary.OldestDate.HasValue && summary.LatestDate.HasValue)
        {
            DateTime currentDate = summary.OldestDate.Value;
            int recordsCreated = 0;

            while (currentDate <= summary.LatestDate.Value && recordsCreated < summary.RecordCount)
            {
                prices.Add(new HistoricalPrice
                {
                    Ticker = summary.Ticker,
                    DateTime = currentDate,
                    Open = 100m,
                    High = 105m,
                    Low = 95m,
                    Close = 102m,
                    Volume = 1000000
                });
                currentDate = currentDate.AddDays(1);
                recordsCreated++;
            }
        }
        return prices;
    }
}
