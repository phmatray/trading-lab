// <copyright file="WeeklyCashManagedStrategyTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Shouldly;
using TradingBot.Core.Events;
using TradingBot.Core.Models.Strategy;
using TradingBot.Core.ValueObjects;
using Xunit;

namespace TradingBot.Core.Tests.Entities;

/// <summary>
/// Unit tests for <see cref="WeeklyCashManagedStrategy"/> aggregate root domain behavior.
/// </summary>
public sealed class WeeklyCashManagedStrategyTests
{
    [Fact]
    public void Enable_WhenDisabled_ShouldEnableAndRaiseEvent()
    {
        // Arrange
        var strategy = CreateTestStrategy(isEnabled: false);

        // Act
        strategy.Enable();

        // Assert
        strategy.IsEnabled.ShouldBeTrue();
        strategy.LastModified.ShouldNotBeNull();
        strategy.DomainEvents.ShouldContain(e => e is StrategyEnabledEvent);

        var domainEvent = strategy.DomainEvents.OfType<StrategyEnabledEvent>().First();
        domainEvent.StrategyId.ShouldBe(strategy.Id);
        domainEvent.StrategyName.ShouldBe(strategy.Name);
    }

    [Fact]
    public void Enable_WhenAlreadyEnabled_ShouldThrow()
    {
        // Arrange
        var strategy = CreateTestStrategy(isEnabled: true);

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => strategy.Enable());
        ex.Message.ShouldContain("already enabled");
    }

    [Fact]
    public void Disable_WhenEnabled_ShouldDisableAndRaiseEvent()
    {
        // Arrange
        var strategy = CreateTestStrategy(isEnabled: true);

        // Act
        strategy.Disable();

        // Assert
        strategy.IsEnabled.ShouldBeFalse();
        strategy.LastModified.ShouldNotBeNull();
        strategy.DomainEvents.ShouldContain(e => e is StrategyDisabledEvent);

        var domainEvent = strategy.DomainEvents.OfType<StrategyDisabledEvent>().First();
        domainEvent.StrategyId.ShouldBe(strategy.Id);
        domainEvent.StrategyName.ShouldBe(strategy.Name);
    }

    [Fact]
    public void Disable_WhenAlreadyDisabled_ShouldThrow()
    {
        // Arrange
        var strategy = CreateTestStrategy(isEnabled: false);

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => strategy.Disable());
        ex.Message.ShouldContain("already disabled");
    }

    [Fact]
    public void UpdateConfiguration_WithValidConfig_ShouldUpdateAndRaiseEvent()
    {
        // Arrange
        var strategy = CreateTestStrategy();
        var newConfig = new StrategyConfiguration(
            minCashRatio: 0.20m,
            maxCashRatio: 0.30m,
            weeklyBuyRatio: 0.10m,
            weeklySellRatio: 0.15m,
            executionDayOfWeek: 4,
            breakoutRuleConfigJson: "{\"isEnabled\":true}");

        // Act
        strategy.UpdateConfiguration(newConfig);

        // Assert
        strategy.MinCashRatio.ShouldBe(0.20m);
        strategy.MaxCashRatio.ShouldBe(0.30m);
        strategy.WeeklyBuyRatio.ShouldBe(0.10m);
        strategy.WeeklySellRatio.ShouldBe(0.15m);
        strategy.ExecutionDayOfWeek.ShouldBe(4);
        strategy.BreakoutRuleConfigJson.ShouldBe("{\"isEnabled\":true}");
        strategy.LastModified.ShouldNotBeNull();

        strategy.DomainEvents.ShouldContain(e => e is StrategyConfigurationUpdatedEvent);
        var domainEvent = strategy.DomainEvents.OfType<StrategyConfigurationUpdatedEvent>().First();
        domainEvent.StrategyId.ShouldBe(strategy.Id);
        domainEvent.StrategyName.ShouldBe(strategy.Name);
    }

    [Fact]
    public void UpdateConfiguration_WithNullConfig_ShouldThrow()
    {
        // Arrange
        var strategy = CreateTestStrategy();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => strategy.UpdateConfiguration(null!));
    }

    [Fact]
    public void UpdateConfiguration_WithInvalidConfig_ShouldThrow()
    {
        // Arrange
        var strategy = CreateTestStrategy();
        var invalidConfig = new StrategyConfiguration(
            minCashRatio: 0.30m, // Invalid: min > max
            maxCashRatio: 0.20m,
            weeklyBuyRatio: 0.05m,
            weeklySellRatio: 0.10m,
            executionDayOfWeek: 5);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => strategy.UpdateConfiguration(invalidConfig));
    }

    [Fact]
    public void UpdateDailyData_WithValidData_ShouldUpdatePricesAndMA20()
    {
        // Arrange
        var strategy = CreateTestStrategy();
        strategy.ClearDomainEvents(); // Clear any setup events

        // Act
        strategy.UpdateDailyData(
            underlyingPrice: 50000m,
            etpPrice: 45m,
            ma20: 48000m);

        // Assert
        strategy.CurrentUnderlyingPrice.ShouldBe(50000m);
        strategy.CurrentEtpPrice.ShouldBe(45m);
        strategy.CurrentMA20.ShouldBe(48000m);
        strategy.LastDailyUpdateTimestamp.ShouldNotBeNull();
    }

    [Fact]
    public void UpdateDailyData_WhenUnderlyingAboveMA20_ShouldResetDaysBelowCounter()
    {
        // Arrange
        var strategy = CreateTestStrategy();
        strategy.DaysBelowMA20 = 5; // Start with some days below

        // Act
        strategy.UpdateDailyData(
            underlyingPrice: 50000m, // Above MA20
            etpPrice: 45m,
            ma20: 48000m);

        // Assert
        strategy.DaysBelowMA20.ShouldBe(0);
        strategy.DomainEvents.ShouldContain(e => e is MA20UpdatedEvent);

        var domainEvent = strategy.DomainEvents.OfType<MA20UpdatedEvent>().First();
        domainEvent.MA20Value.ShouldBe(48000m);
        domainEvent.CurrentPrice.ShouldBe(50000m);
        domainEvent.DaysBelowMA20.ShouldBe(0);
    }

    [Fact]
    public void UpdateDailyData_WhenUnderlyingBelowMA20_ShouldIncrementDaysBelowCounter()
    {
        // Arrange
        var strategy = CreateTestStrategy();
        strategy.DaysBelowMA20 = 1; // Start with 1 day below
        strategy.ClearDomainEvents();

        // Act
        strategy.UpdateDailyData(
            underlyingPrice: 47000m, // Below MA20
            etpPrice: 45m,
            ma20: 48000m);

        // Assert
        strategy.DaysBelowMA20.ShouldBe(2);
        strategy.DomainEvents.ShouldContain(e => e is MA20UpdatedEvent);

        var domainEvent = strategy.DomainEvents.OfType<MA20UpdatedEvent>().First();
        domainEvent.DaysBelowMA20.ShouldBe(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void UpdateDailyData_WithInvalidUnderlyingPrice_ShouldThrow(decimal price)
    {
        // Arrange
        var strategy = CreateTestStrategy();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            strategy.UpdateDailyData(price, 45m, 48000m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void UpdateDailyData_WithInvalidEtpPrice_ShouldThrow(decimal price)
    {
        // Arrange
        var strategy = CreateTestStrategy();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            strategy.UpdateDailyData(50000m, price, 48000m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5000)]
    public void UpdateDailyData_WithInvalidMA20_ShouldThrow(decimal ma20)
    {
        // Arrange
        var strategy = CreateTestStrategy();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            strategy.UpdateDailyData(50000m, 45m, ma20));
    }

    [Fact]
    public void RecordExecution_WithBuyAndSellOrders_ShouldUpdateTimestampAndRaiseEvent()
    {
        // Arrange
        var strategy = CreateTestStrategy();
        strategy.ClearDomainEvents();
        var buyOrderId = Guid.NewGuid();
        var sellOrderId = Guid.NewGuid();

        // Act
        strategy.RecordExecution(buyOrderId, sellOrderId, cashRatioAfter: 0.18m);

        // Assert
        strategy.LastExecutionTimestamp.ShouldNotBeNull();
        strategy.LastModified.ShouldNotBeNull();
        strategy.DomainEvents.ShouldContain(e => e is StrategyExecutedEvent);

        var domainEvent = strategy.DomainEvents.OfType<StrategyExecutedEvent>().First();
        domainEvent.BuyOrderId.ShouldBe(buyOrderId);
        domainEvent.SellOrderId.ShouldBe(sellOrderId);
        domainEvent.CashRatioAfter.ShouldBe(0.18m);
        domainEvent.DaysBelowMA20.ShouldBe(strategy.DaysBelowMA20);
    }

    [Fact]
    public void RecordExecution_WithOnlyBuyOrder_ShouldRaiseEvent()
    {
        // Arrange
        var strategy = CreateTestStrategy();
        strategy.ClearDomainEvents();
        var buyOrderId = Guid.NewGuid();

        // Act
        strategy.RecordExecution(buyOrderId, sellOrderId: null, cashRatioAfter: 0.20m);

        // Assert
        var domainEvent = strategy.DomainEvents.OfType<StrategyExecutedEvent>().First();
        domainEvent.BuyOrderId.ShouldBe(buyOrderId);
        domainEvent.SellOrderId.ShouldBeNull();
    }

    [Fact]
    public void Validate_WithValidStrategy_ShouldNotThrow()
    {
        // Arrange
        var strategy = CreateTestStrategy();

        // Act & Assert
        Should.NotThrow(() => strategy.Validate());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithInvalidName_ShouldThrow(string? name)
    {
        // Arrange
        var strategy = CreateTestStrategy();
        strategy.Name = name!;

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => strategy.Validate());
        ex.Message.ShouldContain("Strategy name is required");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithInvalidEtpSymbol_ShouldThrow(string? symbol)
    {
        // Arrange
        var strategy = CreateTestStrategy();
        strategy.EtpSymbol = symbol!;

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => strategy.Validate());
        ex.Message.ShouldContain("ETP symbol is required");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithInvalidUnderlyingSymbol_ShouldThrow(string? symbol)
    {
        // Arrange
        var strategy = CreateTestStrategy();
        strategy.UnderlyingSymbol = symbol!;

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => strategy.Validate());
        ex.Message.ShouldContain("Underlying symbol is required");
    }

    private static WeeklyCashManagedStrategy CreateTestStrategy(bool isEnabled = false)
    {
        return new WeeklyCashManagedStrategy
        {
            Id = Guid.NewGuid(),
            Name = "Test Strategy",
            EtpSymbol = "BTCW",
            UnderlyingSymbol = "COIN",
            IsEnabled = isEnabled,
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
