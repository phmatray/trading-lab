using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradingStrat.Application.Configuration;
using TradingStrat.Application.Factories;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Services;
using TradingStrat.Application.UseCases;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.Services.Indicators;

namespace TradingStrat.Application.DependencyInjection;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure options
        services.Configure<TradingConfiguration>(
            configuration.GetSection("Trading"));

        // Domain Services (from Domain layer)
        services.AddTransient<IIndicatorCalculator, IndicatorCalculator>();
        services.AddTransient<PerformanceCalculator>();

        // Application Services
        services.AddScoped<BacktestEngine>();
        services.AddScoped<IStrategyFactory, StrategyFactory>();
        services.AddTransient<FeatureEngineering>();
        services.AddSingleton<ITickerResolver, TickerResolver>();

        // Use Cases
        services.AddScoped<IDataFetchingUseCase, FetchHistoricalDataUseCase>();
        services.AddScoped<IBacktestUseCase, RunBacktestUseCase>();
        services.AddScoped<ILiveAnalysisUseCase, AnalyzeCurrentPositionUseCase>();

        return services;
    }
}
