using TheAppManager.Modules;
using TradyStrat.Features.Settings.UseCases;

namespace TradyStrat.Modules;

public sealed class SettingsModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<UpdateGoalUseCase>();
        builder.Services.AddScoped<ProbeInstrumentUseCase>();
        builder.Services.AddScoped<AddInstrumentUseCase>();
        builder.Services.AddScoped<ListInstrumentsUseCase>();
    }
}
