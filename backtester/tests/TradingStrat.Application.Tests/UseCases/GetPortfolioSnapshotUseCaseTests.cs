using FakeItEasy;
using Shouldly;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.Services;
using TradingStrat.Application.Tests.TestDoubles;
using TradingStrat.Application.UseCases;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Tests.UseCases;

public class GetPortfolioSnapshotUseCaseTests
{
    private readonly InMemoryPortfolioRepository _portfolioPort;
    private readonly IMarketDataPort _marketDataPortFake;
    private readonly MarketPriceService _priceService;
    private readonly PortfolioValuationService _valuationService;
    private readonly GetPortfolioSnapshotUseCase _useCase;

    public GetPortfolioSnapshotUseCaseTests()
    {
        _portfolioPort = new InMemoryPortfolioRepository();
        _marketDataPortFake = A.Fake<IMarketDataPort>();
        _priceService = new MarketPriceService();
        _valuationService = new PortfolioValuationService();

        _useCase = new GetPortfolioSnapshotUseCase(
            _portfolioPort,
            _marketDataPortFake,
            _priceService,
            _valuationService);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyPortfolio_ShouldReturnCashOnlySnapshot()
    {
        // Arrange
        Portfolio portfolio = await _portfolioPort.CreatePortfolioAsync("Cash Portfolio", null, 10000m);

        // Act
        Result<PortfolioSnapshot> result = await _useCase.ExecuteAsync(portfolio.Id);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        PortfolioSnapshot snapshot = result.Value;
        snapshot.ShouldNotBeNull();
        snapshot.PortfolioId.ShouldBe(portfolio.Id);
        snapshot.PortfolioName.ShouldBe("Cash Portfolio");
        snapshot.Cash.ShouldBe(10000m);
        snapshot.Positions.ShouldBeEmpty();
        snapshot.TotalValue.ShouldBe(10000m);
        snapshot.TotalCost.ShouldBe(10000m);
        snapshot.UnrealizedGainLoss.ShouldBe(0m);
        snapshot.UnrealizedGainLossPercentage.ShouldBe(0m);
    }

    [Fact]
    public async Task ExecuteAsync_WithSinglePosition_ShouldReturnSnapshot()
    {
        // Arrange
        Portfolio portfolio = await _portfolioPort.CreatePortfolioAsync("Test Portfolio", null, 5000m);
        await _portfolioPort.AddPositionAsync(new Position
        {
            PortfolioId = portfolio.Id,
            Ticker = "AAPL",
            Quantity = 100,
            EntryPrice = 150m,
            EntryDate = DateTime.Today.AddDays(-30)
        });

        // Mock market data to return current price of $160
        A.CallTo(() => _marketDataPortFake.FetchHistoricalDataAsync(
                "AAPL",
                A<TimeFrame>.Ignored,
                A<DateTime>.Ignored,
                A<DateTime>.Ignored))
            .Returns(new List<HistoricalPrice>
            {
                new()
                {
                    Ticker = "AAPL",
                    DateTime = DateTime.Today,
                    Close = 160m
                }
            });

        // Act
        Result<PortfolioSnapshot> result = await _useCase.ExecuteAsync(portfolio.Id);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        PortfolioSnapshot snapshot = result.Value;
        snapshot.ShouldNotBeNull();
        snapshot.Cash.ShouldBe(5000m);
        snapshot.Positions.Count.ShouldBe(1);

        PositionSnapshot position = snapshot.Positions[0];
        position.Ticker.ShouldBe("AAPL");
        position.Quantity.ShouldBe(100);
        position.EntryPrice.ShouldBe(150m);
        position.CurrentPrice.ShouldBe(160m);
        position.CostBasis.ShouldBe(15000m); // 100 * 150
        position.MarketValue.ShouldBe(16000m); // 100 * 160
        position.UnrealizedGainLoss.ShouldBe(1000m); // 16000 - 15000
        position.UnrealizedGainLossPercentage.ShouldBe(6.666666666666666666666666667m, 0.01m);

        snapshot.TotalValue.ShouldBe(21000m); // 5000 cash + 16000 position
        snapshot.TotalCost.ShouldBe(20000m); // 5000 cash + 15000 cost basis
        snapshot.UnrealizedGainLoss.ShouldBe(1000m);

        A.CallTo(() => _marketDataPortFake.FetchHistoricalDataAsync(
                "AAPL",
                A<TimeFrame>.Ignored,
                A<DateTime>.Ignored,
                A<DateTime>.Ignored))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteAsync_WithMultiplePositions_ShouldReturnCompleteSnapshot()
    {
        // Arrange
        Portfolio portfolio = await _portfolioPort.CreatePortfolioAsync("Diversified Portfolio", null, 2000m);

        await _portfolioPort.AddPositionAsync(new Position
        {
            PortfolioId = portfolio.Id,
            Ticker = "AAPL",
            Quantity = 100,
            EntryPrice = 150m,
            EntryDate = DateTime.Today.AddDays(-30)
        });

        await _portfolioPort.AddPositionAsync(new Position
        {
            PortfolioId = portfolio.Id,
            Ticker = "MSFT",
            Quantity = 50,
            EntryPrice = 300m,
            EntryDate = DateTime.Today.AddDays(-20)
        });

        await _portfolioPort.AddPositionAsync(new Position
        {
            PortfolioId = portfolio.Id,
            Ticker = "GOOGL",
            Quantity = 25,
            EntryPrice = 2500m,
            EntryDate = DateTime.Today.AddDays(-10)
        });

        // Mock market data
        A.CallTo(() => _marketDataPortFake.FetchHistoricalDataAsync("AAPL", A<TimeFrame>.Ignored, A<DateTime>.Ignored, A<DateTime>.Ignored))
            .Returns(new List<HistoricalPrice> { new() { Ticker = "AAPL", DateTime = DateTime.Today, Close = 155m } });

        A.CallTo(() => _marketDataPortFake.FetchHistoricalDataAsync("MSFT", A<TimeFrame>.Ignored, A<DateTime>.Ignored, A<DateTime>.Ignored))
            .Returns(new List<HistoricalPrice> { new() { Ticker = "MSFT", DateTime = DateTime.Today, Close = 310m } });

        A.CallTo(() => _marketDataPortFake.FetchHistoricalDataAsync("GOOGL", A<TimeFrame>.Ignored, A<DateTime>.Ignored, A<DateTime>.Ignored))
            .Returns(new List<HistoricalPrice> { new() { Ticker = "GOOGL", DateTime = DateTime.Today, Close = 2450m } });

        // Act
        Result<PortfolioSnapshot> result = await _useCase.ExecuteAsync(portfolio.Id);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        PortfolioSnapshot snapshot = result.Value;
        snapshot.ShouldNotBeNull();
        snapshot.Positions.Count.ShouldBe(3);

        // AAPL: 100 * 150 = 15000 cost, 100 * 155 = 15500 market
        PositionSnapshot aaplPosition = snapshot.Positions.First(p => p.Ticker == "AAPL");
        aaplPosition.UnrealizedGainLoss.ShouldBe(500m);

        // MSFT: 50 * 300 = 15000 cost, 50 * 310 = 15500 market
        PositionSnapshot msftPosition = snapshot.Positions.First(p => p.Ticker == "MSFT");
        msftPosition.UnrealizedGainLoss.ShouldBe(500m);

        // GOOGL: 25 * 2500 = 62500 cost, 25 * 2450 = 61250 market (loss)
        PositionSnapshot googlPosition = snapshot.Positions.First(p => p.Ticker == "GOOGL");
        googlPosition.UnrealizedGainLoss.ShouldBe(-1250m);

        // Total: 2000 cash + 92250 positions = 94250 market value
        // Total cost: 2000 cash + 92500 positions = 94500
        snapshot.TotalValue.ShouldBe(94250m);
        snapshot.TotalCost.ShouldBe(94500m);
        snapshot.UnrealizedGainLoss.ShouldBe(-250m);
    }

    [Fact]
    public async Task ExecuteAsync_WithProfitablePositions_ShouldCalculateCorrectGains()
    {
        // Arrange
        Portfolio portfolio = await _portfolioPort.CreatePortfolioAsync("Winning Portfolio", null, 1000m);

        await _portfolioPort.AddPositionAsync(new Position
        {
            PortfolioId = portfolio.Id,
            Ticker = "TSLA",
            Quantity = 10,
            EntryPrice = 200m,
            EntryDate = DateTime.Today.AddDays(-60)
        });

        // Price doubled
        A.CallTo(() => _marketDataPortFake.FetchHistoricalDataAsync("TSLA", A<TimeFrame>.Ignored, A<DateTime>.Ignored, A<DateTime>.Ignored))
            .Returns(new List<HistoricalPrice>
            {
                new() { Ticker = "TSLA", DateTime = DateTime.Today, Close = 400m }
            });

        // Act
        Result<PortfolioSnapshot> result = await _useCase.ExecuteAsync(portfolio.Id);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        PortfolioSnapshot snapshot = result.Value;
        PositionSnapshot position = snapshot.Positions[0];
        position.UnrealizedGainLoss.ShouldBe(2000m); // (400 - 200) * 10
        position.UnrealizedGainLossPercentage.ShouldBe(100m); // 100% gain

        snapshot.UnrealizedGainLoss.ShouldBe(2000m);
    }

    [Fact]
    public async Task ExecuteAsync_WithLosingPositions_ShouldCalculateCorrectLosses()
    {
        // Arrange
        Portfolio portfolio = await _portfolioPort.CreatePortfolioAsync("Losing Portfolio", null, 1000m);

        await _portfolioPort.AddPositionAsync(new Position
        {
            PortfolioId = portfolio.Id,
            Ticker = "SNAP",
            Quantity = 100,
            EntryPrice = 50m,
            EntryDate = DateTime.Today.AddDays(-90)
        });

        // Price halved
        A.CallTo(() => _marketDataPortFake.FetchHistoricalDataAsync("SNAP", A<TimeFrame>.Ignored, A<DateTime>.Ignored, A<DateTime>.Ignored))
            .Returns(new List<HistoricalPrice>
            {
                new() { Ticker = "SNAP", DateTime = DateTime.Today, Close = 25m }
            });

        // Act
        Result<PortfolioSnapshot> result = await _useCase.ExecuteAsync(portfolio.Id);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        PortfolioSnapshot snapshot = result.Value;
        PositionSnapshot position = snapshot.Positions[0];
        position.UnrealizedGainLoss.ShouldBe(-2500m); // (25 - 50) * 100
        position.UnrealizedGainLossPercentage.ShouldBe(-50m); // 50% loss

        snapshot.UnrealizedGainLoss.ShouldBe(-2500m);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentPortfolio_ShouldReturnFailure()
    {
        // Act
        Result<PortfolioSnapshot> result = await _useCase.ExecuteAsync(9999);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].Type.ShouldBe(ErrorType.NotFound);
        result.Errors[0].Code.ShouldBe("PORTFOLIO_NOT_FOUND");
        result.Errors[0].Message.ShouldContain("Portfolio 9999 not found");
    }

    [Fact]
    public async Task ExecuteAsync_WhenMarketDataUnavailable_ShouldReturnFailure()
    {
        // Arrange
        Portfolio portfolio = await _portfolioPort.CreatePortfolioAsync("Test Portfolio", null, 5000m);
        await _portfolioPort.AddPositionAsync(new Position
        {
            PortfolioId = portfolio.Id,
            Ticker = "UNKNOWN",
            Quantity = 100,
            EntryPrice = 100m,
            EntryDate = DateTime.Today
        });

        // Mock market data returning empty result
        A.CallTo(() => _marketDataPortFake.FetchHistoricalDataAsync(
                "UNKNOWN",
                A<TimeFrame>.Ignored,
                A<DateTime>.Ignored,
                A<DateTime>.Ignored))
            .Returns(new List<HistoricalPrice>());

        // Act
        Result<PortfolioSnapshot> result = await _useCase.ExecuteAsync(portfolio.Id);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeEmpty();
        result.Errors[0].Message.ShouldContain("No recent price data available for UNKNOWN");
    }

    [Fact]
    public async Task ExecuteAsync_WhenClosePriceIsNull_ShouldReturnFailure()
    {
        // Arrange
        Portfolio portfolio = await _portfolioPort.CreatePortfolioAsync("Test Portfolio", null, 5000m);
        await _portfolioPort.AddPositionAsync(new Position
        {
            PortfolioId = portfolio.Id,
            Ticker = "INVALID",
            Quantity = 100,
            EntryPrice = 100m,
            EntryDate = DateTime.Today
        });

        // Mock market data returning data with null Close price
        A.CallTo(() => _marketDataPortFake.FetchHistoricalDataAsync(
                "INVALID",
                A<TimeFrame>.Ignored,
                A<DateTime>.Ignored,
                A<DateTime>.Ignored))
            .Returns(new List<HistoricalPrice>
            {
                new()
                {
                    Ticker = "INVALID",
                    DateTime = DateTime.Today,
                    Close = null // Null closing price
                }
            });

        // Act
        Result<PortfolioSnapshot> result = await _useCase.ExecuteAsync(portfolio.Id);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeEmpty();
        result.Errors[0].Message.ShouldContain("No closing price available for INVALID");
    }

    [Fact]
    public async Task ExecuteAsync_WithProgressReporting_ShouldReportProgress()
    {
        // Arrange
        Portfolio portfolio = await _portfolioPort.CreatePortfolioAsync("Test Portfolio", null, 5000m);
        await _portfolioPort.AddPositionAsync(new Position
        {
            PortfolioId = portfolio.Id,
            Ticker = "AAPL",
            Quantity = 100,
            EntryPrice = 150m,
            EntryDate = DateTime.Today
        });

        A.CallTo(() => _marketDataPortFake.FetchHistoricalDataAsync("AAPL", A<TimeFrame>.Ignored, A<DateTime>.Ignored, A<DateTime>.Ignored))
            .Returns(new List<HistoricalPrice>
            {
                new() { Ticker = "AAPL", DateTime = DateTime.Today, Close = 160m }
            });

        var progressMessages = new List<string>();
        var progress = new Progress<string>(msg => progressMessages.Add(msg));

        // Act
        await _useCase.ExecuteAsync(portfolio.Id, progress);

        // Assert
        progressMessages.ShouldNotBeEmpty();
        progressMessages.ShouldContain(msg => msg.Contains("Loading portfolio"));
        progressMessages.ShouldContain(msg => msg.Contains("Fetching current market prices"));
        progressMessages.ShouldContain(msg => msg.Contains("Fetching price for AAPL"));
        progressMessages.ShouldContain(msg => msg.Contains("Calculating portfolio valuation"));
        progressMessages.ShouldContain(msg => msg.Contains("Portfolio snapshot complete"));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFetchRecentPriceData()
    {
        // Arrange
        Portfolio portfolio = await _portfolioPort.CreatePortfolioAsync("Test Portfolio", null, 5000m);
        await _portfolioPort.AddPositionAsync(new Position
        {
            PortfolioId = portfolio.Id,
            Ticker = "AAPL",
            Quantity = 100,
            EntryPrice = 150m,
            EntryDate = DateTime.Today
        });

        A.CallTo(() => _marketDataPortFake.FetchHistoricalDataAsync("AAPL", A<TimeFrame>.Ignored, A<DateTime>.Ignored, A<DateTime>.Ignored))
            .Returns(new List<HistoricalPrice>
            {
                new() { Ticker = "AAPL", DateTime = DateTime.Today, Close = 160m }
            });

        // Act
        await _useCase.ExecuteAsync(portfolio.Id);

        // Assert - verify that it fetches last 7 days of data
        A.CallTo(() => _marketDataPortFake.FetchHistoricalDataAsync(
                "AAPL",
                A<TimeFrame>.Ignored,
                A<DateTime>.That.Matches(d => d >= DateTime.Today.AddDays(-8) && d <= DateTime.Today.AddDays(-6)),
                DateTime.Today))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCalculateAllocationPercentages()
    {
        // Arrange
        Portfolio portfolio = await _portfolioPort.CreatePortfolioAsync("Test Portfolio", null, 10000m);

        await _portfolioPort.AddPositionAsync(new Position
        {
            PortfolioId = portfolio.Id,
            Ticker = "AAPL",
            Quantity = 100,
            EntryPrice = 150m,
            EntryDate = DateTime.Today
        });

        await _portfolioPort.AddPositionAsync(new Position
        {
            PortfolioId = portfolio.Id,
            Ticker = "MSFT",
            Quantity = 100,
            EntryPrice = 300m,
            EntryDate = DateTime.Today
        });

        // Current prices same as entry (no gain/loss)
        A.CallTo(() => _marketDataPortFake.FetchHistoricalDataAsync("AAPL", A<TimeFrame>.Ignored, A<DateTime>.Ignored, A<DateTime>.Ignored))
            .Returns(new List<HistoricalPrice> { new() { Ticker = "AAPL", DateTime = DateTime.Today, Close = 150m } });

        A.CallTo(() => _marketDataPortFake.FetchHistoricalDataAsync("MSFT", A<TimeFrame>.Ignored, A<DateTime>.Ignored, A<DateTime>.Ignored))
            .Returns(new List<HistoricalPrice> { new() { Ticker = "MSFT", DateTime = DateTime.Today, Close = 300m } });

        // Act
        Result<PortfolioSnapshot> result = await _useCase.ExecuteAsync(portfolio.Id);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        PortfolioSnapshot snapshot = result.Value;
        // Total value: 10000 cash + 15000 AAPL + 30000 MSFT = 55000
        snapshot.TotalValue.ShouldBe(55000m);

        PositionSnapshot aaplPosition = snapshot.Positions.First(p => p.Ticker == "AAPL");
        // AAPL allocation: 15000 / 55000 = 27.27%
        aaplPosition.AllocationPercentage.ShouldBe(27.272727272727272727272727273m, 0.01m);

        PositionSnapshot msftPosition = snapshot.Positions.First(p => p.Ticker == "MSFT");
        // MSFT allocation: 30000 / 55000 = 54.55%
        msftPosition.AllocationPercentage.ShouldBe(54.545454545454545454545454545m, 0.01m);

        // Cash is 10000 / 55000 = 18.18% (not explicitly shown in position allocation)
    }
}
