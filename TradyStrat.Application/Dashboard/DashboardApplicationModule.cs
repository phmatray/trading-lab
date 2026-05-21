using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;
using TradyStrat.Application.Dashboard.Navigation;
using TradyStrat.Application.Dashboard.UseCases;

namespace TradyStrat.Application.Dashboard;

public sealed class DashboardApplicationModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<LoadDashboardUseCase>();
        services.AddScoped<BuildFocusDerivedSliceUseCase>();
        services.AddScoped<IEntryNavigationService, EntryNavigationService>();
    }
}
