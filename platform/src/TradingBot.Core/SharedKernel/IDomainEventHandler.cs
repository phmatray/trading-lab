// <copyright file="IDomainEventHandler.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using MediatR;

namespace TradingBot.Core.SharedKernel;

/// <summary>
/// Marker interface for domain event handlers.
/// </summary>
/// <typeparam name="T">The type of domain event to handle.</typeparam>
#pragma warning disable S3246 // Generic type parameters should be co/contravariant when possible
public interface IDomainEventHandler<T> : INotificationHandler<T>
  where T : IDomainEvent
{
}
#pragma warning restore S3246 // Generic type parameters should be co/contravariant when possible
