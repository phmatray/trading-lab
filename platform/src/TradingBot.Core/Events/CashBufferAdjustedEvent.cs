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
