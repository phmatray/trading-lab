using TradingStrat.Web.Services;

namespace TradingStrat.Web.DependencyInjection;

public static class WebServiceRegistration
{
    public static IServiceCollection AddWeb(this IServiceCollection services)
    {
        // Scoped services for Blazor Server circuits
        services.AddScoped<ProgressService>();
        services.AddScoped<ChartDataService>();
        services.AddHttpContextAccessor();

        return services;
    }
}
