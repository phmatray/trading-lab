using FakeItEasy;
using Shouldly;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.UseCases;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Tests.UseCases;

public class DeleteHistoricalDataUseCaseTests
{
    private readonly IHistoricalDataPort _historicalDataPort;
    private readonly DeleteHistoricalDataUseCase _useCase;

    public DeleteHistoricalDataUseCaseTests()
    {
        _historicalDataPort = A.Fake<IHistoricalDataPort>();
        _useCase = new DeleteHistoricalDataUseCase(_historicalDataPort);
    }

    [Fact]
    public async Task DeleteTickerAsync_WithValidTicker_DeletesAllTimeframes()
    {
        // Arrange
        string ticker = "AAPL";
        int deletedRecords = 250;

        A.CallTo(() => _historicalDataPort.DeleteTickerDataAsync(ticker, null))
            .Returns(deletedRecords);

        // Act
        var result = await _useCase.DeleteTickerAsync(ticker);

        // Assert
        result.ShouldNotBeNull();
        result.Value.RecordsDeleted.ShouldBe(deletedRecords);
        result.Value.Message.ShouldContain("AAPL");
        result.Value.Message.ShouldContain("250 record(s)");
        result.Value.Message.ShouldContain("all timeframes");

        A.CallTo(() => _historicalDataPort.DeleteTickerDataAsync(ticker, null))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DeleteTickerAsync_WithSpecificTimeframe_DeletesOnlyThatTimeframe()
    {
        // Arrange
        string ticker = "MSFT";
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        int deletedRecords = 100;

        A.CallTo(() => _historicalDataPort.DeleteTickerDataAsync(ticker, timeFrame))
            .Returns(deletedRecords);

        // Act
        var result = await _useCase.DeleteTickerAsync(ticker, timeFrame);

        // Assert
        result.ShouldNotBeNull();
        result.Value.RecordsDeleted.ShouldBe(deletedRecords);
        result.Value.Message.ShouldContain("MSFT");
        result.Value.Message.ShouldContain("100 record(s)");
        result.Value.Message.ShouldContain("D1");

        A.CallTo(() => _historicalDataPort.DeleteTickerDataAsync(ticker, timeFrame))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DeleteTickerAsync_WithNoRecordsFound_ReturnsZeroDeleted()
    {
        // Arrange
        string ticker = "NONEXISTENT";

        A.CallTo(() => _historicalDataPort.DeleteTickerDataAsync(ticker, null))
            .Returns(0);

        // Act
        var result = await _useCase.DeleteTickerAsync(ticker);

        // Assert
        result.ShouldNotBeNull();
        result.Value.RecordsDeleted.ShouldBe(0);
        result.Value.Message.ShouldContain("NONEXISTENT");
        result.Value.Message.ShouldContain("0 record(s)");
    }

    [Fact]
    public async Task DeleteTickerAsync_WithEmptyTicker_ReturnsFailure()
    {
        // Arrange
        string ticker = "";

        // Act
        var result = await _useCase.DeleteTickerAsync(ticker);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("Ticker cannot be null or empty");
        result.Errors[0].Code.ShouldBe("TICKER_REQUIRED");
    }

    [Fact]
    public async Task DeleteTickerAsync_WithNullTicker_ReturnsFailure()
    {
        // Arrange
        string? ticker = null;

        // Act
        var result = await _useCase.DeleteTickerAsync(ticker!);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("Ticker cannot be null or empty");
        result.Errors[0].Code.ShouldBe("TICKER_REQUIRED");
    }

    [Fact]
    public async Task DeleteDateRangeAsync_WithValidRange_DeletesRecordsInRange()
    {
        // Arrange
        string ticker = "GOOGL";
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        DateTime startDate = new(2024, 1, 1);
        DateTime endDate = new(2024, 6, 30);
        int deletedRecords = 125;

        A.CallTo(() => _historicalDataPort.DeleteDateRangeAsync(ticker, timeFrame, startDate, endDate))
            .Returns(deletedRecords);

        // Act
        var result = await _useCase.DeleteDateRangeAsync(ticker, timeFrame, startDate, endDate);

        // Assert
        result.ShouldNotBeNull();
        result.Value.RecordsDeleted.ShouldBe(deletedRecords);
        result.Value.Message.ShouldContain("GOOGL");
        result.Value.Message.ShouldContain("125 record(s)");
        result.Value.Message.ShouldContain("D1");
        result.Value.Message.ShouldContain("2024-01-01");
        result.Value.Message.ShouldContain("2024-06-30");

        A.CallTo(() => _historicalDataPort.DeleteDateRangeAsync(ticker, timeFrame, startDate, endDate))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DeleteDateRangeAsync_WithStartDateAfterEndDate_ReturnsFailure()
    {
        // Arrange
        string ticker = "AAPL";
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        DateTime startDate = new(2024, 12, 31);
        DateTime endDate = new(2024, 1, 1);

        // Act
        var result = await _useCase.DeleteDateRangeAsync(ticker, timeFrame, startDate, endDate);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("Start date must be less than or equal to end date");
        result.Errors[0].Code.ShouldBe("INVALID_DATE_RANGE");
    }

    [Fact]
    public async Task DeleteDateRangeAsync_WithEmptyTicker_ReturnsFailure()
    {
        // Arrange
        string ticker = "";
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        DateTime startDate = new(2024, 1, 1);
        DateTime endDate = new(2024, 6, 30);

        // Act
        var result = await _useCase.DeleteDateRangeAsync(ticker, timeFrame, startDate, endDate);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("Ticker cannot be null or empty");
        result.Errors[0].Code.ShouldBe("TICKER_REQUIRED");
    }

    [Fact]
    public async Task DeleteDateRangeAsync_WithSingleDay_DeletesOnlyThatDay()
    {
        // Arrange
        string ticker = "TSLA";
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        DateTime singleDate = new(2024, 7, 15);
        int deletedRecords = 1;

        A.CallTo(() => _historicalDataPort.DeleteDateRangeAsync(ticker, timeFrame, singleDate, singleDate))
            .Returns(deletedRecords);

        // Act
        var result = await _useCase.DeleteDateRangeAsync(ticker, timeFrame, singleDate, singleDate);

        // Assert
        result.ShouldNotBeNull();
        result.Value.RecordsDeleted.ShouldBe(deletedRecords);
        result.Value.Message.ShouldContain("2024-07-15");

        A.CallTo(() => _historicalDataPort.DeleteDateRangeAsync(ticker, timeFrame, singleDate, singleDate))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DeleteDateRangeAsync_WithNoRecordsInRange_ReturnsZeroDeleted()
    {
        // Arrange
        string ticker = "AAPL";
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.D1 };
        DateTime startDate = new(2020, 1, 1);
        DateTime endDate = new(2020, 1, 31);

        A.CallTo(() => _historicalDataPort.DeleteDateRangeAsync(ticker, timeFrame, startDate, endDate))
            .Returns(0);

        // Act
        var result = await _useCase.DeleteDateRangeAsync(ticker, timeFrame, startDate, endDate);

        // Assert
        result.ShouldNotBeNull();
        result.Value.RecordsDeleted.ShouldBe(0);
        result.Value.Message.ShouldContain("0 record(s)");
    }

    [Fact]
    public async Task DeleteDateRangeAsync_WithIntradayTimeframe_DeletesCorrectly()
    {
        // Arrange
        string ticker = "AAPL";
        TimeFrame timeFrame = new() { Unit = TimeFrameUnit.M5 };
        DateTime startDate = new(2024, 12, 1);
        DateTime endDate = new(2024, 12, 31);
        int deletedRecords = 5760; // 30 days * 8 hours * 12 bars per hour

        A.CallTo(() => _historicalDataPort.DeleteDateRangeAsync(ticker, timeFrame, startDate, endDate))
            .Returns(deletedRecords);

        // Act
        var result = await _useCase.DeleteDateRangeAsync(ticker, timeFrame, startDate, endDate);

        // Assert
        result.ShouldNotBeNull();
        result.Value.RecordsDeleted.ShouldBe(deletedRecords);
        result.Value.Message.ShouldContain("M5");
        result.Value.Message.ShouldContain("5760 record(s)");
    }
}
