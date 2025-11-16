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
