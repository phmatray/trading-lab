// <copyright file="TradingBotDbContext.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TradingBot.Core.Entities;
using TradingBot.Core.Enums;
using TradingBot.Core.Models.Backtest;
using TradingBot.Core.Models.Configuration;
using TradingBot.Core.Models.MarketData;
using TradingBot.Core.Models.Portfolio;
using TradingBot.Core.Models.Trading;
using TradingBot.Core.ValueObjects;

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

    /// <summary>
    /// Gets the UserPreferences DbSet.
    /// </summary>
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();

    /// <summary>
    /// Gets the StrategyConfigurations DbSet.
    /// </summary>
    public DbSet<StrategyConfiguration> StrategyConfigurations => Set<StrategyConfiguration>();

    /// <summary>
    /// Gets the RiskSettings DbSet.
    /// </summary>
    public DbSet<RiskSettings> RiskSettings => Set<RiskSettings>();

    /// <summary>
    /// Gets the BacktestResults DbSet.
    /// </summary>
    public DbSet<BacktestResult> BacktestResults => Set<BacktestResult>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TradingBotDbContext).Assembly);

        // Configure SmartEnum value converters manually
        ConfigureSmartEnumConverters(modelBuilder);

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

    /// <summary>
    /// Configures SmartEnum value converters for all properties.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    private static void ConfigureSmartEnumConverters(ModelBuilder modelBuilder)
    {
        var signalTypeConverter = new ValueConverter<SignalType, int>(
            v => v.Value,
            v => SignalType.FromValue(v));

        var orderTypeConverter = new ValueConverter<OrderType, int>(
            v => v.Value,
            v => OrderType.FromValue(v));

        var orderSideConverter = new ValueConverter<OrderSide, int>(
            v => v.Value,
            v => OrderSide.FromValue(v));

        var orderStatusConverter = new ValueConverter<OrderStatus, int>(
            v => v.Value,
            v => OrderStatus.FromValue(v));

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(SignalType))
                {
                    property.SetValueConverter(signalTypeConverter);
                }
                else if (property.ClrType == typeof(OrderType))
                {
                    property.SetValueConverter(orderTypeConverter);
                }
                else if (property.ClrType == typeof(OrderSide))
                {
                    property.SetValueConverter(orderSideConverter);
                }
                else if (property.ClrType == typeof(OrderStatus))
                {
                    property.SetValueConverter(orderStatusConverter);
                }
            }
        }
    }
}
