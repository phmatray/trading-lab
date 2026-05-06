using TheAppManager.Modules;
using TradyStrat.Application.UseCases.Settings;

namespace TradyStrat.Modules;

public sealed class SettingsModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<UpdateGoalUseCase>();
    }
}
