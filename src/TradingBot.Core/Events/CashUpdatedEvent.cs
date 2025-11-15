// <copyright file="CashUpdatedEvent.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Ardalis.SharedKernel;

namespace TradingBot.Core.Events;

/// <summary>
/// Domain event raised when an account's cash balance is updated.
/// </summary>
public sealed class CashUpdatedEvent : DomainEventBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CashUpdatedEvent"/> class.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="newBalance">The new cash balance.</param>
    public CashUpdatedEvent(string accountId, decimal newBalance)
    {
        AccountId = accountId;
        NewBalance = newBalance;
    }

    /// <summary>
    /// Gets the account identifier.
    /// </summary>
    public string AccountId { get; }

    /// <summary>
    /// Gets the new cash balance.
    /// </summary>
    public decimal NewBalance { get; }
}
