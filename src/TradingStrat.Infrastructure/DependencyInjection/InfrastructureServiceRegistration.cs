using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.Services;
using TradingStrat.Domain.Services;
using TradingStrat.Infrastructure.Assistant;
using TradingStrat.Infrastructure.BackgroundServices;
using TradingStrat.Infrastructure.Configuration;
using TradingStrat.Infrastructure.Export;
using TradingStrat.Infrastructure.MachineLearning;
using TradingStrat.Infrastructure.MarketData;
using TradingStrat.Infrastructure.Persistence;
using TradingStrat.Infrastructure.Persistence.EfCore;

namespace TradingStrat.Infrastructure.DependencyInjection;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database context
        string connectionString = configuration.GetSection("Trading:Database:ConnectionString").Value
            ?? "Data Source=trading.db";

        services.AddDbContext<TradingContext>(options =>
            options.UseSqlite(connectionString));

        // HTTP Clients
        services.AddHttpClient("Anthropic");
        services.AddHttpClient<YahooFinanceAdapter>();
        services.AddHttpClient<AlphaVantageAdapter>();

        // Port implementations
        services.AddScoped<IHistoricalDataPort, HistoricalDataRepository>();

        // Market Data Adapters (register both, factory will select based on timeframe)
        services.AddScoped<YahooFinanceAdapter>();
        services.AddScoped<AlphaVantageAdapter>();
        services.AddScoped<MarketDataPortFactory>();

        // Keep default IMarketDataPort for backward compatibility (uses Yahoo Finance)
        services.AddScoped<IMarketDataPort>(sp => sp.GetRequiredService<YahooFinanceAdapter>());
        services.AddScoped<ICoverageReportExporter, CoverageReportCsvAdapter>();
        services.AddScoped<IHistoricalDataExporter, HistoricalDataExportAdapter>();
        services.AddSingleton<IMLModelPort, MlNetModelAdapter>();
        services.AddScoped<IMLPredictionService, MLPredictionService>();
        services.AddScoped<IAssistantPort, AnthropicAdapter>();
        services.AddScoped<IChatHistoryPort, ChatHistoryRepository>();
        services.AddScoped<IPortfolioPort, PortfolioRepository>();
        services.AddScoped<IPortfolioExportPort, PortfolioCsvAdapter>();
        services.AddScoped<ICustomStrategyPort, CustomStrategyRepository>();
        services.AddScoped<IBacktestArchivePort, BacktestArchiveRepository>();
        services.AddScoped<IActivityEventPort, ActivityEventRepository>();

        // Domain services (singleton - no state)
        services.AddSingleton<DataCoverageService>();
        services.AddSingleton<StrategyDefinitionValidator>();

        // Data Refresh Background Service
        services.Configure<DataRefreshConfiguration>(
            configuration.GetSection("Trading:DataRefresh"));
        services.AddHostedService<DataRefreshBackgroundService>();

        return services;
    }
}
