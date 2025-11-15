// <copyright file="HasDomainEventsBase.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations.Schema;

namespace TradingBot.Core.SharedKernel;

/// <summary>
/// Base class that provides domain event management functionality.
/// </summary>
public abstract class HasDomainEventsBase : IHasDomainEvents
{
  private readonly List<IDomainEvent> _domainEvents = new();

  /// <summary>
  /// Gets the collection of domain events.
  /// </summary>
  [NotMapped]
  public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

  /// <summary>
  /// Clears all domain events.
  /// </summary>
  public void ClearDomainEvents() => _domainEvents.Clear();

  /// <summary>
  /// Registers a domain event.
  /// </summary>
  /// <param name="domainEvent">The domain event to register.</param>
  protected void RegisterDomainEvent(DomainEventBase domainEvent) => _domainEvents.Add(domainEvent);
}
