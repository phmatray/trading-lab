// <copyright file="TradingBotDbContextFactory.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TradingBot.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for TradingBotDbContext.
/// </summary>
public sealed class TradingBotDbContextFactory : IDesignTimeDbContextFactory<TradingBotDbContext>
{
    /// <inheritdoc/>
    public TradingBotDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TradingBotDbContext>();

        // Use SQLite for design-time (migrations)
        optionsBuilder.UseSqlite("Data Source=tradingbot.db");

        return new TradingBotDbContext(optionsBuilder.Options);
    }
}
