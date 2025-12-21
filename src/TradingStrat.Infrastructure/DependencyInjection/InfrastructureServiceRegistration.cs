using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Services;
using TradingStrat.Infrastructure.Assistant;
using TradingStrat.Infrastructure.Export;
using TradingStrat.Infrastructure.MachineLearning;
using TradingStrat.Infrastructure.MarketData;
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

        // Port implementations
        services.AddScoped<IHistoricalDataPort, HistoricalDataRepository>();
        services.AddScoped<IMarketDataPort, YahooFinanceAdapter>();
        services.AddScoped<IExportPort, ExportAdapter>();
        services.AddSingleton<IMLModelPort, MlNetModelAdapter>();
        services.AddScoped<IAssistantPort, AnthropicAdapter>();
        services.AddScoped<IChatHistoryPort, ChatHistoryRepository>();
        services.AddScoped<IPortfolioPort, PortfolioRepository>();
        services.AddScoped<IPortfolioExportPort, PortfolioCsvAdapter>();

        // Domain services (no external dependencies, but registered for DI)
        services.AddTransient<PortfolioValuationService>();
        services.AddTransient<PortfolioRebalancingService>();
        services.AddTransient<PortfolioPerformanceService>();

        return services;
    }
}
