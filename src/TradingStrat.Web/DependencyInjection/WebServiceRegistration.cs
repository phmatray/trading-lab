using TradingStrat.Web.Services;
using TradingStrat.Web.Services.State;

namespace TradingStrat.Web.DependencyInjection;

public static class WebServiceRegistration
{
    public static IServiceCollection AddWeb(this IServiceCollection services)
    {
        // Scoped services for Blazor Server circuits
        services.AddScoped<ProgressService>();
        services.AddScoped<ChartDataService>();

        // State management services
        services.AddScoped<LocalStorageService>();
        services.AddScoped<UserPreferencesService>();
        services.AddScoped<AppStateService>();
        services.AddScoped<ChatStateService>();
        services.AddScoped<FormStateService>();

        services.AddHttpContextAccessor();

        return services;
    }
}
