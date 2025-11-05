// <copyright file="IAccountRepository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Portfolio;

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Repository interface for Account entity operations.
/// </summary>
public interface IAccountRepository : IRepository<Account>
{
    /// <summary>
    /// Gets the current (first) account.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current account if any exist, null otherwise.</returns>
    Task<Account?> GetCurrentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the account by account identifier.
    /// </summary>
    /// <param name="accountId">Account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Account if found, null otherwise.</returns>
    Task<Account?> GetByAccountIdAsync(string accountId, CancellationToken cancellationToken = default);
}
