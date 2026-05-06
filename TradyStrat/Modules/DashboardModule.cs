using TheAppManager.Modules;
using TradyStrat.Application.UseCases.Dashboard;

namespace TradyStrat.Modules;

public sealed class DashboardModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<LoadDashboardUseCase>();
    }
}
