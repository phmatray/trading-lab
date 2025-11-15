// <copyright file="PositionClosedEvent.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Ardalis.SharedKernel;

namespace TradingBot.Core.Events;

/// <summary>
/// Domain event raised when a position is closed.
/// </summary>
public sealed class PositionClosedEvent : DomainEventBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PositionClosedEvent"/> class.
    /// </summary>
    /// <param name="positionId">The position identifier.</param>
    /// <param name="symbol">The symbol.</param>
    /// <param name="realizedPnL">The realized profit and loss.</param>
    public PositionClosedEvent(Guid positionId, string symbol, decimal realizedPnL)
    {
        PositionId = positionId;
        Symbol = symbol;
        RealizedPnL = realizedPnL;
    }

    /// <summary>
    /// Gets the position identifier.
    /// </summary>
    public Guid PositionId { get; }

    /// <summary>
    /// Gets the symbol.
    /// </summary>
    public string Symbol { get; }

    /// <summary>
    /// Gets the realized profit and loss.
    /// </summary>
    public decimal RealizedPnL { get; }
}
