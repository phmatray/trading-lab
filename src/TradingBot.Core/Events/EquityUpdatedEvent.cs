// <copyright file="EquityUpdatedEvent.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.SharedKernel;

namespace TradingBot.Core.Events;

/// <summary>
/// Domain event raised when an account's equity is updated.
/// </summary>
public sealed class EquityUpdatedEvent : DomainEventBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EquityUpdatedEvent"/> class.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="newEquity">The new equity value.</param>
    public EquityUpdatedEvent(string accountId, decimal newEquity)
    {
        AccountId = accountId;
        NewEquity = newEquity;
    }

    /// <summary>
    /// Gets the account identifier.
    /// </summary>
    public string AccountId { get; }

    /// <summary>
    /// Gets the new equity value.
    /// </summary>
    public decimal NewEquity { get; }
}
