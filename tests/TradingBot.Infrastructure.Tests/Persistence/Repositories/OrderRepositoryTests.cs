// <copyright file="OrderRepositoryTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using TradingBot.Core.Enums;
using TradingBot.Core.Models.Trading;
using TradingBot.Infrastructure.Persistence;
using TradingBot.Infrastructure.Persistence.Repositories;

namespace TradingBot.Infrastructure.Tests.Persistence.Repositories;

/// <summary>
/// Unit tests for OrderRepository.
/// </summary>
public class OrderRepositoryTests : IDisposable
{
    private readonly TradingBotDbContext _context;
    private readonly OrderRepository _repository;

    public OrderRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<TradingBotDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TradingBotDbContext(options);
        _repository = new OrderRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddOrderToDatabase()
    {
        // Arrange
        var order = CreateSampleOrder();

        // Act
        await _repository.AddAsync(order);
        await _repository.SaveChangesAsync();

        // Assert
        var retrieved = await _repository.GetByIdAsync(order.Id);
        retrieved.ShouldNotBeNull();
        retrieved.Symbol.ShouldBe("SPY");
        retrieved.Quantity.ShouldBe(10m);
    }

    [Fact]
    public async Task GetByIdAsync_WhenOrderExists_ShouldReturnOrder()
    {
        // Arrange
        var order = CreateSampleOrder();
        await _repository.AddAsync(order);
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(order.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(order.Id);
        result.Symbol.ShouldBe(order.Symbol);
    }

    [Fact]
    public async Task GetByIdAsync_WhenOrderDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllOrders()
    {
        // Arrange
        await _repository.AddAsync(CreateSampleOrder());
        await _repository.AddAsync(CreateSampleOrder("AAPL"));
        await _repository.AddAsync(CreateSampleOrder("MSFT"));
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetBySymbolAsync_ShouldReturnOrdersForSymbol()
    {
        // Arrange
        await _repository.AddAsync(CreateSampleOrder());
        await _repository.AddAsync(CreateSampleOrder());
        await _repository.AddAsync(CreateSampleOrder("AAPL"));
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetBySymbolAsync("SPY");

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldAllBe(o => o.Symbol == "SPY");
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldReturnOrdersWithStatus()
    {
        // Arrange
        await _repository.AddAsync(CreateSampleOrder("SPY", OrderStatus.Pending));
        await _repository.AddAsync(CreateSampleOrder("AAPL", OrderStatus.Filled));
        await _repository.AddAsync(CreateSampleOrder("MSFT", OrderStatus.Pending));
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStatusAsync(OrderStatus.Pending);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldAllBe(o => o.Status == OrderStatus.Pending);
    }

    [Fact]
    public async Task GetByDateRangeAsync_ShouldReturnOrdersInRange()
    {
        // Arrange
        var now = DateTime.UtcNow;
        await _repository.AddAsync(CreateSampleOrder(createdAt: now.AddDays(-5)));
        await _repository.AddAsync(CreateSampleOrder("AAPL", createdAt: now.AddDays(-3)));
        await _repository.AddAsync(CreateSampleOrder("MSFT", createdAt: now.AddDays(-1)));
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetByDateRangeAsync(now.AddDays(-4), now.AddDays(-2));

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].Symbol.ShouldBe("AAPL");
    }

    [Fact]
    public async Task GetOpenOrdersAsync_ShouldReturnOnlyOpenOrders()
    {
        // Arrange
        await _repository.AddAsync(CreateSampleOrder("SPY", OrderStatus.Pending));
        await _repository.AddAsync(CreateSampleOrder("AAPL", OrderStatus.Submitted));
        await _repository.AddAsync(CreateSampleOrder("MSFT", OrderStatus.PartiallyFilled));
        await _repository.AddAsync(CreateSampleOrder("TSLA", OrderStatus.Filled));
        await _repository.AddAsync(CreateSampleOrder("NVDA", OrderStatus.Cancelled));
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetOpenOrdersAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        result.ShouldNotContain(o => o.Status == OrderStatus.Filled);
        result.ShouldNotContain(o => o.Status == OrderStatus.Cancelled);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateOrderInDatabase()
    {
        // Arrange
        var order = CreateSampleOrder();
        await _repository.AddAsync(order);
        await _repository.SaveChangesAsync();

        // Act
        order.Status = OrderStatus.Filled;
        order.FilledQuantity = 10m;
        order.AverageFillPrice = 450.50m;
        await _repository.UpdateAsync(order);
        await _repository.SaveChangesAsync();

        // Assert
        var updated = await _repository.GetByIdAsync(order.Id);
        updated.ShouldNotBeNull();
        updated.Status.ShouldBe(OrderStatus.Filled);
        updated.FilledQuantity.ShouldBe(10m);
        updated.AverageFillPrice.ShouldBe(450.50m);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveOrderFromDatabase()
    {
        // Arrange
        var order = CreateSampleOrder();
        await _repository.AddAsync(order);
        await _repository.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(order);
        await _repository.SaveChangesAsync();

        // Assert
        var deleted = await _repository.GetByIdAsync(order.Id);
        deleted.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_WithPredicate_ShouldReturnMatchingOrders()
    {
        // Arrange
        await _repository.AddAsync(CreateSampleOrder(quantity: 5m));
        await _repository.AddAsync(CreateSampleOrder("AAPL", quantity: 15m));
        await _repository.AddAsync(CreateSampleOrder("MSFT", quantity: 25m));
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.FindAsync(o => o.Quantity > 10m);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldAllBe(o => o.Quantity > 10m);
    }

    [Fact]
    public async Task GetBySymbolAsync_ShouldOrderByCreatedAtDescending()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var order1 = CreateSampleOrder(createdAt: now.AddMinutes(-10));
        var order2 = CreateSampleOrder(createdAt: now.AddMinutes(-5));
        var order3 = CreateSampleOrder(createdAt: now);

        await _repository.AddAsync(order1);
        await _repository.AddAsync(order2);
        await _repository.AddAsync(order3);
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetBySymbolAsync("SPY");

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        result[0].Id.ShouldBe(order3.Id); // Most recent first
        result.Last().Id.ShouldBe(order1.Id);  // Oldest last
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private static Order CreateSampleOrder(
        string symbol = "SPY",
        OrderStatus? status = null,
        decimal quantity = 10m,
        DateTime? createdAt = null)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            Symbol = symbol,
            Type = OrderType.Market,
            Side = OrderSide.Buy,
            Quantity = quantity,
            Status = status ?? OrderStatus.Pending,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            FilledQuantity = 0m,
            AverageFillPrice = 0m,
            Commission = 0m,
            StrategyName = "test_strategy",
        };
    }
}
