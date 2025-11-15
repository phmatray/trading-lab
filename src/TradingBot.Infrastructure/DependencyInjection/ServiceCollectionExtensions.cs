// <copyright file="ServiceCollectionExtensions.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Ardalis.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using TradingBot.Analytics;
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
        // MediatR for domain events
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly);
        });

        // Domain Event Dispatcher (from Ardalis.SharedKernel)
        services.AddScoped<IDomainEventDispatcher, MediatorDomainEventDispatcher>();

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

        // Generic repositories (Ardalis.SharedKernel support)
        services.AddScoped(typeof(Ardalis.SharedKernel.IRepository<>), typeof(EfRepository<>));
        services.AddScoped(typeof(Ardalis.SharedKernel.IReadRepository<>), typeof(EfReadRepository<>));

        // Repositories (Scoped - tied to DbContext lifetime)
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IPositionRepository, PositionRepository>();
        services.AddScoped<ITradeRepository, TradeRepository>();
        services.AddScoped<ICandleRepository, CandleRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IUserPreferencesRepository, UserPreferencesRepository>();
        services.AddScoped<IRiskSettingsRepository, RiskSettingsRepository>();
        services.AddScoped<IStrategyConfigurationRepository, StrategyConfigurationRepository>();
        services.AddScoped<IBacktestResultRepository, BacktestResultRepository>();

        // Infrastructure services
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddSingleton<IMarketDataService, YahooFinanceService>();
        services.AddScoped<IHistoricalDataCache, HistoricalDataCache>();
        services.AddSingleton<IConfigurationService, Configuration.ConfigurationService>();
        services.AddScoped<IUserPreferencesService, UserPreferencesService>();
        services.AddHttpClient<ISymbolSearchService, YahooFinanceSymbolSearchService>();
        services.AddMemoryCache();

        // Engine services
        // Note: Changed to Scoped to avoid DI lifetime conflicts with DbContext-dependent services
        services.AddScoped<IStrategyEngine, StrategyEngine>();
        services.AddScoped<IOrderExecutionService, OrderExecutionService>();
        services.AddScoped<IPortfolioManager, PortfolioManager>();
        services.AddScoped<IRiskManager, RiskManager>();
        services.AddScoped<IStopLossManager, StopLossManager>();
        services.AddScoped<IPositionSizeCalculator, PositionSizeCalculator>();
        services.AddScoped<SignalProcessor>();

        // Analytics services - Changed to Scoped as they depend on IPortfolioManager
        services.AddScoped<IBacktestingEngine, Analytics.BacktestingEngine>();
        services.AddScoped<Analytics.EquityCurveGenerator>();
        services.AddScoped<Analytics.DrawdownAnalyzer>();
        services.AddScoped<Analytics.MetricsCalculator>();

        // Background Jobs - Disabled to avoid DI lifetime conflicts with web applications
        // These services require singleton lifetime but depend on scoped services
        // For CLI applications, these can be re-enabled with refactored dependencies

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
