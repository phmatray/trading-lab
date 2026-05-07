using TheAppManager.Modules;
using TradyStrat.Features.Settings.UseCases;

namespace TradyStrat.Modules;

public sealed class SettingsModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<UpdateGoalUseCase>();
    }
}
