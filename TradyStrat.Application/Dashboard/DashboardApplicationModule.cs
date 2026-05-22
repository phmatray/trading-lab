using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;
using TradyStrat.Application.Dashboard.Navigation;

namespace TradyStrat.Application.Dashboard;

public sealed class DashboardApplicationModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IEntryNavigationService, EntryNavigationService>();
        // LoadDashboardUseCase + BuildFocusDerivedSliceUseCase .bak'd during Phase 3
        // Suggestion-AR rewrite; restored when use cases are rewritten.
    }
}
