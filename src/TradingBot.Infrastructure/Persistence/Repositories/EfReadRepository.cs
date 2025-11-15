// <copyright file="EfReadRepository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Ardalis.Specification.EntityFrameworkCore;
using TradingBot.Core.SharedKernel;

namespace TradingBot.Infrastructure.Persistence.Repositories;

/// <summary>
/// Generic read-only repository implementation using Entity Framework Core.
/// Extends Ardalis.Specification's RepositoryBase for query support.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public class EfReadRepository<T> : RepositoryBase<T>
    where T : class, IAggregateRoot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EfReadRepository{T}"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public EfReadRepository(TradingBotDbContext dbContext)
        : base(dbContext)
    {
    }
}
