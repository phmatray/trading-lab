using TradingStrat.Web.Services;
using TradingStrat.Web.Services.State;

namespace TradingStrat.Web.DependencyInjection;

public static class WebServiceRegistration
{
    public static IServiceCollection AddWeb(this IServiceCollection services)
    {
        // Memory cache for data status and ticker lists
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 100; // Limit to 100 cached entries
        });

        // Singleton services (stateless, immutable metadata)
        services.AddSingleton<IndicatorMetadataService>();

        // Scoped services for Blazor Server circuits
        services.AddScoped<ProgressService>();
        services.AddScoped<ChartDataService>();

        // Cache services
        services.AddScoped<IDataStatusCacheService, DataStatusCacheService>();
        services.AddScoped<ITickerListCacheService, TickerListCacheService>();

        // State management services
        services.AddScoped<LocalStorageService>();
        services.AddScoped<UserPreferencesService>();
        services.AddScoped<AppStateService>();
        services.AddScoped<ChatStateService>();
        services.AddScoped<FormStateService>();
        services.AddScoped<PortfolioStateService>();
        services.AddScoped<WorkspaceStateService>();

        // Notification service
        services.AddScoped<NotificationService>();

        // Data freshness service
        services.AddScoped<IDataFreshnessService, DataFreshnessService>();

        // AI analysis service
        services.AddScoped<IDataAnalysisService, DataAnalysisService>();
        services.AddScoped<AiInsightsService>();

        services.AddHttpContextAccessor();

        return services;
    }
}
