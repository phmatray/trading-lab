using Microsoft.Extensions.DependencyInjection;
using TradingStrat.Presentation.Console;

namespace TradingStrat.Presentation.DependencyInjection;

public static class PresentationServiceRegistration
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        // Register menu
        services.AddScoped<ProgramMenu>();

        // Presenters are static classes, so no registration needed

        return services;
    }
}
