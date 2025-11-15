// <copyright file="IDomainEvent.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using MediatR;

namespace TradingBot.Core.SharedKernel;

/// <summary>
/// Represents a domain event that can be published and handled.
/// </summary>
public interface IDomainEvent : INotification
{
  /// <summary>
  /// Gets the date and time when the event occurred.
  /// </summary>
  DateTime DateOccurred { get; }
}
