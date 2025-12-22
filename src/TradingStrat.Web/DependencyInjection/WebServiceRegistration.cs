using TradingStrat.Web.Services;
using TradingStrat.Web.Services.State;

namespace TradingStrat.Web.DependencyInjection;

public static class WebServiceRegistration
{
    public static IServiceCollection AddWeb(this IServiceCollection services)
    {
        // Singleton services (stateless, immutable metadata)
        services.AddSingleton<IndicatorMetadataService>();

        // Scoped services for Blazor Server circuits
        services.AddScoped<ProgressService>();
        services.AddScoped<ChartDataService>();

        // State management services
        services.AddScoped<LocalStorageService>();
        services.AddScoped<UserPreferencesService>();
        services.AddScoped<AppStateService>();
        services.AddScoped<ChatStateService>();
        services.AddScoped<FormStateService>();
        services.AddScoped<PortfolioStateService>();

        // Notification service
        services.AddScoped<NotificationService>();

        // Data freshness service
        services.AddScoped<IDataFreshnessService, DataFreshnessService>();

        // AI analysis service
        services.AddScoped<IDataAnalysisService, DataAnalysisService>();

        services.AddHttpContextAccessor();

        return services;
    }
}
