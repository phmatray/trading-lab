# Weekly Cash-Managed Trading Strategy - Data Model

**Date**: 2025-01-16
**Phase**: 1 (Design & Contracts)
**Status**: Completed

This document defines the complete data model for the Weekly Cash-Managed Trading Strategy feature, including all entities, value objects, domain events, state transitions, validation rules, and EF Core configurations following the project's DDD patterns.

---

## 1. Entity Definitions

### 1.1 WeeklyCashManagedStrategy (Aggregate Root)

**Purpose**: Represents the configuration and runtime state of a weekly cash-managed trading strategy instance.

**Inheritance**: `EntityBase<Guid>, IAggregateRoot`

**Namespace**: `TradingBot.Core.Models.Strategy`

```csharp
// <copyright file="WeeklyCashManagedStrategy.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Events;
using TradingBot.Core.SharedKernel;
using TradingBot.Core.ValueObjects;

namespace TradingBot.Core.Models.Strategy;

/// <summary>
/// Represents a weekly cash-managed trading strategy aggregate root.
/// Automatically buys ETP when underlying asset is above MA20 and sells when below MA20 for 2+ days.
/// </summary>
public sealed class WeeklyCashManagedStrategy : EntityBase<Guid>, IAggregateRoot
{
    /// <summary>
    /// Gets or sets the strategy name (user-defined, unique identifier).
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the ETP symbol to trade (e.g., "BTCW", "ETHW").
    /// </summary>
    public required string EtpSymbol { get; set; }

    /// <summary>
    /// Gets or sets the underlying asset symbol to track (e.g., "COIN" for Bitcoin).
    /// </summary>
    public required string UnderlyingSymbol { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the strategy is currently enabled.
    /// </summary>
    public required bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the minimum cash ratio (decimal 0-1, e.g., 0.15 = 15%).
    /// Cash ratio below this triggers sell to rebuild buffer.
    /// </summary>
    public required decimal MinCashRatio { get; set; }

    /// <summary>
    /// Gets or sets the maximum cash ratio (decimal 0-1, e.g., 0.25 = 25%).
    /// Cash ratio above this triggers buy to deploy excess cash.
    /// </summary>
    public required decimal MaxCashRatio { get; set; }

    /// <summary>
    /// Gets or sets the weekly buy ratio (decimal 0-1, e.g., 0.05 = 5% of equity).
    /// Percentage of total equity to invest in ETP each week when conditions met.
    /// </summary>
    public required decimal WeeklyBuyRatio { get; set; }

    /// <summary>
    /// Gets or sets the weekly sell ratio (decimal 0-1, e.g., 0.10 = 10% of position).
    /// Percentage of ETP position to sell each week when conditions met.
    /// </summary>
    public required decimal WeeklySellRatio { get; set; }

    /// <summary>
    /// Gets or sets the execution day of week (0 = Sunday, 5 = Friday).
    /// Default: 5 (Friday after market close).
    /// </summary>
    public required int ExecutionDayOfWeek { get; set; }

    /// <summary>
    /// Gets or sets the consecutive days counter for how long underlying has been below MA20.
    /// Resets to 0 when underlying crosses back above MA20.
    /// Triggers sell when >= 2.
    /// </summary>
    public int DaysBelowMA20 { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last weekly routine execution (UTC).
    /// </summary>
    public DateTime? LastExecutionTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last daily routine execution (UTC).
    /// </summary>
    public DateTime? LastDailyUpdateTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the current MA20 value for the underlying asset.
    /// Updated daily by the daily routine.
    /// </summary>
    public decimal? CurrentMA20 { get; set; }

    /// <summary>
    /// Gets or sets the current price of the underlying asset.
    /// Updated daily by the daily routine.
    /// </summary>
    public decimal? CurrentUnderlyingPrice { get; set; }

    /// <summary>
    /// Gets or sets the current price of the ETP.
    /// Updated daily by the daily routine.
    /// </summary>
    public decimal? CurrentEtpPrice { get; set; }

    /// <summary>
    /// Gets or sets the optional breakout rule configuration (JSON serialized).
    /// Null if breakout rule is disabled.
    /// </summary>
    public string? BreakoutRuleConfigJson { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the strategy was created (UTC).
    /// </summary>
    public required DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the strategy was last modified (UTC).
    /// </summary>
    public DateTime? LastModified { get; set; }

    // ===== Domain Behavior Methods =====

    /// <summary>
    /// Enables the strategy and raises a domain event.
    /// </summary>
    public void Enable()
    {
        if (IsEnabled)
        {
            throw new InvalidOperationException("Strategy is already enabled");
        }

        IsEnabled = true;
        LastModified = DateTime.UtcNow;

        RegisterDomainEvent(new StrategyEnabledEvent(Id, Name));
    }

    /// <summary>
    /// Disables the strategy and raises a domain event.
    /// </summary>
    public void Disable()
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException("Strategy is already disabled");
        }

        IsEnabled = false;
        LastModified = DateTime.UtcNow;

        RegisterDomainEvent(new StrategyDisabledEvent(Id, Name));
    }

    /// <summary>
    /// Updates the daily market data (prices, MA20, days below MA20 counter).
    /// </summary>
    /// <param name="underlyingPrice">Current underlying asset price.</param>
    /// <param name="etpPrice">Current ETP price.</param>
    /// <param name="ma20">Current 20-day moving average of underlying.</param>
    public void UpdateDailyData(decimal underlyingPrice, decimal etpPrice, decimal ma20)
    {
        if (underlyingPrice <= 0)
        {
            throw new ArgumentException("Underlying price must be positive", nameof(underlyingPrice));
        }

        if (etpPrice <= 0)
        {
            throw new ArgumentException("ETP price must be positive", nameof(etpPrice));
        }

        if (ma20 <= 0)
        {
            throw new ArgumentException("MA20 must be positive", nameof(ma20));
        }

        var previousMA20 = CurrentMA20;
        var previousDaysBelowMA20 = DaysBelowMA20;

        CurrentUnderlyingPrice = underlyingPrice;
        CurrentEtpPrice = etpPrice;
        CurrentMA20 = ma20;
        LastDailyUpdateTimestamp = DateTime.UtcNow;

        // Update days below MA20 counter
        if (underlyingPrice < ma20)
        {
            DaysBelowMA20++;
        }
        else
        {
            DaysBelowMA20 = 0; // Reset counter when crossing above MA20
        }

        // Raise event if MA20 changed significantly or days counter changed
        if (previousMA20 != ma20 || previousDaysBelowMA20 != DaysBelowMA20)
        {
            RegisterDomainEvent(new MA20UpdatedEvent(
                Id,
                UnderlyingSymbol,
                ma20,
                underlyingPrice,
                DaysBelowMA20));
        }
    }

    /// <summary>
    /// Records a weekly execution and raises a domain event.
    /// </summary>
    /// <param name="buyOrderId">Optional buy order ID if buy was executed.</param>
    /// <param name="sellOrderId">Optional sell order ID if sell was executed.</param>
    /// <param name="cashRatioAfter">Cash ratio after execution.</param>
    public void RecordExecution(Guid? buyOrderId, Guid? sellOrderId, decimal cashRatioAfter)
    {
        LastExecutionTimestamp = DateTime.UtcNow;
        LastModified = DateTime.UtcNow;

        RegisterDomainEvent(new StrategyExecutedEvent(
            Id,
            Name,
            buyOrderId,
            sellOrderId,
            cashRatioAfter,
            DaysBelowMA20));
    }

    /// <summary>
    /// Updates the configuration parameters and raises a domain event.
    /// </summary>
    /// <param name="config">New configuration values.</param>
    public void UpdateConfiguration(StrategyConfiguration config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        // Validate configuration (validation logic in value object)
        config.Validate();

        MinCashRatio = config.MinCashRatio;
        MaxCashRatio = config.MaxCashRatio;
        WeeklyBuyRatio = config.WeeklyBuyRatio;
        WeeklySellRatio = config.WeeklySellRatio;
        ExecutionDayOfWeek = config.ExecutionDayOfWeek;
        BreakoutRuleConfigJson = config.BreakoutRuleConfigJson;
        LastModified = DateTime.UtcNow;

        RegisterDomainEvent(new StrategyConfigurationUpdatedEvent(Id, Name));
    }

    /// <summary>
    /// Validates the entity state.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException("Strategy name is required");
        }

        if (string.IsNullOrWhiteSpace(EtpSymbol))
        {
            throw new InvalidOperationException("ETP symbol is required");
        }

        if (string.IsNullOrWhiteSpace(UnderlyingSymbol))
        {
            throw new InvalidOperationException("Underlying symbol is required");
        }

        if (MinCashRatio < 0 || MinCashRatio > 1)
        {
            throw new InvalidOperationException("MinCashRatio must be between 0 and 1");
        }

        if (MaxCashRatio < 0 || MaxCashRatio > 1)
        {
            throw new InvalidOperationException("MaxCashRatio must be between 0 and 1");
        }

        if (MinCashRatio >= MaxCashRatio)
        {
            throw new InvalidOperationException("MinCashRatio must be less than MaxCashRatio");
        }

        if (WeeklyBuyRatio < 0 || WeeklyBuyRatio > 1)
        {
            throw new InvalidOperationException("WeeklyBuyRatio must be between 0 and 1");
        }

        if (WeeklySellRatio < 0 || WeeklySellRatio > 1)
        {
            throw new InvalidOperationException("WeeklySellRatio must be between 0 and 1");
        }

        if (ExecutionDayOfWeek < 0 || ExecutionDayOfWeek > 6)
        {
            throw new InvalidOperationException("ExecutionDayOfWeek must be between 0 (Sunday) and 6 (Saturday)");
        }
    }
}
```

---

## 2. Value Objects

### 2.1 StrategyConfiguration

**Purpose**: Encapsulates all configurable parameters for the strategy in an immutable value object.

**Namespace**: `TradingBot.Core.ValueObjects`

```csharp
// <copyright file="StrategyConfiguration.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.SharedKernel;

namespace TradingBot.Core.ValueObjects;

/// <summary>
/// Immutable value object representing the configuration parameters for a weekly cash-managed strategy.
/// </summary>
public sealed class StrategyConfiguration : ValueObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StrategyConfiguration"/> class.
    /// </summary>
    /// <param name="minCashRatio">Minimum cash ratio (0-1).</param>
    /// <param name="maxCashRatio">Maximum cash ratio (0-1).</param>
    /// <param name="weeklyBuyRatio">Weekly buy ratio (0-1).</param>
    /// <param name="weeklySellRatio">Weekly sell ratio (0-1).</param>
    /// <param name="executionDayOfWeek">Execution day of week (0-6).</param>
    /// <param name="breakoutRuleConfigJson">Optional breakout rule JSON config.</param>
    public StrategyConfiguration(
        decimal minCashRatio,
        decimal maxCashRatio,
        decimal weeklyBuyRatio,
        decimal weeklySellRatio,
        int executionDayOfWeek,
        string? breakoutRuleConfigJson = null)
    {
        MinCashRatio = minCashRatio;
        MaxCashRatio = maxCashRatio;
        WeeklyBuyRatio = weeklyBuyRatio;
        WeeklySellRatio = weeklySellRatio;
        ExecutionDayOfWeek = executionDayOfWeek;
        BreakoutRuleConfigJson = breakoutRuleConfigJson;
    }

    /// <summary>
    /// Gets the minimum cash ratio (0-1).
    /// </summary>
    public decimal MinCashRatio { get; }

    /// <summary>
    /// Gets the maximum cash ratio (0-1).
    /// </summary>
    public decimal MaxCashRatio { get; }

    /// <summary>
    /// Gets the weekly buy ratio (0-1).
    /// </summary>
    public decimal WeeklyBuyRatio { get; }

    /// <summary>
    /// Gets the weekly sell ratio (0-1).
    /// </summary>
    public decimal WeeklySellRatio { get; }

    /// <summary>
    /// Gets the execution day of week (0=Sunday, 5=Friday).
    /// </summary>
    public int ExecutionDayOfWeek { get; }

    /// <summary>
    /// Gets the optional breakout rule configuration JSON.
    /// </summary>
    public string? BreakoutRuleConfigJson { get; }

    /// <summary>
    /// Validates the configuration and throws if invalid.
    /// </summary>
    public void Validate()
    {
        if (MinCashRatio < 0 || MinCashRatio > 1)
        {
            throw new ArgumentException("MinCashRatio must be between 0 and 1", nameof(MinCashRatio));
        }

        if (MaxCashRatio < 0 || MaxCashRatio > 1)
        {
            throw new ArgumentException("MaxCashRatio must be between 0 and 1", nameof(MaxCashRatio));
        }

        if (MinCashRatio >= MaxCashRatio)
        {
            throw new ArgumentException("MinCashRatio must be less than MaxCashRatio");
        }

        if (WeeklyBuyRatio < 0 || WeeklyBuyRatio > 1)
        {
            throw new ArgumentException("WeeklyBuyRatio must be between 0 and 1", nameof(WeeklyBuyRatio));
        }

        if (WeeklySellRatio < 0 || WeeklySellRatio > 1)
        {
            throw new ArgumentException("WeeklySellRatio must be between 0 and 1", nameof(WeeklySellRatio));
        }

        if (ExecutionDayOfWeek < 0 || ExecutionDayOfWeek > 6)
        {
            throw new ArgumentException("ExecutionDayOfWeek must be between 0 (Sunday) and 6 (Saturday)", nameof(ExecutionDayOfWeek));
        }
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MinCashRatio;
        yield return MaxCashRatio;
        yield return WeeklyBuyRatio;
        yield return WeeklySellRatio;
        yield return ExecutionDayOfWeek;
        yield return BreakoutRuleConfigJson;
    }
}
```

### 2.2 BreakoutRuleConfig

**Purpose**: Immutable configuration for optional breakout rule that accelerates buying during strong momentum.

**Namespace**: `TradingBot.Core.ValueObjects`

```csharp
// <copyright file="BreakoutRuleConfig.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.SharedKernel;

namespace TradingBot.Core.ValueObjects;

/// <summary>
/// Immutable value object representing breakout rule configuration.
/// When enabled, doubles buy amount if weekly price increase and volume conditions are met.
/// </summary>
public sealed class BreakoutRuleConfig : ValueObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BreakoutRuleConfig"/> class.
    /// </summary>
    /// <param name="isEnabled">Whether the breakout rule is enabled.</param>
    /// <param name="weeklyPriceIncreaseThreshold">Minimum weekly price increase percentage (e.g., 0.10 = 10%).</param>
    /// <param name="volumeMultiplier">Minimum volume as multiple of 20-day average (e.g., 1.5 = 150% of avg).</param>
    /// <param name="buyRatioMultiplier">Multiplier for buy ratio when breakout detected (e.g., 2.0 = double).</param>
    public BreakoutRuleConfig(
        bool isEnabled,
        decimal weeklyPriceIncreaseThreshold = 0.10m,
        decimal volumeMultiplier = 1.5m,
        decimal buyRatioMultiplier = 2.0m)
    {
        IsEnabled = isEnabled;
        WeeklyPriceIncreaseThreshold = weeklyPriceIncreaseThreshold;
        VolumeMultiplier = volumeMultiplier;
        BuyRatioMultiplier = buyRatioMultiplier;
    }

    /// <summary>
    /// Gets a value indicating whether the breakout rule is enabled.
    /// </summary>
    public bool IsEnabled { get; }

    /// <summary>
    /// Gets the weekly price increase threshold (0-1, e.g., 0.10 = 10%).
    /// </summary>
    public decimal WeeklyPriceIncreaseThreshold { get; }

    /// <summary>
    /// Gets the volume multiplier (e.g., 1.5 = 150% of 20-day average volume).
    /// </summary>
    public decimal VolumeMultiplier { get; }

    /// <summary>
    /// Gets the buy ratio multiplier (e.g., 2.0 = double the buy amount).
    /// </summary>
    public decimal BuyRatioMultiplier { get; }

    /// <summary>
    /// Validates the configuration and throws if invalid.
    /// </summary>
    public void Validate()
    {
        if (WeeklyPriceIncreaseThreshold < 0 || WeeklyPriceIncreaseThreshold > 1)
        {
            throw new ArgumentException("WeeklyPriceIncreaseThreshold must be between 0 and 1", nameof(WeeklyPriceIncreaseThreshold));
        }

        if (VolumeMultiplier <= 0)
        {
            throw new ArgumentException("VolumeMultiplier must be positive", nameof(VolumeMultiplier));
        }

        if (BuyRatioMultiplier <= 1)
        {
            throw new ArgumentException("BuyRatioMultiplier must be greater than 1", nameof(BuyRatioMultiplier));
        }
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return IsEnabled;
        yield return WeeklyPriceIncreaseThreshold;
        yield return VolumeMultiplier;
        yield return BuyRatioMultiplier;
    }
}
```

---

## 3. Domain Events

All domain events extend `DomainEventBase` (which includes `DateOccurred` timestamp) and are dispatched via MediatR before `SaveChangesAsync` completes.

**Namespace**: `TradingBot.Core.Events`

### 3.1 StrategyEnabledEvent

```csharp
// <copyright file="StrategyEnabledEvent.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.SharedKernel;

namespace TradingBot.Core.Events;

/// <summary>
/// Domain event raised when a weekly cash-managed strategy is enabled.
/// </summary>
public sealed class StrategyEnabledEvent : DomainEventBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StrategyEnabledEvent"/> class.
    /// </summary>
    /// <param name="strategyId">The strategy identifier.</param>
    /// <param name="strategyName">The strategy name.</param>
    public StrategyEnabledEvent(Guid strategyId, string strategyName)
    {
        StrategyId = strategyId;
        StrategyName = strategyName;
    }

    /// <summary>
    /// Gets the strategy identifier.
    /// </summary>
    public Guid StrategyId { get; }

    /// <summary>
    /// Gets the strategy name.
    /// </summary>
    public string StrategyName { get; }
}
```

### 3.2 StrategyDisabledEvent

```csharp
// <copyright file="StrategyDisabledEvent.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.SharedKernel;

namespace TradingBot.Core.Events;

/// <summary>
/// Domain event raised when a weekly cash-managed strategy is disabled.
/// </summary>
public sealed class StrategyDisabledEvent : DomainEventBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StrategyDisabledEvent"/> class.
    /// </summary>
    /// <param name="strategyId">The strategy identifier.</param>
    /// <param name="strategyName">The strategy name.</param>
    public StrategyDisabledEvent(Guid strategyId, string strategyName)
    {
        StrategyId = strategyId;
        StrategyName = strategyName;
    }

    /// <summary>
    /// Gets the strategy identifier.
    /// </summary>
    public Guid StrategyId { get; }

    /// <summary>
    /// Gets the strategy name.
    /// </summary>
    public string StrategyName { get; }
}
```

### 3.3 StrategyExecutedEvent

```csharp
// <copyright file="StrategyExecutedEvent.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.SharedKernel;

namespace TradingBot.Core.Events;

/// <summary>
/// Domain event raised when the weekly routine executes and completes.
/// </summary>
public sealed class StrategyExecutedEvent : DomainEventBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StrategyExecutedEvent"/> class.
    /// </summary>
    /// <param name="strategyId">The strategy identifier.</param>
    /// <param name="strategyName">The strategy name.</param>
    /// <param name="buyOrderId">The buy order ID if buy was executed.</param>
    /// <param name="sellOrderId">The sell order ID if sell was executed.</param>
    /// <param name="cashRatioAfter">Cash ratio after execution.</param>
    /// <param name="daysBelowMA20">Current days below MA20 counter.</param>
    public StrategyExecutedEvent(
        Guid strategyId,
        string strategyName,
        Guid? buyOrderId,
        Guid? sellOrderId,
        decimal cashRatioAfter,
        int daysBelowMA20)
    {
        StrategyId = strategyId;
        StrategyName = strategyName;
        BuyOrderId = buyOrderId;
        SellOrderId = sellOrderId;
        CashRatioAfter = cashRatioAfter;
        DaysBelowMA20 = daysBelowMA20;
    }

    /// <summary>
    /// Gets the strategy identifier.
    /// </summary>
    public Guid StrategyId { get; }

    /// <summary>
    /// Gets the strategy name.
    /// </summary>
    public string StrategyName { get; }

    /// <summary>
    /// Gets the buy order ID (null if no buy executed).
    /// </summary>
    public Guid? BuyOrderId { get; }

    /// <summary>
    /// Gets the sell order ID (null if no sell executed).
    /// </summary>
    public Guid? SellOrderId { get; }

    /// <summary>
    /// Gets the cash ratio after execution.
    /// </summary>
    public decimal CashRatioAfter { get; }

    /// <summary>
    /// Gets the current days below MA20 counter.
    /// </summary>
    public int DaysBelowMA20 { get; }
}
```

### 3.4 MA20UpdatedEvent

```csharp
// <copyright file="MA20UpdatedEvent.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.SharedKernel;

namespace TradingBot.Core.Events;

/// <summary>
/// Domain event raised when the MA20 indicator is updated during the daily routine.
/// </summary>
public sealed class MA20UpdatedEvent : DomainEventBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MA20UpdatedEvent"/> class.
    /// </summary>
    /// <param name="strategyId">The strategy identifier.</param>
    /// <param name="symbol">The underlying asset symbol.</param>
    /// <param name="ma20Value">The new MA20 value.</param>
    /// <param name="currentPrice">The current underlying price.</param>
    /// <param name="daysBelowMA20">The updated days below MA20 counter.</param>
    public MA20UpdatedEvent(
        Guid strategyId,
        string symbol,
        decimal ma20Value,
        decimal currentPrice,
        int daysBelowMA20)
    {
        StrategyId = strategyId;
        Symbol = symbol;
        MA20Value = ma20Value;
        CurrentPrice = currentPrice;
        DaysBelowMA20 = daysBelowMA20;
    }

    /// <summary>
    /// Gets the strategy identifier.
    /// </summary>
    public Guid StrategyId { get; }

    /// <summary>
    /// Gets the underlying asset symbol.
    /// </summary>
    public string Symbol { get; }

    /// <summary>
    /// Gets the new MA20 value.
    /// </summary>
    public decimal MA20Value { get; }

    /// <summary>
    /// Gets the current underlying price.
    /// </summary>
    public decimal CurrentPrice { get; }

    /// <summary>
    /// Gets the updated days below MA20 counter.
    /// </summary>
    public int DaysBelowMA20 { get; }
}
```

### 3.5 StrategyConfigurationUpdatedEvent

```csharp
// <copyright file="StrategyConfigurationUpdatedEvent.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.SharedKernel;

namespace TradingBot.Core.Events;

/// <summary>
/// Domain event raised when strategy configuration parameters are updated.
/// </summary>
public sealed class StrategyConfigurationUpdatedEvent : DomainEventBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StrategyConfigurationUpdatedEvent"/> class.
    /// </summary>
    /// <param name="strategyId">The strategy identifier.</param>
    /// <param name="strategyName">The strategy name.</param>
    public StrategyConfigurationUpdatedEvent(Guid strategyId, string strategyName)
    {
        StrategyId = strategyId;
        StrategyName = strategyName;
    }

    /// <summary>
    /// Gets the strategy identifier.
    /// </summary>
    public Guid StrategyId { get; }

    /// <summary>
    /// Gets the strategy name.
    /// </summary>
    public string StrategyName { get; }
}
```

### 3.6 CashBufferAdjustedEvent

```csharp
// <copyright file="CashBufferAdjustedEvent.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.SharedKernel;

namespace TradingBot.Core.Events;

/// <summary>
/// Domain event raised when cash buffer adjustment executes (buy or sell to maintain ratio).
/// </summary>
public sealed class CashBufferAdjustedEvent : DomainEventBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CashBufferAdjustedEvent"/> class.
    /// </summary>
    /// <param name="strategyId">The strategy identifier.</param>
    /// <param name="orderId">The adjustment order ID (buy or sell).</param>
    /// <param name="adjustmentType">The adjustment type (Buy or Sell).</param>
    /// <param name="cashRatioBefore">Cash ratio before adjustment.</param>
    /// <param name="cashRatioAfter">Cash ratio after adjustment.</param>
    public CashBufferAdjustedEvent(
        Guid strategyId,
        Guid orderId,
        string adjustmentType,
        decimal cashRatioBefore,
        decimal cashRatioAfter)
    {
        StrategyId = strategyId;
        OrderId = orderId;
        AdjustmentType = adjustmentType;
        CashRatioBefore = cashRatioBefore;
        CashRatioAfter = cashRatioAfter;
    }

    /// <summary>
    /// Gets the strategy identifier.
    /// </summary>
    public Guid StrategyId { get; }

    /// <summary>
    /// Gets the adjustment order ID.
    /// </summary>
    public Guid OrderId { get; }

    /// <summary>
    /// Gets the adjustment type (Buy or Sell).
    /// </summary>
    public string AdjustmentType { get; }

    /// <summary>
    /// Gets the cash ratio before adjustment.
    /// </summary>
    public decimal CashRatioBefore { get; }

    /// <summary>
    /// Gets the cash ratio after adjustment.
    /// </summary>
    public decimal CashRatioAfter { get; }
}
```

---

## 4. State Transitions

### 4.1 Strategy Lifecycle State Machine

```
[Created] ---> [Enabled] ---> [Executing] ---> [Disabled]
   |              |               |                |
   |              |<--------------|                |
   |              |                                |
   |<---------------------------------------------|
```

**State Descriptions**:

| State | Description | Valid Transitions |
|-------|-------------|-------------------|
| **Created** | Strategy configured but not yet enabled | → Enabled |
| **Enabled** | Strategy active, waiting for next execution | → Executing, → Disabled |
| **Executing** | Weekly routine running (transient state) | → Enabled |
| **Disabled** | Strategy paused, no execution | → Enabled, → Created (if reconfigured) |

**State Transition Rules**:
- `Created → Enabled`: User enables strategy via UI toggle, raises `StrategyEnabledEvent`
- `Enabled → Executing`: Weekly routine starts on configured day of week
- `Executing → Enabled`: Weekly routine completes, raises `StrategyExecutedEvent`
- `Enabled → Disabled`: User disables strategy via UI toggle, raises `StrategyDisabledEvent`
- `Disabled → Enabled`: User re-enables strategy, raises `StrategyEnabledEvent`

### 4.2 DaysBelowMA20 Counter Transitions

```
[0] --Underlying < MA20--> [1] --Underlying < MA20--> [2] --Underlying < MA20--> [3] ...
 ^                          ^                          |
 |                          |                          |
 +--Underlying >= MA20------+--Underlying >= MA20-----+
```

**Counter Logic**:
- **Increment**: Daily routine detects `UnderlyingPrice < MA20` → `DaysBelowMA20++`
- **Reset to 0**: Daily routine detects `UnderlyingPrice >= MA20` → `DaysBelowMA20 = 0`
- **Sell Trigger**: Weekly routine executes sell when `DaysBelowMA20 >= 2`

---

## 5. Validation Rules

### 5.1 Entity-Level Validation

Implemented in `WeeklyCashManagedStrategy.Validate()`:

| Field | Rule | Error Message |
|-------|------|---------------|
| `Name` | Required, not null/empty | "Strategy name is required" |
| `EtpSymbol` | Required, not null/empty | "ETP symbol is required" |
| `UnderlyingSymbol` | Required, not null/empty | "Underlying symbol is required" |
| `MinCashRatio` | Range [0, 1] | "MinCashRatio must be between 0 and 1" |
| `MaxCashRatio` | Range [0, 1] | "MaxCashRatio must be between 0 and 1" |
| `MinCashRatio < MaxCashRatio` | Must be strictly less than | "MinCashRatio must be less than MaxCashRatio" |
| `WeeklyBuyRatio` | Range [0, 1] | "WeeklyBuyRatio must be between 0 and 1" |
| `WeeklySellRatio` | Range [0, 1] | "WeeklySellRatio must be between 0 and 1" |
| `ExecutionDayOfWeek` | Range [0, 6] | "ExecutionDayOfWeek must be between 0 (Sunday) and 6 (Saturday)" |

### 5.2 Configuration Value Object Validation

Implemented in `StrategyConfiguration.Validate()` and `BreakoutRuleConfig.Validate()`:

**StrategyConfiguration**:
- Same rules as entity-level validation
- Validates before being applied to entity via `UpdateConfiguration()`

**BreakoutRuleConfig**:
- `WeeklyPriceIncreaseThreshold`: Range [0, 1]
- `VolumeMultiplier`: Must be > 0
- `BuyRatioMultiplier`: Must be > 1

### 5.3 Business Rule Validation

**Buy Execution Conditions** (checked by weekly routine service):
```csharp
// All conditions must be true to execute buy
bool canBuy =
    strategy.IsEnabled &&
    currentDayOfWeek == strategy.ExecutionDayOfWeek &&
    currentUnderlyingPrice > currentMA20 &&
    currentCashRatio > strategy.MinCashRatio &&
    availableCash > 0;
```

**Sell Execution Conditions**:
```csharp
// All conditions must be true to execute sell
bool canSell =
    strategy.IsEnabled &&
    currentDayOfWeek == strategy.ExecutionDayOfWeek &&
    strategy.DaysBelowMA20 >= 2 &&
    etpSharesHeld > 0;
```

---

## 6. EF Core Configuration

### 6.1 WeeklyCashManagedStrategyConfiguration

**File**: `/src/TradingBot.Infrastructure/Persistence/Configurations/WeeklyCashManagedStrategyConfiguration.cs`

```csharp
// <copyright file="WeeklyCashManagedStrategyConfiguration.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingBot.Core.Models.Strategy;

namespace TradingBot.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for WeeklyCashManagedStrategy.
/// </summary>
internal sealed class WeeklyCashManagedStrategyConfiguration : IEntityTypeConfiguration<WeeklyCashManagedStrategy>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<WeeklyCashManagedStrategy> builder)
    {
        builder.ToTable("weekly_cash_managed_strategies");

        builder.HasKey(s => s.Id);

        // Ignore domain events collection (not persisted)
        builder.Ignore(s => s.DomainEvents);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(s => s.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.EtpSymbol)
            .HasColumnName("etp_symbol")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.UnderlyingSymbol)
            .HasColumnName("underlying_symbol")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.IsEnabled)
            .HasColumnName("is_enabled")
            .IsRequired();

        builder.Property(s => s.MinCashRatio)
            .HasColumnName("min_cash_ratio")
            .HasPrecision(5, 4) // e.g., 0.1500 (15%)
            .IsRequired();

        builder.Property(s => s.MaxCashRatio)
            .HasColumnName("max_cash_ratio")
            .HasPrecision(5, 4) // e.g., 0.2500 (25%)
            .IsRequired();

        builder.Property(s => s.WeeklyBuyRatio)
            .HasColumnName("weekly_buy_ratio")
            .HasPrecision(5, 4) // e.g., 0.0500 (5%)
            .IsRequired();

        builder.Property(s => s.WeeklySellRatio)
            .HasColumnName("weekly_sell_ratio")
            .HasPrecision(5, 4) // e.g., 0.1000 (10%)
            .IsRequired();

        builder.Property(s => s.ExecutionDayOfWeek)
            .HasColumnName("execution_day_of_week")
            .IsRequired();

        builder.Property(s => s.DaysBelowMA20)
            .HasColumnName("days_below_ma20")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(s => s.LastExecutionTimestamp)
            .HasColumnName("last_execution_timestamp");

        builder.Property(s => s.LastDailyUpdateTimestamp)
            .HasColumnName("last_daily_update_timestamp");

        builder.Property(s => s.CurrentMA20)
            .HasColumnName("current_ma20")
            .HasPrecision(18, 2);

        builder.Property(s => s.CurrentUnderlyingPrice)
            .HasColumnName("current_underlying_price")
            .HasPrecision(18, 2);

        builder.Property(s => s.CurrentEtpPrice)
            .HasColumnName("current_etp_price")
            .HasPrecision(18, 2);

        builder.Property(s => s.BreakoutRuleConfigJson)
            .HasColumnName("breakout_rule_config_json")
            .HasColumnType("TEXT"); // SQLite JSON storage

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(s => s.LastModified)
            .HasColumnName("last_modified");

        // Indexes
        builder.HasIndex(s => s.Name)
            .IsUnique()
            .HasDatabaseName("idx_wcm_strategy_name");

        builder.HasIndex(s => s.EtpSymbol)
            .HasDatabaseName("idx_wcm_strategy_etp_symbol");

        builder.HasIndex(s => s.UnderlyingSymbol)
            .HasDatabaseName("idx_wcm_strategy_underlying_symbol");

        builder.HasIndex(s => s.IsEnabled)
            .HasDatabaseName("idx_wcm_strategy_is_enabled");

        builder.HasIndex(s => s.LastExecutionTimestamp)
            .HasDatabaseName("idx_wcm_strategy_last_execution");
    }
}
```

### 6.2 Database Schema

**Table**: `weekly_cash_managed_strategies`

| Column Name | Type | Nullable | Default | Constraints |
|-------------|------|----------|---------|-------------|
| `id` | GUID (TEXT) | NO | - | PRIMARY KEY |
| `name` | VARCHAR(100) | NO | - | UNIQUE |
| `etp_symbol` | VARCHAR(20) | NO | - | - |
| `underlying_symbol` | VARCHAR(20) | NO | - | - |
| `is_enabled` | BOOLEAN | NO | - | - |
| `min_cash_ratio` | DECIMAL(5,4) | NO | - | CHECK >= 0 AND <= 1 |
| `max_cash_ratio` | DECIMAL(5,4) | NO | - | CHECK >= 0 AND <= 1 |
| `weekly_buy_ratio` | DECIMAL(5,4) | NO | - | CHECK >= 0 AND <= 1 |
| `weekly_sell_ratio` | DECIMAL(5,4) | NO | - | CHECK >= 0 AND <= 1 |
| `execution_day_of_week` | INTEGER | NO | - | CHECK >= 0 AND <= 6 |
| `days_below_ma20` | INTEGER | NO | 0 | - |
| `last_execution_timestamp` | DATETIME | YES | - | - |
| `last_daily_update_timestamp` | DATETIME | YES | - | - |
| `current_ma20` | DECIMAL(18,2) | YES | - | - |
| `current_underlying_price` | DECIMAL(18,2) | YES | - | - |
| `current_etp_price` | DECIMAL(18,2) | YES | - | - |
| `breakout_rule_config_json` | TEXT | YES | - | - |
| `created_at` | DATETIME | NO | - | - |
| `last_modified` | DATETIME | YES | - | - |

**Indexes**:
- `idx_wcm_strategy_name` (UNIQUE) on `name`
- `idx_wcm_strategy_etp_symbol` on `etp_symbol`
- `idx_wcm_strategy_underlying_symbol` on `underlying_symbol`
- `idx_wcm_strategy_is_enabled` on `is_enabled`
- `idx_wcm_strategy_last_execution` on `last_execution_timestamp`

---

## 7. Relationships

### 7.1 Strategy → Account

**Relationship Type**: Conceptual (strategy operates on account state)

**Implementation**: No direct foreign key. Strategy calculates account state from:
- `Account.Cash` (from existing Account entity)
- `Position.Quantity` where `Symbol = EtpSymbol` (from existing Position entity)
- Total equity = Cash + (ETP Position Value)
- Cash ratio = Cash / Total Equity

**Rationale**: Strategy does not "own" the account. It observes account/position state and generates orders. Account is a singleton entity shared across all strategies.

### 7.2 Strategy → Orders

**Relationship Type**: Conceptual (strategy generates orders via `OrderExecutionService`)

**Implementation**: Orders created by the strategy have:
- `Order.StrategyName = strategy.Name` (string reference)
- No direct foreign key to avoid tight coupling

**Rationale**: Orders are generated by the strategy but are not directly owned. The strategy name acts as a logical grouping for filtering/reporting. This follows existing pattern where `Order.StrategyName` is a string field.

### 7.3 Strategy → Positions

**Relationship Type**: Conceptual (strategy manages positions for its ETP symbol)

**Implementation**: Strategy queries positions where:
- `Position.Symbol = strategy.EtpSymbol`
- `Position.StrategyName = strategy.Name`

**Rationale**: Positions are managed by `PortfolioManager` service. Strategy does not directly own positions but tracks them via symbol + strategy name filter.

### 7.4 Strategy → Candles (Market Data)

**Relationship Type**: Data dependency (strategy reads historical candles for MA20 calculation)

**Implementation**: Strategy uses `IMarketDataService` to fetch:
- Last 20 candles for `UnderlyingSymbol` (MA20 calculation)
- Current quote for `UnderlyingSymbol` and `EtpSymbol` (price checks)

**Rationale**: Candles are cached market data. No direct relationship needed. Strategy reads via service interface.

### 7.5 Entity Relationship Diagram

```
┌──────────────────────────────────────┐
│   WeeklyCashManagedStrategy          │
│   (Aggregate Root)                   │
├──────────────────────────────────────┤
│ - Id: Guid                           │
│ - Name: string (UNIQUE)              │
│ - EtpSymbol: string                  │
│ - UnderlyingSymbol: string           │
│ - Configuration: StrategyConfiguration│
│ - State: DaysBelowMA20, MA20, etc.   │
└────────┬─────────────────────────────┘
         │ (generates)
         │ StrategyName reference
         ▼
┌─────────────────────┐       ┌─────────────────────┐
│      Order          │       │      Position       │
│ (Existing Entity)   │       │ (Existing Entity)   │
├─────────────────────┤       ├─────────────────────┤
│ - StrategyName      │       │ - Symbol            │
│ - Symbol            │       │ - StrategyName      │
└─────────────────────┘       └─────────────────────┘
         │                             │
         │ (observes)                  │ (observes)
         ▼                             ▼
┌─────────────────────────────────────┐
│         Account                      │
│    (Existing Singleton)              │
├──────────────────────────────────────┤
│ - Cash: decimal                      │
│ - Equity: decimal                    │
└──────────────────────────────────────┘

         │ (reads)
         ▼
┌─────────────────────┐
│      Candle         │
│ (Market Data Cache) │
├─────────────────────┤
│ - Symbol            │
│ - Close             │
│ - Date              │
└─────────────────────┘
```

---

## 8. Repository Pattern

### 8.1 Repository Interface

**File**: `/src/TradingBot.Core/Interfaces/IWeeklyCashManagedStrategyRepository.cs`

```csharp
// <copyright file="IWeeklyCashManagedStrategyRepository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Strategy;

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Repository interface for WeeklyCashManagedStrategy aggregate root.
/// Extends base repository with strategy-specific queries.
/// </summary>
public interface IWeeklyCashManagedStrategyRepository : IRepository<WeeklyCashManagedStrategy>
{
    /// <summary>
    /// Gets a strategy by its unique name.
    /// </summary>
    /// <param name="name">The strategy name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The strategy or null if not found.</returns>
    Task<WeeklyCashManagedStrategy?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all enabled strategies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of enabled strategies.</returns>
    Task<IReadOnlyList<WeeklyCashManagedStrategy>> GetEnabledStrategiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets strategies that are due for execution based on the current day of week.
    /// </summary>
    /// <param name="currentDayOfWeek">Current day of week (0=Sunday, 6=Saturday).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of strategies due for execution.</returns>
    Task<IReadOnlyList<WeeklyCashManagedStrategy>> GetStrategiesDueForExecutionAsync(
        int currentDayOfWeek,
        CancellationToken cancellationToken = default);
}
```

### 8.2 Repository Implementation

**File**: `/src/TradingBot.Infrastructure/Repositories/WeeklyCashManagedStrategyRepository.cs`

```csharp
// <copyright file="WeeklyCashManagedStrategyRepository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Strategy;
using TradingBot.Infrastructure.Persistence;

namespace TradingBot.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for WeeklyCashManagedStrategy.
/// </summary>
internal sealed class WeeklyCashManagedStrategyRepository : EfRepository<WeeklyCashManagedStrategy>, IWeeklyCashManagedStrategyRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WeeklyCashManagedStrategyRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public WeeklyCashManagedStrategyRepository(TradingBotDbContext dbContext)
        : base(dbContext)
    {
    }

    /// <inheritdoc/>
    public async Task<WeeklyCashManagedStrategy?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<WeeklyCashManagedStrategy>()
            .FirstOrDefaultAsync(s => s.Name == name, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<WeeklyCashManagedStrategy>> GetEnabledStrategiesAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<WeeklyCashManagedStrategy>()
            .Where(s => s.IsEnabled)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<WeeklyCashManagedStrategy>> GetStrategiesDueForExecutionAsync(
        int currentDayOfWeek,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<WeeklyCashManagedStrategy>()
            .Where(s => s.IsEnabled && s.ExecutionDayOfWeek == currentDayOfWeek)
            .ToListAsync(cancellationToken);
    }
}
```

---

## 9. Data Migration

### 9.1 EF Core Migration Command

```bash
dotnet ef migrations add AddWeeklyCashManagedStrategy \
    --project src/TradingBot.Infrastructure \
    --startup-project src/TradingBot.Web \
    --output-dir Persistence/Migrations
```

### 9.2 Migration Script (Generated)

The migration will create:
1. `weekly_cash_managed_strategies` table with all columns
2. Unique index on `name`
3. Non-unique indexes on `etp_symbol`, `underlying_symbol`, `is_enabled`, `last_execution_timestamp`

---

## 10. Validation Summary Table

| Validation Type | Location | Enforcement |
|-----------------|----------|-------------|
| **Entity-level** | `WeeklyCashManagedStrategy.Validate()` | Called before save |
| **Value Object** | `StrategyConfiguration.Validate()` | Called in constructor |
| **Value Object** | `BreakoutRuleConfig.Validate()` | Called in constructor |
| **Business Rules** | Weekly routine service logic | Runtime checks before order execution |
| **Database Constraints** | EF Core configuration | SQLite CHECK constraints (if supported) |
| **API Input** | Web controller/service validation | Before entity creation/update |

---

## 11. Example Usage Scenarios

### 11.1 Creating a New Strategy

```csharp
var strategy = new WeeklyCashManagedStrategy
{
    Id = Guid.NewGuid(),
    Name = "BTC-Weekly-Strategy",
    EtpSymbol = "BTCW",
    UnderlyingSymbol = "COIN",
    IsEnabled = false,
    MinCashRatio = 0.15m,
    MaxCashRatio = 0.25m,
    WeeklyBuyRatio = 0.05m,
    WeeklySellRatio = 0.10m,
    ExecutionDayOfWeek = 5, // Friday
    DaysBelowMA20 = 0,
    CreatedAt = DateTime.UtcNow,
};

strategy.Validate(); // Throws if invalid
await _repository.AddAsync(strategy);
await _repository.SaveChangesAsync(); // Domain events dispatched here
```

### 11.2 Updating Daily Data

```csharp
var strategy = await _repository.GetByNameAsync("BTC-Weekly-Strategy");

// Daily routine updates MA20 and prices
strategy.UpdateDailyData(
    underlyingPrice: 150.25m,
    etpPrice: 148.50m,
    ma20: 145.00m);

await _repository.UpdateAsync(strategy);
await _repository.SaveChangesAsync(); // MA20UpdatedEvent dispatched
```

### 11.3 Recording Weekly Execution

```csharp
var strategy = await _repository.GetByNameAsync("BTC-Weekly-Strategy");

// Weekly routine executes buy order
var buyOrderId = Guid.NewGuid(); // Created by OrderExecutionService
var cashRatioAfter = 0.18m; // Calculated after order fills

strategy.RecordExecution(
    buyOrderId: buyOrderId,
    sellOrderId: null,
    cashRatioAfter: cashRatioAfter);

await _repository.UpdateAsync(strategy);
await _repository.SaveChangesAsync(); // StrategyExecutedEvent dispatched
```

---

## 12. Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| **Aggregate Root** | WeeklyCashManagedStrategy is the root for all strategy configuration and state. Ensures consistency. |
| **Value Objects** | StrategyConfiguration and BreakoutRuleConfig are immutable, validated at construction. |
| **Domain Events** | All state changes raise events for SignalR broadcasts, auditing, and event handlers. |
| **No Foreign Keys** | Strategy references Account, Orders, Positions conceptually via string names/symbols. Loose coupling. |
| **JSON Storage for Breakout Config** | Flexible storage for optional feature. Avoids separate table for 1:1 relationship. |
| **String StrategyName** | Follows existing pattern in Order/Position. Allows filtering without FK. |
| **Validation in Entity** | `Validate()` method ensures entity is always in valid state before persistence. |
| **Repository Pattern** | Abstracts data access. Enables unit testing with fake repositories. |
| **EF Core Configuration** | Fluent API in separate configuration class. Keeps entity clean. |
| **Domain Events via MediatR** | Existing infrastructure. Events dispatched before SaveChangesAsync. |

---

## Conclusion

This data model provides a complete, DDD-compliant design for the Weekly Cash-Managed Trading Strategy feature. It follows all established patterns in the TradingBot codebase, including:

- EntityBase<Guid> for entities
- ValueObject base class for immutable value objects
- DomainEventBase for events
- Repository pattern with IRepository<T>
- EF Core fluent configuration
- Validation at multiple levels
- Domain behavior methods in entities

The model is ready for implementation in **Phase 2: Implementation**.
