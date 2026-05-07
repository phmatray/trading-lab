using TheAppManager.Modules;
using TradyStrat.Features.Dashboard.UseCases;

namespace TradyStrat.Modules;

public sealed class DashboardModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<LoadDashboardUseCase>();
    }
}
