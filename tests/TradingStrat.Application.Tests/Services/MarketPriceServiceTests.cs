using FakeItEasy;
using Shouldly;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.Services;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Tests.Services;

public class MarketPriceServiceTests
{
    private readonly IMarketDataPort _marketDataPort;
    private readonly MarketPriceService _service;

    public MarketPriceServiceTests()
    {
        _marketDataPort = A.Fake<IMarketDataPort>();
        _service = new MarketPriceService();
    }

    [Fact]
    public async Task GetCurrentPricesAsync_WithValidTickers_ReturnsSuccess()
    {
        // Arrange
        List<string> tickers = new() { "AAPL", "MSFT" };

        A.CallTo(() => _marketDataPort.FetchHistoricalDataAsync(
                "AAPL",
                TimeFrame.D1,
                A<DateTime>._,
                A<DateTime>._))
            .Returns(new List<HistoricalPrice>
            {
                new() { Ticker = "AAPL", DateTime = DateTime.Today, Close = 150.00m }
            });

        A.CallTo(() => _marketDataPort.FetchHistoricalDataAsync(
                "MSFT",
                TimeFrame.D1,
                A<DateTime>._,
                A<DateTime>._))
            .Returns(new List<HistoricalPrice>
            {
                new() { Ticker = "MSFT", DateTime = DateTime.Today, Close = 350.00m }
            });

        // Act
        Domain.Common.Result<Dictionary<string, decimal>> result = await _service.GetCurrentPricesAsync(
            tickers,
            _marketDataPort);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(2);
        result.Value["AAPL"].ShouldBe(150.00m);
        result.Value["MSFT"].ShouldBe(350.00m);
    }

    [Fact]
    public async Task GetCurrentPricesAsync_WithProgressReporter_DoesNotThrow()
    {
        // Arrange
        List<string> tickers = new() { "AAPL" };
        IProgress<string>? progress = A.Fake<IProgress<string>>();

        A.CallTo(() => _marketDataPort.FetchHistoricalDataAsync(
                A<string>._,
                A<TimeFrame>._,
                A<DateTime>._,
                A<DateTime>._))
            .Returns(new List<HistoricalPrice>
            {
                new() { Ticker = "AAPL", DateTime = DateTime.Today, Close = 150.00m }
            });

        // Act
        Domain.Common.Result<Dictionary<string, decimal>> result = await _service.GetCurrentPricesAsync(
            tickers,
            _marketDataPort,
            progress);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        A.CallTo(() => progress.Report(A<string>.That.Contains("AAPL"))).MustHaveHappened();
    }

    [Fact]
    public async Task GetCurrentPricesAsync_WithNoData_ReturnsFailure()
    {
        // Arrange
        List<string> tickers = new() { "INVALID" };

        A.CallTo(() => _marketDataPort.FetchHistoricalDataAsync(
                A<string>._,
                A<TimeFrame>._,
                A<DateTime>._,
                A<DateTime>._))
            .Returns(new List<HistoricalPrice>());

        // Act
        Domain.Common.Result<Dictionary<string, decimal>> result = await _service.GetCurrentPricesAsync(
            tickers,
            _marketDataPort);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeEmpty();
        result.Errors[0].Code.ShouldBe("PRICE_DATA_NOT_FOUND");
    }

    [Fact]
    public async Task GetCurrentPricesAsync_WithMissingClosePrice_ReturnsFailure()
    {
        // Arrange
        List<string> tickers = new() { "AAPL" };

        A.CallTo(() => _marketDataPort.FetchHistoricalDataAsync(
                A<string>._,
                A<TimeFrame>._,
                A<DateTime>._,
                A<DateTime>._))
            .Returns(new List<HistoricalPrice>
            {
                new() { Ticker = "AAPL", DateTime = DateTime.Today, Close = null }
            });

        // Act
        Domain.Common.Result<Dictionary<string, decimal>> result = await _service.GetCurrentPricesAsync(
            tickers,
            _marketDataPort);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeEmpty();
        result.Errors[0].Code.ShouldBe("CLOSING_PRICE_MISSING");
    }

    [Fact]
    public async Task GetCurrentPricesAsync_WithMultipleTickersAndOneFailure_ReturnsFailure()
    {
        // Arrange
        List<string> tickers = new() { "AAPL", "INVALID" };

        A.CallTo(() => _marketDataPort.FetchHistoricalDataAsync(
                "AAPL",
                A<TimeFrame>._,
                A<DateTime>._,
                A<DateTime>._))
            .Returns(new List<HistoricalPrice>
            {
                new() { Ticker = "AAPL", DateTime = DateTime.Today, Close = 150.00m }
            });

        A.CallTo(() => _marketDataPort.FetchHistoricalDataAsync(
                "INVALID",
                A<TimeFrame>._,
                A<DateTime>._,
                A<DateTime>._))
            .Returns(new List<HistoricalPrice>());

        // Act
        Domain.Common.Result<Dictionary<string, decimal>> result = await _service.GetCurrentPricesAsync(
            tickers,
            _marketDataPort);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GetLatestPriceAsync_WithValidTicker_ReturnsSuccess()
    {
        // Arrange
        string ticker = "AAPL";

        A.CallTo(() => _marketDataPort.FetchHistoricalDataAsync(
                ticker,
                TimeFrame.D1,
                A<DateTime>._,
                A<DateTime>._))
            .Returns(new List<HistoricalPrice>
            {
                new() { Ticker = ticker, DateTime = DateTime.Today.AddDays(-1), Close = 149.00m },
                new() { Ticker = ticker, DateTime = DateTime.Today, Close = 150.00m }
            });

        // Act
        Domain.Common.Result<decimal> result = await _service.GetLatestPriceAsync(ticker, _marketDataPort);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(150.00m); // Should get the most recent price
    }

    [Fact]
    public async Task GetLatestPriceAsync_WithException_ReturnsFailure()
    {
        // Arrange
        string ticker = "AAPL";

        A.CallTo(() => _marketDataPort.FetchHistoricalDataAsync(
                A<string>._,
                A<TimeFrame>._,
                A<DateTime>._,
                A<DateTime>._))
            .Throws(new Exception("Network error"));

        // Act
        Domain.Common.Result<decimal> result = await _service.GetLatestPriceAsync(ticker, _marketDataPort);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeEmpty();
        result.Errors[0].Code.ShouldBe("PRICE_FETCH_FAILED");
        result.Errors[0].Message.ShouldContain("Network error");
    }

    [Fact]
    public async Task GetLatestPriceAsync_FetchesLast7Days()
    {
        // Arrange
        string ticker = "AAPL";
        DateTime? actualStartDate = null;

        A.CallTo(() => _marketDataPort.FetchHistoricalDataAsync(
                ticker,
                TimeFrame.D1,
                A<DateTime>._,
                A<DateTime>._))
            .Invokes((string t, TimeFrame tf, DateTime start, DateTime end) => actualStartDate = start)
            .Returns(new List<HistoricalPrice>
            {
                new() { Ticker = ticker, DateTime = DateTime.Today, Close = 150.00m }
            });

        // Act
        await _service.GetLatestPriceAsync(ticker, _marketDataPort);

        // Assert
        actualStartDate.ShouldNotBeNull();
        actualStartDate.Value.ShouldBe(DateTime.Today.AddDays(-7));
    }
}
