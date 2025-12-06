using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradingStrat.Application.Ports.Outbound;
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
        var connectionString = configuration.GetSection("Trading:Database:ConnectionString").Value
            ?? "Data Source=trading.db";

        services.AddDbContext<TradingContext>(options =>
            options.UseSqlite(connectionString));

        // Port implementations
        services.AddScoped<IHistoricalDataPort, HistoricalDataRepository>();
        services.AddScoped<IMarketDataPort, YahooFinanceAdapter>();
        services.AddScoped<IExportPort, ExportAdapter>();
        services.AddSingleton<IMLModelPort, MlNetModelAdapter>();

        return services;
    }
}
