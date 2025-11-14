// <copyright file="PortfolioManagementIntegrationTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Trading;
using TradingBot.Web.Services;

namespace TradingBot.Web.Tests.Integration;

/// <summary>
/// Integration tests for portfolio management workflows.
/// </summary>
public class PortfolioManagementIntegrationTests
{
    /// <summary>
    /// Tests the complete end-to-end close position workflow.
    /// Simulates: Get positions -> Find position -> Close position -> Verify closure.
    /// </summary>
    [Fact]
    public async Task ClosePosition_EndToEnd_CompletesSuccessfully()
    {
        // Arrange - Setup initial portfolio state
        var positionId = Guid.NewGuid();
        var symbol = "AAPL";

        var openPosition = new Position
        {
            Id = positionId,
            Symbol = symbol,
            Side = OrderSide.Buy,
            Quantity = 100m,
            EntryPrice = 150.00m,
            CurrentPrice = 155.00m,
            UnrealizedPnL = 500m,
            EntryTime = DateTime.UtcNow.AddHours(-2),
            StrategyName = "MomentumStrategy",
            OpenedAt = DateTime.UtcNow.AddHours(-2)
        };

        // Setup mock portfolio manager with realistic behavior
        var portfolioManager = A.Fake<IPortfolioManager>();

        // Step 1: Initial state - position exists
        A.CallTo(() => portfolioManager.GetPositionsAsync(A<CancellationToken>._))
            .Returns(new List<Position> { openPosition });

        // Step 2: Close position succeeds
        A.CallTo(() => portfolioManager.ClosePositionAsync(symbol, A<CancellationToken>._))
            .Returns(true);

        // Step 3: After closure, position list is empty
        A.CallTo(() => portfolioManager.GetPositionsAsync(A<CancellationToken>._))
            .ReturnsNextFromSequence(
                new List<Position> { openPosition }, // First call: position exists
                new List<Position>());               // Second call: position closed

        var logger = A.Fake<ILogger<PortfolioService>>();
        var portfolioService = new PortfolioService(portfolioManager, logger);

        // Act - Execute the close position workflow

        // 1. Verify position exists before closure
        var positionsBeforeClosure = await portfolioService.GetOpenPositionsAsync(CancellationToken.None);
        var positionToClose = positionsBeforeClosure.FirstOrDefault(p => p.Id == positionId);
        positionToClose.ShouldNotBeNull("Position should exist before closure");
        positionToClose.Symbol.ShouldBe(symbol);

        // 2. Close the position
        var closeResult = await portfolioService.ClosePositionAsync(positionId, CancellationToken.None);
        closeResult.ShouldBeTrue("Close position operation should succeed");

        // 3. Verify position is removed after closure
        var positionsAfterClosure = await portfolioService.GetOpenPositionsAsync(CancellationToken.None);
        positionsAfterClosure.ShouldNotContain(p => p.Id == positionId, "Position should not exist after closure");

        // Assert - Verify all expected interactions occurred
        A.CallTo(() => portfolioManager.GetPositionsAsync(A<CancellationToken>._))
            .MustHaveHappened(2, Times.Exactly);
        A.CallTo(() => portfolioManager.ClosePositionAsync(symbol, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    /// <summary>
    /// Tests the close position workflow when position doesn't exist.
    /// </summary>
    [Fact]
    public async Task ClosePosition_NonExistentPosition_ReturnsFalse()
    {
        // Arrange
        var nonExistentPositionId = Guid.NewGuid();
        var portfolioManager = A.Fake<IPortfolioManager>();

        A.CallTo(() => portfolioManager.GetPositionsAsync(A<CancellationToken>._))
            .Returns(new List<Position>()); // No positions

        var logger = A.Fake<ILogger<PortfolioService>>();
        var portfolioService = new PortfolioService(portfolioManager, logger);

        // Act
        var result = await portfolioService.ClosePositionAsync(nonExistentPositionId, CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
        A.CallTo(() => portfolioManager.ClosePositionAsync(A<string>._, A<CancellationToken>._))
            .MustNotHaveHappened(); // Should not attempt to close
    }

    /// <summary>
    /// Tests closing multiple positions in sequence.
    /// </summary>
    [Fact]
    public async Task ClosePosition_MultiplePositions_ClosesEachSuccessfully()
    {
        // Arrange
        var position1 = new Position
        {
            Id = Guid.NewGuid(),
            Symbol = "AAPL",
            Side = OrderSide.Buy,
            Quantity = 100m,
            EntryPrice = 150.00m,
            CurrentPrice = 155.00m,
            UnrealizedPnL = 500m,
            EntryTime = DateTime.UtcNow.AddHours(-2),
            StrategyName = "Strategy1",
            OpenedAt = DateTime.UtcNow.AddHours(-2)
        };

        var position2 = new Position
        {
            Id = Guid.NewGuid(),
            Symbol = "GOOGL",
            Side = OrderSide.Buy,
            Quantity = 50m,
            EntryPrice = 2800.00m,
            CurrentPrice = 2820.00m,
            UnrealizedPnL = 1000m,
            EntryTime = DateTime.UtcNow.AddHours(-1),
            StrategyName = "Strategy2",
            OpenedAt = DateTime.UtcNow.AddHours(-1)
        };

        var portfolioManager = A.Fake<IPortfolioManager>();

        // Configure sequential position list updates
        A.CallTo(() => portfolioManager.GetPositionsAsync(A<CancellationToken>._))
            .ReturnsNextFromSequence(
                new List<Position> { position1, position2 }, // Initial: both positions
                new List<Position> { position2 },            // After closing position1
                new List<Position>());                       // After closing position2

        A.CallTo(() => portfolioManager.ClosePositionAsync("AAPL", A<CancellationToken>._))
            .Returns(true);
        A.CallTo(() => portfolioManager.ClosePositionAsync("GOOGL", A<CancellationToken>._))
            .Returns(true);

        var logger = A.Fake<ILogger<PortfolioService>>();
        var portfolioService = new PortfolioService(portfolioManager, logger);

        // Act
        var initialPositions = await portfolioService.GetOpenPositionsAsync(CancellationToken.None);
        initialPositions.Count().ShouldBe(2);

        var result1 = await portfolioService.ClosePositionAsync(position1.Id, CancellationToken.None);
        var afterFirst = await portfolioService.GetOpenPositionsAsync(CancellationToken.None);

        var result2 = await portfolioService.ClosePositionAsync(position2.Id, CancellationToken.None);
        var afterSecond = await portfolioService.GetOpenPositionsAsync(CancellationToken.None);

        // Assert
        result1.ShouldBeTrue();
        result2.ShouldBeTrue();
        afterFirst.Count().ShouldBe(1);
        afterFirst.First().Symbol.ShouldBe("GOOGL");
        afterSecond.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests the close position workflow with error handling.
    /// </summary>
    [Fact]
    public async Task ClosePosition_PortfolioManagerThrowsException_PropagatesException()
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
            UnrealizedPnL = 500m,
            EntryTime = DateTime.UtcNow,
            StrategyName = "TestStrategy",
            OpenedAt = DateTime.UtcNow
        };

        var portfolioManager = A.Fake<IPortfolioManager>();
        A.CallTo(() => portfolioManager.GetPositionsAsync(A<CancellationToken>._))
            .Returns(new List<Position> { position });
        A.CallTo(() => portfolioManager.ClosePositionAsync(symbol, A<CancellationToken>._))
            .Throws(new InvalidOperationException("Trading is currently halted"));

        var logger = A.Fake<ILogger<PortfolioService>>();
        var portfolioService = new PortfolioService(portfolioManager, logger);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await portfolioService.ClosePositionAsync(positionId, CancellationToken.None);
        });
    }

    /// <summary>
    /// Tests the trade history export after closing a position.
    /// </summary>
    [Fact]
    public async Task ClosePosition_ExportTradeHistory_IncludesClosedPosition()
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
            UnrealizedPnL = 500m,
            EntryTime = DateTime.UtcNow.AddHours(-2),
            StrategyName = "MomentumStrategy",
            OpenedAt = DateTime.UtcNow.AddHours(-2)
        };

        var closedTrade = new Trade
        {
            Id = Guid.NewGuid(),
            Symbol = symbol,
            Side = OrderSide.Buy,
            Quantity = 100m,
            EntryPrice = 150.00m,
            ExitPrice = 155.00m,
            EntryTime = DateTime.UtcNow.AddHours(-2),
            ExitTime = DateTime.UtcNow,
            RealizedPnL = 500m,
            Commission = 1.00m,
            StrategyName = "MomentumStrategy"
        };

        var portfolioManager = A.Fake<IPortfolioManager>();
        A.CallTo(() => portfolioManager.GetPositionsAsync(A<CancellationToken>._))
            .Returns(new List<Position> { position });
        A.CallTo(() => portfolioManager.ClosePositionAsync(symbol, A<CancellationToken>._))
            .Returns(true);
        A.CallTo(() => portfolioManager.GetTradeHistoryAsync(
                A<DateTime?>._, A<DateTime?>._, A<string?>._, A<string?>._, A<CancellationToken>._))
            .Returns(new List<Trade> { closedTrade });

        var logger = A.Fake<ILogger<PortfolioService>>();
        var portfolioService = new PortfolioService(portfolioManager, logger);

        // Act
        var closeResult = await portfolioService.ClosePositionAsync(positionId, CancellationToken.None);
        var csv = await portfolioService.ExportTradeHistoryAsync(
            null, null, null, null, CancellationToken.None);

        // Assert
        closeResult.ShouldBeTrue();
        csv.ShouldContain(symbol);
        csv.ShouldContain("500.00"); // Realized P&L
        csv.ShouldContain("MomentumStrategy");
    }
}
