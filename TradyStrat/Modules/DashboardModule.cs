using TheAppManager.Modules;
using TradyStrat.Application.Dashboard.Navigation;
using TradyStrat.Application.Dashboard.UseCases;

namespace TradyStrat.Modules;

public sealed class DashboardModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<LoadDashboardUseCase>();
        builder.Services.AddScoped<IEntryNavigationService, EntryNavigationService>();
    }
}
