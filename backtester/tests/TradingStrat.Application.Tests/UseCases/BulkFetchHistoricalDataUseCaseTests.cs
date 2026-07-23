using FakeItEasy;
using Shouldly;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.UseCases;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

using BulkFetchDataCommand = TradingStrat.Application.Ports.Inbound.BulkFetchDataCommand;
using BulkFetchProgress = TradingStrat.Application.Ports.Inbound.BulkFetchProgress;

namespace TradingStrat.Application.Tests.UseCases;

public class BulkFetchHistoricalDataUseCaseTests
{
    private readonly IHistoricalDataPort _historicalDataPort;
    private readonly IMarketDataPort _marketDataPort;
    private readonly BulkFetchHistoricalDataUseCase _useCase;

    public BulkFetchHistoricalDataUseCaseTests()
    {
        _historicalDataPort = A.Fake<IHistoricalDataPort>();
        _marketDataPort = A.Fake<IMarketDataPort>();
        _useCase = new BulkFetchHistoricalDataUseCase(_historicalDataPort, _marketDataPort);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidTickers_FetchesAllSuccessfully()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        List<string> tickers = new() { "AAPL", "MSFT", "GOOGL" };
        BulkFetchDataCommand command = new(tickers, timeFrame);

        List<HistoricalPrice> aaplPrices = CreateTestPrices("AAPL", 10);
        List<HistoricalPrice> msftPrices = CreateTestPrices("MSFT", 12);
        List<HistoricalPrice> googlPrices = CreateTestPrices("GOOGL", 8);

        A.CallTo(() => _historicalDataPort.GetLatestDataDateAsync("AAPL", timeFrame))
            .Returns(Task.FromResult<DateTime?>(null));
        A.CallTo(() => _historicalDataPort.GetLatestDataDateAsync("MSFT", timeFrame))
            .Returns(Task.FromResult<DateTime?>(null));
        A.CallTo(() => _historicalDataPort.GetLatestDataDateAsync("GOOGL", timeFrame))
            .Returns(Task.FromResult<DateTime?>(null));

        A.CallTo(() => _marketDataPort.FetchHistoricalDataAsync("AAPL", timeFrame, A<DateTime>._, A<DateTime>._))
            .Returns(aaplPrices);
        A.CallTo(() => _marketDataPort.FetchHistoricalDataAsync("MSFT", timeFrame, A<DateTime>._, A<DateTime>._))
            .Returns(msftPrices);
        A.CallTo(() => _marketDataPort.FetchHistoricalDataAsync("GOOGL", timeFrame, A<DateTime>._, A<DateTime>._))
            .Returns(googlPrices);

        A.CallTo(() => _historicalDataPort.GetDataSummaryAsync("AAPL", timeFrame))
            .Returns(new DataSummaryResult("AAPL", null, 10, 10, DateTime.Today.AddDays(-10), DateTime.Today, 95m, 105m, 102m));
        A.CallTo(() => _historicalDataPort.GetDataSummaryAsync("MSFT", timeFrame))
            .Returns(new DataSummaryResult("MSFT", null, 12, 12, DateTime.Today.AddDays(-12), DateTime.Today, 95m, 105m, 102m));
        A.CallTo(() => _historicalDataPort.GetDataSummaryAsync("GOOGL", timeFrame))
            .Returns(new DataSummaryResult("GOOGL", null, 8, 8, DateTime.Today.AddDays(-8), DateTime.Today, 95m, 105m, 102m));

        // Act
        Result<BulkFetchResult> result = await _useCase.ExecuteAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.Value.TotalTickers.ShouldBe(3);
        result.Value.SuccessfulTickers.ShouldBe(3);
        result.Value.FailedTickers.ShouldBe(0);
        result.Value.SkippedTickers.ShouldBe(0);
        result.Value.SuccessfulResults.ShouldContainKey("AAPL");
        result.Value.SuccessfulResults.ShouldContainKey("MSFT");
        result.Value.SuccessfulResults.ShouldContainKey("GOOGL");
        result.Value.FailedResults.ShouldBeEmpty();
        result.Value.SkippedResults.ShouldBeEmpty();

        // Verify all tickers were saved
        A.CallTo(() => _historicalDataPort.SaveHistoricalDataAsync("AAPL", null, timeFrame, aaplPrices))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _historicalDataPort.SaveHistoricalDataAsync("MSFT", null, timeFrame, msftPrices))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _historicalDataPort.SaveHistoricalDataAsync("GOOGL", null, timeFrame, googlPrices))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteAsync_WithPartialFailures_ContinuesProcessingOtherTickers()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        List<string> tickers = new() { "AAPL", "INVALID", "MSFT" };
        BulkFetchDataCommand command = new(tickers, timeFrame);

        List<HistoricalPrice> aaplPrices = CreateTestPrices("AAPL", 10);
        List<HistoricalPrice> msftPrices = CreateTestPrices("MSFT", 12);

        A.CallTo(() => _historicalDataPort.GetLatestDataDateAsync("AAPL", timeFrame))
            .Returns(Task.FromResult<DateTime?>(null));
        A.CallTo(() => _historicalDataPort.GetLatestDataDateAsync("INVALID", timeFrame))
            .Returns(Task.FromResult<DateTime?>(null));
        A.CallTo(() => _historicalDataPort.GetLatestDataDateAsync("MSFT", timeFrame))
            .Returns(Task.FromResult<DateTime?>(null));

        A.CallTo(() => _marketDataPort.FetchHistoricalDataAsync("AAPL", timeFrame, A<DateTime>._, A<DateTime>._))
            .Returns(aaplPrices);
        A.CallTo(() => _marketDataPort.FetchHistoricalDataAsync("INVALID", timeFrame, A<DateTime>._, A<DateTime>._))
            .Throws(new InvalidOperationException("Ticker not found"));
        A.CallTo(() => _marketDataPort.FetchHistoricalDataAsync("MSFT", timeFrame, A<DateTime>._, A<DateTime>._))
            .Returns(msftPrices);

        A.CallTo(() => _historicalDataPort.GetDataSummaryAsync("AAPL", timeFrame))
            .Returns(new DataSummaryResult("AAPL", null, 10, 10, DateTime.Today.AddDays(-10), DateTime.Today, 95m, 105m, 102m));
        A.CallTo(() => _historicalDataPort.GetDataSummaryAsync("MSFT", timeFrame))
            .Returns(new DataSummaryResult("MSFT", null, 12, 12, DateTime.Today.AddDays(-12), DateTime.Today, 95m, 105m, 102m));

        // Act
        Result<BulkFetchResult> result = await _useCase.ExecuteAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.Value.TotalTickers.ShouldBe(3);
        result.Value.SuccessfulTickers.ShouldBe(2);
        result.Value.FailedTickers.ShouldBe(1);
        result.Value.SkippedTickers.ShouldBe(0);
        result.Value.SuccessfulResults.ShouldContainKey("AAPL");
        result.Value.SuccessfulResults.ShouldContainKey("MSFT");
        result.Value.FailedResults.ShouldContainKey("INVALID");
        result.Value.FailedResults["INVALID"].ShouldContain("Ticker not found");

        // Verify successful tickers were still saved
        A.CallTo(() => _historicalDataPort.SaveHistoricalDataAsync("AAPL", null, timeFrame, aaplPrices))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _historicalDataPort.SaveHistoricalDataAsync("MSFT", null, timeFrame, msftPrices))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteAsync_WithSkipExistingAndUpToDateData_SkipsTicker()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        List<string> tickers = new() { "AAPL", "MSFT" };
        BulkFetchDataCommand command = new(tickers, timeFrame, SkipExisting: true);

        List<HistoricalPrice> msftPrices = CreateTestPrices("MSFT", 10);

        // AAPL is up-to-date (latest date is today)
        A.CallTo(() => _historicalDataPort.GetLatestDataDateAsync("AAPL", timeFrame))
            .Returns(DateTime.Today);
        // MSFT needs update (latest date is 5 days ago)
        A.CallTo(() => _historicalDataPort.GetLatestDataDateAsync("MSFT", timeFrame))
            .Returns(DateTime.Today.AddDays(-5));

        A.CallTo(() => _marketDataPort.FetchHistoricalDataAsync("MSFT", timeFrame, A<DateTime>._, A<DateTime>._))
            .Returns(msftPrices);

        A.CallTo(() => _historicalDataPort.GetDataSummaryAsync("MSFT", timeFrame))
            .Returns(new DataSummaryResult("MSFT", null, 10, 10, DateTime.Today.AddDays(-10), DateTime.Today, 95m, 105m, 102m));

        // Act
        Result<BulkFetchResult> result = await _useCase.ExecuteAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.Value.TotalTickers.ShouldBe(2);
        result.Value.SuccessfulTickers.ShouldBe(1);
        result.Value.FailedTickers.ShouldBe(0);
        result.Value.SkippedTickers.ShouldBe(1);
        result.Value.SkippedResults.ShouldContain("AAPL");
        result.Value.SuccessfulResults.ShouldContainKey("MSFT");

        // Verify AAPL was not fetched
        A.CallTo(() => _marketDataPort.FetchHistoricalDataAsync("AAPL", A<TimeFrame>._, A<DateTime>._, A<DateTime>._))
            .MustNotHaveHappened();
        // Verify MSFT was fetched
        A.CallTo(() => _historicalDataPort.SaveHistoricalDataAsync("MSFT", null, timeFrame, msftPrices))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomDateRange_UsesSpecifiedDates()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        DateTime startDate = new(2024, 1, 1);
        DateTime endDate = new(2024, 12, 31);
        List<string> tickers = new() { "AAPL" };
        BulkFetchDataCommand command = new(tickers, timeFrame, startDate, endDate);

        List<HistoricalPrice> prices = CreateTestPrices("AAPL", 10);

        A.CallTo(() => _marketDataPort.FetchHistoricalDataAsync("AAPL", timeFrame, startDate, endDate))
            .Returns(prices);

        A.CallTo(() => _historicalDataPort.GetDataSummaryAsync("AAPL", timeFrame))
            .Returns(new DataSummaryResult("AAPL", null, 10, 10, startDate, endDate, 95m, 105m, 102m));

        // Act
        Result<BulkFetchResult> result = await _useCase.ExecuteAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.Value.SuccessfulTickers.ShouldBe(1);

        // Verify the exact date range was used
        A.CallTo(() => _marketDataPort.FetchHistoricalDataAsync("AAPL", timeFrame, startDate, endDate))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteAsync_WithIncrementalUpdate_StartsFromLatestDate()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        DateTime latestExistingDate = new(2024, 6, 1);
        List<string> tickers = new() { "AAPL" };
        BulkFetchDataCommand command = new(tickers, timeFrame);

        List<HistoricalPrice> prices = CreateTestPrices("AAPL", 5);

        A.CallTo(() => _historicalDataPort.GetLatestDataDateAsync("AAPL", timeFrame))
            .Returns(latestExistingDate);

        A.CallTo(() => _marketDataPort.FetchHistoricalDataAsync(
                "AAPL",
                timeFrame,
                latestExistingDate.AddDays(1), // Should start from day after latest
                A<DateTime>._))
            .Returns(prices);

        A.CallTo(() => _historicalDataPort.GetDataSummaryAsync("AAPL", timeFrame))
            .Returns(new DataSummaryResult("AAPL", null, 5, 5, latestExistingDate.AddDays(1), DateTime.Today, 95m, 105m, 102m));

        // Act
        Result<BulkFetchResult> result = await _useCase.ExecuteAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.Value.SuccessfulTickers.ShouldBe(1);

        // Verify incremental fetch started from day after latest
        A.CallTo(() => _marketDataPort.FetchHistoricalDataAsync(
                "AAPL",
                timeFrame,
                latestExistingDate.AddDays(1),
                A<DateTime>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void ExecuteAsync_WithEmptyTickerList_ThrowsArgumentException()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        List<string> tickers = new();

        // Act & Assert - validation happens in command constructor
        ArgumentException ex = Should.Throw<ArgumentException>(() =>
            new BulkFetchDataCommand(tickers, timeFrame));

        ex.Message.ShouldContain("Tickers list cannot be empty");
        ex.ParamName.ShouldBe("Tickers");
    }

    [Fact]
    public async Task ExecuteAsync_ReportsProgressCorrectly()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        List<string> tickers = new() { "AAPL", "MSFT" };
        BulkFetchDataCommand command = new(tickers, timeFrame);

        List<HistoricalPrice> aaplPrices = CreateTestPrices("AAPL", 10);
        List<HistoricalPrice> msftPrices = CreateTestPrices("MSFT", 12);

        A.CallTo(() => _historicalDataPort.GetLatestDataDateAsync(A<string>._, timeFrame))
            .Returns(Task.FromResult<DateTime?>(null));

        A.CallTo(() => _marketDataPort.FetchHistoricalDataAsync("AAPL", timeFrame, A<DateTime>._, A<DateTime>._))
            .Returns(aaplPrices);
        A.CallTo(() => _marketDataPort.FetchHistoricalDataAsync("MSFT", timeFrame, A<DateTime>._, A<DateTime>._))
            .Returns(msftPrices);

        A.CallTo(() => _historicalDataPort.GetDataSummaryAsync(A<string>._, timeFrame))
            .Returns(new DataSummaryResult("", null, 10, 10, DateTime.Today.AddDays(-10), DateTime.Today, 95m, 105m, 102m));

        List<BulkFetchProgress> progressReports = new();
        Progress<BulkFetchProgress> progress = new(p => progressReports.Add(p));

        // Act
        Result<BulkFetchResult> result = await _useCase.ExecuteAsync(command, progress);

        // Assert
        result.Value.SuccessfulTickers.ShouldBe(2);
        progressReports.ShouldNotBeEmpty();

        // Verify progress was reported for both tickers
        progressReports.ShouldContain(p => p.CurrentTicker == "AAPL");
        progressReports.ShouldContain(p => p.CurrentTicker == "MSFT");

        // Verify final progress is 100%
        BulkFetchProgress? finalProgress = progressReports.LastOrDefault();
        finalProgress.ShouldNotBeNull();
        finalProgress.CompletedTickers.ShouldBe(2);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_StopsProcessing()
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        List<string> tickers = new() { "AAPL", "MSFT", "GOOGL" };
        BulkFetchDataCommand command = new(tickers, timeFrame);

        CancellationTokenSource cts = new();
        List<HistoricalPrice> prices = CreateTestPrices("AAPL", 10);

        A.CallTo(() => _historicalDataPort.GetLatestDataDateAsync(A<string>._, timeFrame))
            .Returns(Task.FromResult<DateTime?>(null));

        A.CallTo(() => _marketDataPort.FetchHistoricalDataAsync("AAPL", timeFrame, A<DateTime>._, A<DateTime>._))
            .Returns(prices);

        A.CallTo(() => _historicalDataPort.GetDataSummaryAsync("AAPL", timeFrame))
            .ReturnsLazily(() =>
            {
                cts.Cancel(); // Cancel after first ticker
                return new DataSummaryResult("AAPL", null, 10, 10, DateTime.Today.AddDays(-10), DateTime.Today, 95m, 105m, 102m);
            });

        // Act
        Result<BulkFetchResult> result = await _useCase.ExecuteAsync(command, cancellationToken: cts.Token);

        // Assert
        result.Value.SuccessfulTickers.ShouldBeLessThan(3); // Should not process all tickers
        result.Value.TotalTickers.ShouldBe(3);
    }

    private List<HistoricalPrice> CreateTestPrices(string ticker, int count)
    {
        List<HistoricalPrice> prices = new();
        for (int i = 0; i < count; i++)
        {
            prices.Add(new HistoricalPrice
            {
                Ticker = ticker,
                DateTime = DateTime.Today.AddDays(-count + i),
                Open = 100m + i,
                High = 105m + i,
                Low = 95m + i,
                Close = 102m + i,
                Volume = 1000000 + (i * 1000)
            });
        }
        return prices;
    }
}
