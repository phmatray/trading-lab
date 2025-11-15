// <copyright file="AccountSuspendedEvent.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Ardalis.SharedKernel;

namespace TradingBot.Core.Events;

/// <summary>
/// Domain event raised when an account is suspended.
/// </summary>
public sealed class AccountSuspendedEvent : DomainEventBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountSuspendedEvent"/> class.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="reason">The suspension reason.</param>
    public AccountSuspendedEvent(string accountId, string reason)
    {
        AccountId = accountId;
        Reason = reason;
    }

    /// <summary>
    /// Gets the account identifier.
    /// </summary>
    public string AccountId { get; }

    /// <summary>
    /// Gets the suspension reason.
    /// </summary>
    public string Reason { get; }
}
