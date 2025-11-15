// <copyright file="IDomainEventDispatcher.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.SharedKernel;

/// <summary>
/// A simple interface for sending domain events. Can use MediatR or any other implementation.
/// </summary>
public interface IDomainEventDispatcher
{
  /// <summary>
  /// Dispatches domain events from entities and clears them.
  /// </summary>
  /// <param name="entitiesWithEvents">The entities containing domain events.</param>
  /// <returns>A task representing the asynchronous operation.</returns>
  Task DispatchAndClearEvents(IEnumerable<IHasDomainEvents> entitiesWithEvents);
}
