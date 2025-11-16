// <copyright file="EntityBase.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

#pragma warning disable SA1402 // File may only contain a single type

namespace TradingBot.Core.SharedKernel;

/// <summary>
/// Base class for entities with domain event support. Default ID type is int.
/// </summary>
public abstract class EntityBase : HasDomainEventsBase
{
    /// <summary>
    /// Gets or sets the entity identifier.
    /// </summary>
    public int Id { get; set; }
}

/// <summary>
/// Generic base class for entities with custom ID types and domain event support.
/// </summary>
/// <typeparam name="TId">The type of the entity's identifier.</typeparam>
public abstract class EntityBase<TId> : HasDomainEventsBase
  where TId : struct, IEquatable<TId>
{
    /// <summary>
    /// Gets or sets the entity identifier.
    /// </summary>
    public TId Id { get; set; }
}

/// <summary>
/// Base class for entities with strongly-typed IDs (e.g., using Vogen).
/// </summary>
/// <typeparam name="T">The entity type itself.</typeparam>
/// <typeparam name="TId">The type of the entity's identifier.</typeparam>
public abstract class EntityBase<T, TId> : HasDomainEventsBase
  where T : EntityBase<T, TId>
  where TId : struct, IEquatable<TId>
{
    /// <summary>
    /// Gets or sets the entity identifier.
    /// </summary>
    public TId Id { get; set; }
}
