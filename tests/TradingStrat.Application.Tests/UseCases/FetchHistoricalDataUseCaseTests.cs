using FluentAssertions;
using Moq;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Services;
using TradingStrat.Application.Tests.TestDoubles;
using TradingStrat.Application.UseCases;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.Tests.UseCases;

public class FetchHistoricalDataUseCaseTests
{
    private readonly InMemoryHistoricalDataRepository _historicalDataPort;
    private readonly FakeMarketDataAdapter _marketDataPort;
    private readonly Mock<ITickerResolver> _tickerResolverMock;
    private readonly FetchHistoricalDataUseCase _useCase;

    public FetchHistoricalDataUseCaseTests()
    {
        _historicalDataPort = new InMemoryHistoricalDataRepository();
        _marketDataPort = new FakeMarketDataAdapter();
        _tickerResolverMock = new Mock<ITickerResolver>();

        _useCase = new FetchHistoricalDataUseCase(
            _historicalDataPort,
            _marketDataPort,
            _tickerResolverMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidTicker_ShouldFetchAndSaveData()
    {
        // Arrange
        var command = new FetchDataCommand(
            Ticker: "TEST",
            Isin: null,
            StartDate: new DateTime(2024, 1, 1),
            EndDate: new DateTime(2024, 1, 31));

        // Act
        var result = await _useCase.ExecuteAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.Ticker.Should().Be("TEST");
        result.TotalRecords.Should().BeGreaterThan(0);

        var savedData = await _historicalDataPort.GetHistoricalDataAsync("TEST");
        savedData.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithISIN_ShouldResolveTickerAndFetch()
    {
        // Arrange
        var command = new FetchDataCommand(
            Ticker: "CON3.L",
            Isin: "XS2399367254",
            StartDate: new DateTime(2024, 1, 1),
            EndDate: new DateTime(2024, 1, 31));

        _tickerResolverMock
            .Setup(x => x.GetAllTickersForIsin("XS2399367254"))
            .Returns(new List<string> { "CON3.L", "3COI.DE" });

        // Act
        var result = await _useCase.ExecuteAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.Ticker.Should().Be("CON3.L");
        _tickerResolverMock.Verify(x => x.GetAllTickersForIsin("XS2399367254"), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDataAlreadyExists_ShouldNotDuplicateRecords()
    {
        // Arrange
        var existingData = new List<HistoricalPrice>
        {
            new() { Ticker = "TEST", DateTime = new DateTime(2024, 1, 1), Close = 100m },
            new() { Ticker = "TEST", DateTime = new DateTime(2024, 1, 2), Close = 101m }
        };

        _historicalDataPort.SeedData("TEST", existingData);

        var command = new FetchDataCommand(
            Ticker: "TEST",
            StartDate: new DateTime(2024, 1, 1),
            EndDate: new DateTime(2024, 1, 31));

        // Act
        var result = await _useCase.ExecuteAsync(command);

        // Assert
        var savedData = await _historicalDataPort.GetHistoricalDataAsync("TEST");

        // Should not have duplicates for existing dates
        savedData.GroupBy(p => p.DateTime)
            .Should().OnlyContain(g => g.Count() == 1, "dates should not be duplicated");
    }

    [Fact]
    public async Task ExecuteAsync_WhenDatabaseUpToDate_ShouldReturnExistingSummary()
    {
        // Arrange
        var futureData = new List<HistoricalPrice>
        {
            new() { Ticker = "TEST", DateTime = DateTime.Today.AddDays(10), Close = 100m }
        };

        _historicalDataPort.SeedData("TEST", futureData);

        var command = new FetchDataCommand(
            Ticker: "TEST",
            StartDate: null,  // Will use latest date + 1
            EndDate: DateTime.Today);

        // Act
        var result = await _useCase.ExecuteAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.TotalRecords.Should().Be(1);  // Only existing record
    }

    [Fact]
    public async Task ExecuteAsync_WithProgressReporting_ShouldReportProgress()
    {
        // Arrange
        var progressMessages = new List<string>();
        var progress = new Progress<string>(msg => progressMessages.Add(msg));

        var command = new FetchDataCommand(
            Ticker: "TEST",
            StartDate: new DateTime(2024, 1, 1),
            EndDate: new DateTime(2024, 1, 10));

        // Act
        await _useCase.ExecuteAsync(command, progress);

        // Assert
        progressMessages.Should().NotBeEmpty();
        progressMessages.Should().Contain(msg => msg.Contains("Initializing"));
        progressMessages.Should().Contain(msg => msg.Contains("Fetching"));
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidISIN_ShouldThrow()
    {
        // Arrange
        var command = new FetchDataCommand(
            Ticker: "INVALID",
            Isin: "INVALID_ISIN");

        _tickerResolverMock
            .Setup(x => x.GetAllTickersForIsin("INVALID_ISIN"))
            .Returns((List<string>?)null);

        // Act
        var act = async () => await _useCase.ExecuteAsync(command);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Could not resolve ISIN*");
    }
}
