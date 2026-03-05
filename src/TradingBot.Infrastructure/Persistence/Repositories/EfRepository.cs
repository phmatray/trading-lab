// <copyright file="EfRepository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Ardalis.Specification.EntityFrameworkCore;
using TradingBot.Core.SharedKernel;

namespace TradingBot.Infrastructure.Persistence.Repositories;

/// <summary>
/// Generic repository implementation using Entity Framework Core.
/// Extends Ardalis.Specification's RepositoryBase for full DDD support.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public class EfRepository<T> : RepositoryBase<T>
    where T : class, IAggregateRoot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EfRepository{T}"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public EfRepository(TradingBotDbContext dbContext)
        : base(dbContext)
    {
    }
}
