// <copyright file="PortfolioServiceTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using TradingBot.Core.Enums;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Trading;
using TradingBot.Web.Services;

namespace TradingBot.Web.Tests.Services;

/// <summary>
/// Unit tests for PortfolioService.
/// </summary>
public class PortfolioServiceTests
{
    /// <summary>
    /// Tests that ClosePositionAsync returns true when position exists and is successfully closed.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ClosePositionAsync_ValidPosition_ReturnsTrue()
    {
        // Arrange
        var positionId = Guid.NewGuid();
        var symbol = "AAPL";
        var position = new Position
        {
            Id = positionId,
            Symbol = symbol,
            Side = OrderSide.Buy,
            Quantity = 100m,
            EntryPrice = 150.00m,
            CurrentPrice = 155.00m,
            StopLoss = null,
            TakeProfit = null,
            OpenedAt = DateTime.UtcNow.AddHours(-2),
            StrategyName = "TestStrategy",
        };

        var portfolioManager = A.Fake<IPortfolioManager>();
        A.CallTo(() => portfolioManager.GetPositionsAsync(A<CancellationToken>._))
            .Returns(new List<Position> { position });
        A.CallTo(() => portfolioManager.ClosePositionAsync(symbol, A<CancellationToken>._))
            .Returns(true);

        var logger = A.Fake<ILogger<PortfolioService>>();
        var service = new PortfolioService(portfolioManager, logger);

        // Act
        var result = await service.ClosePositionAsync(positionId, CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
        A.CallTo(() => portfolioManager.GetPositionsAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => portfolioManager.ClosePositionAsync(symbol, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    /// <summary>
    /// Tests that ClosePositionAsync returns false when position is not found.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ClosePositionAsync_PositionNotFound_ReturnsFalse()
    {
        // Arrange
        var positionId = Guid.NewGuid();
        var portfolioManager = A.Fake<IPortfolioManager>();
        A.CallTo(() => portfolioManager.GetPositionsAsync(A<CancellationToken>._))
            .Returns(new List<Position>()); // Empty list - position not found

        var logger = A.Fake<ILogger<PortfolioService>>();
        var service = new PortfolioService(portfolioManager, logger);

        // Act
        var result = await service.ClosePositionAsync(positionId, CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
        A.CallTo(() => portfolioManager.GetPositionsAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => portfolioManager.ClosePositionAsync(A<string>._, A<CancellationToken>._))
            .MustNotHaveHappened(); // Should not attempt to close if position not found
    }

    /// <summary>
    /// Tests that GetOpenPositionsAsync returns the list of positions.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task GetOpenPositionsAsync_ReturnsPositionsList()
    {
        // Arrange
        var positions = new List<Position>
        {
            new Position
            {
                Id = Guid.NewGuid(),
                Symbol = "AAPL",
                Side = OrderSide.Buy,
                Quantity = 100m,
                EntryPrice = 150.00m,
                CurrentPrice = 155.00m,
                StopLoss = null,
                TakeProfit = null,
                OpenedAt = DateTime.UtcNow.AddHours(-2),
                StrategyName = "MomentumStrategy",
            },
            new Position
            {
                Id = Guid.NewGuid(),
                Symbol = "GOOGL",
                Side = OrderSide.Buy,
                Quantity = 50m,
                EntryPrice = 2800.00m,
                CurrentPrice = 2820.00m,
                StopLoss = null,
                TakeProfit = null,
                OpenedAt = DateTime.UtcNow.AddHours(-1),
                StrategyName = "MeanReversionStrategy",
            }
        };

        var portfolioManager = A.Fake<IPortfolioManager>();
        A.CallTo(() => portfolioManager.GetPositionsAsync(A<CancellationToken>._))
            .Returns(positions);

        var logger = A.Fake<ILogger<PortfolioService>>();
        var service = new PortfolioService(portfolioManager, logger);

        // Act
        var result = await service.GetOpenPositionsAsync(CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);
        A.CallTo(() => portfolioManager.GetPositionsAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    /// <summary>
    /// Tests that GetTradeHistoryAsync with filters returns filtered trades.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task GetTradeHistoryAsync_WithFilters_ReturnsFilteredTrades()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;
        var symbol = "AAPL";
        var strategy = "MomentumStrategy";

        var trades = new List<Trade>
        {
            new Trade
            {
                Id = Guid.NewGuid(),
                Symbol = symbol,
                Side = OrderSide.Buy,
                Quantity = 100m,
                EntryPrice = 150.00m,
                ExitPrice = 155.00m,
                EntryTime = startDate.AddDays(1),
                ExitTime = startDate.AddDays(2),
                Commission = 1.00m,
                StrategyName = strategy
            }
        };

        var portfolioManager = A.Fake<IPortfolioManager>();
        A.CallTo(() => portfolioManager.GetTradeHistoryAsync(
                startDate,
                endDate,
                symbol,
                strategy,
                A<CancellationToken>._))
            .Returns(trades);

        var logger = A.Fake<ILogger<PortfolioService>>();
        var service = new PortfolioService(portfolioManager, logger);

        // Act
        var result = await service.GetTradeHistoryAsync(
            startDate,
            endDate,
            symbol,
            strategy,
            CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(1);
        result.First().Symbol.ShouldBe(symbol);
        result.First().StrategyName.ShouldBe(strategy);
    }

    /// <summary>
    /// Tests that ExportTradeHistoryAsync generates valid CSV content.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ExportTradeHistoryAsync_GeneratesValidCsv()
    {
        // Arrange
        var trades = new List<Trade>
        {
            new Trade
            {
                Id = Guid.NewGuid(),
                Symbol = "AAPL",
                Side = OrderSide.Buy,
                Quantity = 100m,
                EntryPrice = 150.00m,
                ExitPrice = 155.00m,
                EntryTime = DateTime.Parse("2024-01-01 10:00:00"),
                ExitTime = DateTime.Parse("2024-01-02 15:30:00"),
                Commission = 1.00m,
                StrategyName = "MomentumStrategy"
            }
        };

        var portfolioManager = A.Fake<IPortfolioManager>();
        A.CallTo(() => portfolioManager.GetTradeHistoryAsync(
                A<DateTime?>._, A<DateTime?>._, A<string?>._, A<string?>._, A<CancellationToken>._))
            .Returns(trades);

        var logger = A.Fake<ILogger<PortfolioService>>();
        var service = new PortfolioService(portfolioManager, logger);

        // Act
        var csv = await service.ExportTradeHistoryAsync(
            null,
            null,
            null,
            null,
            CancellationToken.None);

        // Assert
        csv.ShouldNotBeNullOrEmpty();
        csv.ShouldContain("Symbol,Side,Quantity"); // Header
        csv.ShouldContain("AAPL"); // Data
        csv.ShouldContain("MomentumStrategy");
    }
}
