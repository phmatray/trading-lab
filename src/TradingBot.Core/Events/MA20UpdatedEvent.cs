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
