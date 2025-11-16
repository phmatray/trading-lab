// <copyright file="DomainEventBase.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.SharedKernel;

/// <summary>
/// A base type for domain events. Depends on Mediator INotification.
/// Includes DateOccurred which is set on creation.
/// </summary>
public abstract class DomainEventBase : IDomainEvent
{
    /// <summary>
    /// Gets or sets the date and time when the event occurred.
    /// </summary>
    public DateTime DateOccurred { get; protected set; } = DateTime.UtcNow;
}
