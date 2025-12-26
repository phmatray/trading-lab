using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradingStrat.Application.Configuration;
using TradingStrat.Application.Factories;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Services;
using TradingStrat.Application.Strategies;
using TradingStrat.Application.UseCases;

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

        // Domain Services (centralized registration)
        services.AddDomainServices();

        // Strategy Registry (singleton - immutable metadata)
        services.AddSingleton<IStrategyRegistry, StrategyRegistry>();

        // Application Services
        services.AddScoped<BacktestEngine>();
        services.AddScoped<IStrategyFactory, StrategyFactory>();
        services.AddSingleton<ITickerResolver, TickerResolver>();
        services.AddScoped<PortfolioContextBuilder>();
        services.AddScoped<MarketRegimeDetector>();
        services.AddScoped<AiRecommendationService>();

        // Use Cases
        services.AddScoped<IDataFetchingUseCase, FetchHistoricalDataUseCase>();
        services.AddScoped<IBacktestUseCase, RunBacktestUseCase>();
        services.AddScoped<ILiveAnalysisUseCase, AnalyzeCurrentPositionUseCase>();
        services.AddScoped<IParameterOptimizationUseCase, RunParameterOptimizationUseCase>();
        services.AddScoped<ISendChatMessageUseCase, SendChatMessageUseCase>();
        services.AddScoped<IAnalyzeStrategyUseCase, AnalyzeStrategyUseCase>();

        // Portfolio Use Cases
        services.AddScoped<ICreatePortfolioUseCase, CreatePortfolioUseCase>();
        services.AddScoped<IManagePositionsUseCase, ManagePositionsUseCase>();
        services.AddScoped<IManageCashUseCase, ManageCashUseCase>();
        services.AddScoped<IGetPortfolioSnapshotUseCase, GetPortfolioSnapshotUseCase>();
        services.AddScoped<ICalculateRebalancingUseCase, CalculateRebalancingUseCase>();
        services.AddScoped<IGetPortfolioPerformanceUseCase, GetPortfolioPerformanceUseCase>();

        // Custom Strategy Use Cases
        services.AddScoped<ICustomStrategyManagementUseCase, CustomStrategyManagementUseCase>();
        services.AddScoped<IOptimizeStrategyParametersUseCase, OptimizeStrategyParametersUseCase>();

        // Dashboard Use Cases
        services.AddScoped<IGetDashboardStatsUseCase, GetDashboardStatsUseCase>();
        services.AddScoped<IGetRecentActivityUseCase, GetRecentActivityUseCase>();
        services.AddScoped<IGetTopStrategiesUseCase, GetTopStrategiesUseCase>();
        services.AddScoped<IGetAllDataStatusUseCase, GetAllDataStatusUseCase>();

        // Backtest Archive Use Cases
        services.AddScoped<ISaveBacktestRunUseCase, SaveBacktestRunUseCase>();
        services.AddScoped<IGetBacktestArchiveUseCase, GetBacktestArchiveUseCase>();

        return services;
    }
}
