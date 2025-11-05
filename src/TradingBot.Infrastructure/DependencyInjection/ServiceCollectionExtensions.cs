// <copyright file="ServiceCollectionExtensions.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using TradingBot.Core.Interfaces;
using TradingBot.Engine;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.MarketData;
using TradingBot.Infrastructure.Persistence;
using TradingBot.Infrastructure.Persistence.Repositories;
using TradingBot.Infrastructure.Services;

namespace TradingBot.Infrastructure.DependencyInjection;

/// <summary>
/// Extension methods for configuring TradingBot services in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all TradingBot services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTradingBotServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=tradingbot.db";

        services.AddDbContext<TradingBotDbContext>(options =>
        {
            options.UseSqlite(connectionString);

            // Enable sensitive data logging in development
            if (configuration.GetValue<bool>("Logging:EnableSensitiveDataLogging"))
            {
                options.EnableSensitiveDataLogging();
            }
        });

        // Repositories (Scoped - tied to DbContext lifetime)
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IPositionRepository, PositionRepository>();
        services.AddScoped<ITradeRepository, TradeRepository>();
        services.AddScoped<ICandleRepository, CandleRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();

        // Infrastructure services
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddSingleton<IMarketDataService, YahooFinanceService>();
        services.AddScoped<IHistoricalDataCache, HistoricalDataCache>();
        services.AddSingleton<IConfigurationService, Configuration.ConfigurationService>();

        // Engine services
        services.AddSingleton<IStrategyEngine, StrategyEngine>();
        services.AddScoped<IOrderExecutionService, OrderExecutionService>();
        services.AddSingleton<IPortfolioManager, PortfolioManager>();
        services.AddSingleton<IRiskManager, RiskManager>();
        services.AddSingleton<SignalProcessor>();
        services.AddSingleton<IBacktestingEngine, BacktestingEngine>();

        // Logging with Serilog
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        return services;
    }

    /// <summary>
    /// Configures Serilog for the application.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The configured logger.</returns>
    public static Serilog.ILogger ConfigureSerilog(IConfiguration configuration)
    {
        return new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                "logs/tradingbot-.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }
}
