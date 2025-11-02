// <copyright file="TradingBotDbContext.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using TradingBot.Core.Models.MarketData;
using TradingBot.Core.Models.Portfolio;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core database context for TradingBot.
/// </summary>
public sealed class TradingBotDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TradingBotDbContext"/> class.
    /// </summary>
    /// <param name="options">The options for this context.</param>
    public TradingBotDbContext(DbContextOptions<TradingBotDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets the Orders DbSet.
    /// </summary>
    public DbSet<Order> Orders => Set<Order>();

    /// <summary>
    /// Gets the Positions DbSet.
    /// </summary>
    public DbSet<Position> Positions => Set<Position>();

    /// <summary>
    /// Gets the Trades DbSet.
    /// </summary>
    public DbSet<Trade> Trades => Set<Trade>();

    /// <summary>
    /// Gets the Candles DbSet.
    /// </summary>
    public DbSet<Candle> Candles => Set<Candle>();

    /// <summary>
    /// Gets the Quotes DbSet.
    /// </summary>
    public DbSet<Quote> Quotes => Set<Quote>();

    /// <summary>
    /// Gets the Accounts DbSet.
    /// </summary>
    public DbSet<Account> Accounts => Set<Account>();

    /// <summary>
    /// Gets the EquityPoints DbSet.
    /// </summary>
    public DbSet<EquityPoint> EquityPoints => Set<EquityPoint>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TradingBotDbContext).Assembly);

        // SQLite-specific configuration
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            // SQLite doesn't support decimal, so use TEXT with custom converter
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                    {
                        property.SetColumnType("TEXT");
                    }
                }
            }
        }
    }
}
