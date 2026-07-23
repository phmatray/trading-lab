// <copyright file="TradeRepositoryTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using TradingBot.Core.Enums;
using TradingBot.Core.Models.Trading;
using TradingBot.Infrastructure.Persistence;
using TradingBot.Infrastructure.Persistence.Repositories;

namespace TradingBot.Infrastructure.Tests.Persistence.Repositories;

/// <summary>
/// Unit tests for TradeRepository.
/// </summary>
public class TradeRepositoryTests : IDisposable
{
    private readonly TradingBotDbContext _context;
    private readonly TradeRepository _repository;

    public TradeRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<TradingBotDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TradingBotDbContext(options);
        _repository = new TradeRepository(_context);
    }

    [Fact]
    public async Task GetBySymbolAsync_ShouldReturnTradesForSymbol()
    {
        // Arrange
        await _repository.AddAsync(CreateSampleTrade());
        await _repository.AddAsync(CreateSampleTrade());
        await _repository.AddAsync(CreateSampleTrade("AAPL"));
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetBySymbolAsync("SPY");

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldAllBe(t => t.Symbol == "SPY");
    }

    [Fact]
    public async Task GetByDateRangeAsync_ShouldReturnTradesInRange()
    {
        // Arrange
        var now = DateTime.UtcNow;
        await _repository.AddAsync(CreateSampleTrade(entryTime: now.AddDays(-5), exitTime: now.AddDays(-4)));
        await _repository.AddAsync(CreateSampleTrade("AAPL", entryTime: now.AddDays(-3), exitTime: now.AddDays(-2)));
        await _repository.AddAsync(CreateSampleTrade("MSFT", entryTime: now.AddDays(-1), exitTime: now));
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetByDateRangeAsync(now.AddDays(-4), now.AddDays(-1));

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].Symbol.ShouldBe("AAPL");
    }

    [Fact]
    public async Task GetByStrategyAsync_ShouldReturnTradesForStrategy()
    {
        // Arrange
        await _repository.AddAsync(CreateSampleTrade(strategyName: "momentum"));
        await _repository.AddAsync(CreateSampleTrade("AAPL", strategyName: "momentum"));
        await _repository.AddAsync(CreateSampleTrade("MSFT", strategyName: "meanreversion"));
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStrategyAsync("momentum");

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldAllBe(t => t.StrategyName == "momentum");
    }

    [Fact]
    public async Task GetWinningTradesAsync_ShouldReturnOnlyProfitableTrades()
    {
        // Arrange
        await _repository.AddAsync(CreateSampleTrade(realizedPnL: 100m));
        await _repository.AddAsync(CreateSampleTrade("AAPL", realizedPnL: -50m));
        await _repository.AddAsync(CreateSampleTrade("MSFT", realizedPnL: 75m));
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetWinningTradesAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldAllBe(t => t.RealizedPnL > 0);
        result[0].RealizedPnL.ShouldBe(100m); // Ordered by PnL descending
    }

    [Fact]
    public async Task GetLosingTradesAsync_ShouldReturnOnlyLosingTrades()
    {
        // Arrange
        await _repository.AddAsync(CreateSampleTrade(realizedPnL: 100m));
        await _repository.AddAsync(CreateSampleTrade("AAPL", realizedPnL: -50m));
        await _repository.AddAsync(CreateSampleTrade("MSFT", realizedPnL: -75m));
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetLosingTradesAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldAllBe(t => t.RealizedPnL < 0);
        result[0].RealizedPnL.ShouldBe(-75m); // Ordered by PnL ascending (most loss first)
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllTrades()
    {
        // Arrange
        await _repository.AddAsync(CreateSampleTrade());
        await _repository.AddAsync(CreateSampleTrade("AAPL"));
        await _repository.AddAsync(CreateSampleTrade("MSFT"));
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private static Trade CreateSampleTrade(
        string symbol = "SPY",
        string strategyName = "test_strategy",
        decimal realizedPnL = 50m,
        DateTime? entryTime = null,
        DateTime? exitTime = null)
    {
        // Calculate prices to achieve desired PnL
        const decimal entryPrice = 450.00m;
        const decimal commission = 2m;
        const decimal quantity = 10m;

        // For buy side: (exitPrice - entryPrice) * quantity - commission = realizedPnL
        // exitPrice = (realizedPnL + commission) / quantity + entryPrice
        var exitPrice = realizedPnL >= 0
            ? ((realizedPnL + commission) / quantity) + entryPrice
            : entryPrice + ((realizedPnL + commission) / quantity);

        return new Trade
        {
            Id = Guid.NewGuid(),
            Symbol = symbol,
            Side = OrderSide.Buy,
            Quantity = quantity,
            EntryPrice = entryPrice,
            ExitPrice = exitPrice,
            EntryTime = entryTime ?? DateTime.UtcNow.AddHours(-1),
            ExitTime = exitTime ?? DateTime.UtcNow,
            Commission = commission,
            StrategyName = strategyName,
        };
    }
}
