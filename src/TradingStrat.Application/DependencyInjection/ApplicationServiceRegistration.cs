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
        services.AddScoped<MarketPriceService>();
        services.AddSingleton<StrategyParameterDefaults>();
        services.AddScoped<AiRecommendationService>();
        services.AddScoped<IDataRefreshService, DataRefreshService>();
        services.AddSingleton<ICsvTickerParser, CsvTickerParser>();

        // Use Cases
        services.AddScoped<IDataFetchingUseCase, FetchHistoricalDataUseCase>();
        services.AddScoped<IBulkDataFetchingUseCase, BulkFetchHistoricalDataUseCase>();
        services.AddScoped<IDeleteHistoricalDataUseCase, DeleteHistoricalDataUseCase>();
        services.AddScoped<IExportHistoricalDataUseCase, ExportHistoricalDataUseCase>();
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

        // Custom Strategy Use Cases (CQRS-lite)
        services.AddScoped<ICustomStrategyQueryUseCase, CustomStrategyQueryUseCase>();
        services.AddScoped<ICustomStrategyCommandUseCase, CustomStrategyCommandUseCase>();

#pragma warning disable CS0618 // Type or member is obsolete
        services.AddScoped<ICustomStrategyManagementUseCase, CustomStrategyManagementUseCase>(); // Legacy facade for backward compatibility
#pragma warning restore CS0618 // Type or member is obsolete

        services.AddScoped<IOptimizeStrategyParametersUseCase, OptimizeStrategyParametersUseCase>();

        // Dashboard Use Cases
        services.AddScoped<IGetDashboardStatsUseCase, GetDashboardStatsUseCase>();
        services.AddScoped<IGetRecentActivityUseCase, GetRecentActivityUseCase>();
        services.AddScoped<IGetTopStrategiesUseCase, GetTopStrategiesUseCase>();
        services.AddScoped<IGetAllDataStatusUseCase, GetAllDataStatusUseCase>();

        // Backtest Archive Use Cases
        services.AddScoped<ISaveBacktestRunUseCase, SaveBacktestRunUseCase>();
        services.AddScoped<IGetBacktestArchiveUseCase, GetBacktestArchiveUseCase>();

        // Strategy Comparison Use Cases
        services.AddScoped<IMultiStrategyComparisonUseCase, MultiStrategyComparisonUseCase>();

        return services;
    }
}
