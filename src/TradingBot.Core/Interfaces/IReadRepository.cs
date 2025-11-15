// <copyright file="IReadRepository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Read-only repository interface for query operations.
/// TODO: Will extend Ardalis.SharedKernel IReadRepositoryBase after entities implement IAggregateRoot (Phase 4).
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
public interface IReadRepository<T>
    where T : class
{
    /// <summary>
    /// Gets an entity by its identifier.
    /// </summary>
    /// <param name="id">Entity identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Entity if found, null otherwise.</returns>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all entities.</returns>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
}
