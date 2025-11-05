// <copyright file="Repository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TradingBot.Core.Interfaces;

namespace TradingBot.Infrastructure.Persistence.Repositories;

/// <summary>
/// Base repository implementation for common data access operations.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
public class Repository<T> : IRepository<T>
    where T : class
{
    /// <summary>
    /// The database context.
    /// </summary>
    private readonly TradingBotDbContext context;

    /// <summary>
    /// The DbSet for the entity type.
    /// </summary>
    private readonly DbSet<T> dbSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="Repository{T}"/> class.
    /// </summary>
    /// <param name="tradingBotContext">The database context.</param>
    public Repository(TradingBotDbContext tradingBotContext)
    {
        context = tradingBotContext ?? throw new ArgumentNullException(nameof(tradingBotContext));
        dbSet = tradingBotContext.Set<T>();
    }

    /// <summary>
    /// Gets the database context.
    /// </summary>
    protected TradingBotDbContext Context => context;

    /// <summary>
    /// Gets the DbSet for the entity type.
    /// </summary>
    protected DbSet<T> DbSet => dbSet;

    /// <inheritdoc/>
    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.Where(predicate).ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    /// <inheritdoc/>
    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(entities, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        DbSet.Update(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public virtual Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        DbSet.Remove(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public virtual async Task<bool> DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity == null)
        {
            return false;
        }

        await DeleteAsync(entity, cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await Context.SaveChangesAsync(cancellationToken);
    }
}
