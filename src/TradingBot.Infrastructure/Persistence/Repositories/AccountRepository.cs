// <copyright file="AccountRepository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Portfolio;

namespace TradingBot.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Account entity operations.
/// </summary>
public class AccountRepository : Repository<Account>, IAccountRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public AccountRepository(TradingBotDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<Account?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Account?> GetByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(a => a.AccountId == accountId, cancellationToken);
    }
}
