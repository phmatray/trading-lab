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
    /// Records a cash buffer adjustment and raises a domain event.
    /// </summary>
    /// <param name="orderId">The adjustment order ID (buy or sell).</param>
    /// <param name="adjustmentType">The adjustment type (Buy or Sell).</param>
    /// <param name="cashRatioBefore">Cash ratio before adjustment.</param>
    /// <param name="cashRatioAfter">Cash ratio after adjustment.</param>
    public void RecordCashBufferAdjustment(Guid orderId, string adjustmentType, decimal cashRatioBefore, decimal cashRatioAfter)
    {
        LastModified = DateTime.UtcNow;

        RegisterDomainEvent(new CashBufferAdjustedEvent(
            Id,
            orderId,
            adjustmentType,
            cashRatioBefore,
            cashRatioAfter));
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
