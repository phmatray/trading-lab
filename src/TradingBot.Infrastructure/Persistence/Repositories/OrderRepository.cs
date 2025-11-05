// <copyright file="OrderRepository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using TradingBot.Core.Enums;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Order entity operations.
/// </summary>
public class OrderRepository : Repository<Order>, IOrderRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrderRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public OrderRepository(TradingBotDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Order>> GetBySymbolAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(o => o.Symbol == symbol)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Order>> GetByStatusAsync(
        OrderStatus status,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(o => o.Status == status)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Order>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Order>> GetOpenOrdersAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(o => o.Status == OrderStatus.Pending ||
                       o.Status == OrderStatus.Submitted ||
                       o.Status == OrderStatus.PartiallyFilled)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
