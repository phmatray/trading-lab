// <copyright file="PositionRepositoryTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using TradingBot.Core.Enums;
using TradingBot.Core.Models.Trading;
using TradingBot.Infrastructure.Persistence;
using TradingBot.Infrastructure.Persistence.Repositories;

namespace TradingBot.Infrastructure.Tests.Persistence.Repositories;

/// <summary>
/// Unit tests for PositionRepository.
/// </summary>
public class PositionRepositoryTests : IDisposable
{
    private readonly TradingBotDbContext _context;
    private readonly PositionRepository _repository;

    public PositionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<TradingBotDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TradingBotDbContext(options);
        _repository = new PositionRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddPositionToDatabase()
    {
        // Arrange
        var position = CreateSamplePosition();

        // Act
        await _repository.AddAsync(position);
        await _repository.SaveChangesAsync();

        // Assert
        var retrieved = await _repository.GetByIdAsync(position.Id);
        retrieved.ShouldNotBeNull();
        retrieved.Symbol.ShouldBe("SPY");
        retrieved.Quantity.ShouldBe(10m);
    }

    [Fact]
    public async Task GetBySymbolAsync_WhenPositionExists_ShouldReturnPosition()
    {
        // Arrange
        var position = CreateSamplePosition();
        await _repository.AddAsync(position);
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetBySymbolAsync("SPY");

        // Assert
        result.ShouldNotBeNull();
        result.Symbol.ShouldBe("SPY");
    }

    [Fact]
    public async Task GetBySymbolAsync_WhenPositionDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetBySymbolAsync("NONEXISTENT");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetOpenPositionsAsync_ShouldReturnOnlyNonZeroQuantityPositions()
    {
        // Arrange
        await _repository.AddAsync(CreateSamplePosition());
        await _repository.AddAsync(CreateSamplePosition("AAPL", quantity: 0m)); // Closed position
        await _repository.AddAsync(CreateSamplePosition("MSFT", quantity: 15m));
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetOpenPositionsAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldAllBe(p => p.Quantity != 0);
    }

    [Fact]
    public async Task GetByStrategyAsync_ShouldReturnPositionsForStrategy()
    {
        // Arrange
        await _repository.AddAsync(CreateSamplePosition(strategyName: "momentum"));
        await _repository.AddAsync(CreateSamplePosition("AAPL", strategyName: "momentum"));
        await _repository.AddAsync(CreateSamplePosition("MSFT", strategyName: "meanreversion"));
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStrategyAsync("momentum");

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldAllBe(p => p.StrategyName == "momentum");
    }

    [Fact]
    public async Task GetOpenPositionsAsync_ShouldOrderByOpenedAtDescending()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var pos1 = CreateSamplePosition(openedAt: now.AddMinutes(-10));
        var pos2 = CreateSamplePosition("AAPL", openedAt: now.AddMinutes(-5));
        var pos3 = CreateSamplePosition("MSFT", openedAt: now);

        await _repository.AddAsync(pos1);
        await _repository.AddAsync(pos2);
        await _repository.AddAsync(pos3);
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetOpenPositionsAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        result[0].Id.ShouldBe(pos3.Id); // Most recent first
        result[^1].Id.ShouldBe(pos1.Id);  // Oldest last
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdatePositionInDatabase()
    {
        // Arrange
        var position = CreateSamplePosition();
        await _repository.AddAsync(position);
        await _repository.SaveChangesAsync();

        // Act
        position.Quantity = 15m;
        position.EntryPrice = 452.50m;
        position.CurrentPrice = 455.00m;
        await _repository.UpdateAsync(position);
        await _repository.SaveChangesAsync();

        // Assert
        var updated = await _repository.GetByIdAsync(position.Id);
        updated.ShouldNotBeNull();
        updated.Quantity.ShouldBe(15m);
        updated.EntryPrice.ShouldBe(452.50m);
        updated.CurrentPrice.ShouldBe(455.00m);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemovePositionFromDatabase()
    {
        // Arrange
        var position = CreateSamplePosition();
        await _repository.AddAsync(position);
        await _repository.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(position);
        await _repository.SaveChangesAsync();

        // Assert
        var deleted = await _repository.GetByIdAsync(position.Id);
        deleted.ShouldBeNull();
    }

    [Fact]
    public async Task GetBySymbolAsync_WhenMultiplePositions_ShouldReturnFirstMatch()
    {
        // Arrange
        // This shouldn't happen in practice (one position per symbol)
        // but testing the behavior
        var pos1 = CreateSamplePosition();
        var pos2 = CreateSamplePosition();

        await _repository.AddAsync(pos1);
        await _repository.AddAsync(pos2);
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetBySymbolAsync("SPY");

        // Assert
        result.ShouldNotBeNull();
        result.Symbol.ShouldBe("SPY");
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private static Position CreateSamplePosition(
        string symbol = "SPY",
        string strategyName = "test_strategy",
        decimal quantity = 10m,
        DateTime? openedAt = null)
    {
        return new Position
        {
            Id = Guid.NewGuid(),
            Symbol = symbol,
            Side = OrderSide.Buy,
            Quantity = quantity,
            EntryPrice = 450.00m,
            CurrentPrice = 450.00m,
            StopLoss = null,
            TakeProfit = null,
            OpenedAt = openedAt ?? DateTime.UtcNow,
            StrategyName = strategyName,
        };
    }
}
