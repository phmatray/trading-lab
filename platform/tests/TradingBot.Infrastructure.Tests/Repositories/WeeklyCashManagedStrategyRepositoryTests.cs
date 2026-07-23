// <copyright file="WeeklyCashManagedStrategyRepositoryTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Strategy;
using TradingBot.Infrastructure.Persistence;
using Xunit;

namespace TradingBot.Infrastructure.Tests.Repositories;

/// <summary>
/// Integration tests for <see cref="IWeeklyCashManagedStrategyRepository"/> persistence.
/// Uses in-memory SQLite database for testing.
/// </summary>
public sealed class WeeklyCashManagedStrategyRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TradingBotDbContext _context;
    private readonly IWeeklyCashManagedStrategyRepository _repository;

    public WeeklyCashManagedStrategyRepositoryTests()
    {
        // Create in-memory SQLite database
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TradingBotDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new TradingBotDbContext(options, null!); // MediatorDomainEventDispatcher not needed for tests
        _context.Database.EnsureCreated();

        _repository = (IWeeklyCashManagedStrategyRepository)System.Activator.CreateInstance(
            System.Type.GetType("TradingBot.Infrastructure.Persistence.Repositories.WeeklyCashManagedStrategyRepository, TradingBot.Infrastructure")!,
            _context)!;
    }

    [Fact]
    public async Task AddAsync_WithValidStrategy_ShouldPersistStrategy()
    {
        // Arrange
        var strategy = CreateTestStrategy("Test Strategy 1");

        // Act
        var result = await _repository.AddAsync(strategy);
        await _context.SaveChangesAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldNotBe(Guid.Empty);

        var retrieved = await _repository.GetByIdAsync(result.Id);
        retrieved.ShouldNotBeNull();
        retrieved!.Name.ShouldBe("Test Strategy 1");
        retrieved.EtpSymbol.ShouldBe("BTCW");
        retrieved.UnderlyingSymbol.ShouldBe("COIN");
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingStrategy_ShouldReturnStrategy()
    {
        // Arrange
        var strategy = CreateTestStrategy("Test Strategy 2");
        await _repository.AddAsync(strategy);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(strategy.Id);

        // Assert
        result.ShouldNotBeNull();
        result!.Id.ShouldBe(strategy.Id);
        result.Name.ShouldBe("Test Strategy 2");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentStrategy_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithModifiedStrategy_ShouldPersistChanges()
    {
        // Arrange
        var strategy = CreateTestStrategy("Original Name");
        await _repository.AddAsync(strategy);
        await _context.SaveChangesAsync();

        // Act
        strategy.Name = "Updated Name";
        strategy.MinCashRatio = 0.20m;
        await _repository.UpdateAsync(strategy);
        await _context.SaveChangesAsync();

        // Assert
        var retrieved = await _repository.GetByIdAsync(strategy.Id);
        retrieved.ShouldNotBeNull();
        retrieved!.Name.ShouldBe("Updated Name");
        retrieved.MinCashRatio.ShouldBe(0.20m);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingStrategy_ShouldRemoveStrategy()
    {
        // Arrange
        var strategy = CreateTestStrategy("To Delete");
        await _repository.AddAsync(strategy);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(strategy);
        await _context.SaveChangesAsync();

        // Assert
        var retrieved = await _repository.GetByIdAsync(strategy.Id);
        retrieved.ShouldBeNull();
    }

    [Fact]
    public async Task GetEnabledStrategiesAsync_ShouldReturnEnabledStrategies()
    {
        // Arrange
        var strategy1 = CreateTestStrategy("Strategy 1");
        strategy1.IsEnabled = true;

        var strategy2 = CreateTestStrategy("Strategy 2");
        strategy2.IsEnabled = true;

        var strategy3 = CreateTestStrategy("Strategy 3");
        strategy3.IsEnabled = false; // Not enabled

        await _repository.AddAsync(strategy1);
        await _repository.AddAsync(strategy2);
        await _repository.AddAsync(strategy3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetEnabledStrategiesAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBeGreaterThanOrEqualTo(2);
        result.ShouldContain(s => s.Name == "Strategy 1");
        result.ShouldContain(s => s.Name == "Strategy 2");
        result.ShouldNotContain(s => s.Name == "Strategy 3"); // Disabled strategy should not be returned
    }

    [Fact]
    public async Task AddAsync_WithEnabledStrategy_ShouldPersistEnabledState()
    {
        // Arrange
        var strategy = CreateTestStrategy("Enabled Strategy");
        strategy.IsEnabled = true;

        // Act
        await _repository.AddAsync(strategy);
        await _context.SaveChangesAsync();

        // Assert
        var retrieved = await _repository.GetByIdAsync(strategy.Id);
        retrieved.ShouldNotBeNull();
        retrieved!.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public async Task AddAsync_WithBreakoutRuleConfig_ShouldPersistJson()
    {
        // Arrange
        var strategy = CreateTestStrategy("Strategy with Breakout");
        strategy.BreakoutRuleConfigJson = "{\"isEnabled\":true,\"threshold\":0.10}";

        // Act
        await _repository.AddAsync(strategy);
        await _context.SaveChangesAsync();

        // Assert
        var retrieved = await _repository.GetByIdAsync(strategy.Id);
        retrieved.ShouldNotBeNull();
        retrieved!.BreakoutRuleConfigJson.ShouldBe("{\"isEnabled\":true,\"threshold\":0.10}");
    }

    [Fact]
    public async Task UpdateAsync_WithDailyData_ShouldPersistPricesAndMA20()
    {
        // Arrange
        var strategy = CreateTestStrategy("Daily Data Strategy");
        await _repository.AddAsync(strategy);
        await _context.SaveChangesAsync();

        // Act
        strategy.CurrentUnderlyingPrice = 50000m;
        strategy.CurrentEtpPrice = 45m;
        strategy.CurrentMA20 = 48000m;
        strategy.DaysBelowMA20 = 2;
        strategy.LastDailyUpdateTimestamp = DateTime.UtcNow;

        await _repository.UpdateAsync(strategy);
        await _context.SaveChangesAsync();

        // Assert
        var retrieved = await _repository.GetByIdAsync(strategy.Id);
        retrieved.ShouldNotBeNull();
        retrieved!.CurrentUnderlyingPrice.ShouldBe(50000m);
        retrieved.CurrentEtpPrice.ShouldBe(45m);
        retrieved.CurrentMA20.ShouldBe(48000m);
        retrieved.DaysBelowMA20.ShouldBe(2);
        retrieved.LastDailyUpdateTimestamp.ShouldNotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithExecutionTimestamp_ShouldPersist()
    {
        // Arrange
        var strategy = CreateTestStrategy("Execution Strategy");
        await _repository.AddAsync(strategy);
        await _context.SaveChangesAsync();

        // Act
        strategy.LastExecutionTimestamp = DateTime.UtcNow;
        await _repository.UpdateAsync(strategy);
        await _context.SaveChangesAsync();

        // Assert
        var retrieved = await _repository.GetByIdAsync(strategy.Id);
        retrieved.ShouldNotBeNull();
        retrieved!.LastExecutionTimestamp.ShouldNotBeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private static WeeklyCashManagedStrategy CreateTestStrategy(string name)
    {
        return new WeeklyCashManagedStrategy
        {
            Id = Guid.NewGuid(),
            Name = name,
            EtpSymbol = "BTCW",
            UnderlyingSymbol = "COIN",
            IsEnabled = false,
            MinCashRatio = 0.15m,
            MaxCashRatio = 0.25m,
            WeeklyBuyRatio = 0.05m,
            WeeklySellRatio = 0.10m,
            ExecutionDayOfWeek = 5,
            DaysBelowMA20 = 0,
            CreatedAt = DateTime.UtcNow,
        };
    }
}
