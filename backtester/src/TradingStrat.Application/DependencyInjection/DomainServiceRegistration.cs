using Microsoft.Extensions.DependencyInjection;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.Services.Indicators;

namespace TradingStrat.Application.DependencyInjection;

/// <summary>
/// Extension methods for registering Domain layer services with the DI container.
/// Centralized registration point for all domain services to avoid duplication
/// across Application and Infrastructure layers.
/// </summary>
public static class DomainServiceRegistration
{
    /// <summary>
    /// Registers all domain services (business logic with zero external dependencies).
    /// Domain services are registered as Transient to ensure stateless behavior.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        // Indicator Calculator (used by strategies and ML feature engineering)
        services.AddTransient<IIndicatorCalculator, IndicatorCalculator>();

        // Performance Calculator (used by backtest engine)
        services.AddTransient<PerformanceCalculator>();

        // Portfolio Domain Services (used by portfolio use cases)
        services.AddTransient<PortfolioValuationService>();
        services.AddTransient<PortfolioRebalancingService>();
        services.AddTransient<PortfolioPerformanceService>();

        // Parameter Optimization (used by optimization use cases)
        services.AddTransient<IParameterOptimizer, ParameterOptimizer>();

        // Data Coverage Service (used by dashboard and data status use cases)
        services.AddSingleton<DataCoverageService>();

        return services;
    }
}
