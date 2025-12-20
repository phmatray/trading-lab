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
        services.Configure<AssistantConfiguration>(
            configuration.GetSection("Trading:Assistant"));

        // Domain Services (from Domain layer)
        services.AddTransient<IIndicatorCalculator, IndicatorCalculator>();
        services.AddTransient<PerformanceCalculator>();

        // Application Services
        services.AddScoped<BacktestEngine>();
        services.AddScoped<IStrategyFactory, StrategyFactory>();
        services.AddSingleton<ITickerResolver, TickerResolver>();
        services.AddScoped<PortfolioContextBuilder>();

        // Use Cases
        services.AddScoped<IDataFetchingUseCase, FetchHistoricalDataUseCase>();
        services.AddScoped<IBacktestUseCase, RunBacktestUseCase>();
        services.AddScoped<ILiveAnalysisUseCase, AnalyzeCurrentPositionUseCase>();
        services.AddScoped<IParameterOptimizationUseCase, RunParameterOptimizationUseCase>();
        services.AddScoped<ISendChatMessageUseCase, SendChatMessageUseCase>();
        services.AddScoped<IAnalyzeStrategyUseCase, AnalyzeStrategyUseCase>();

        return services;
    }
}
