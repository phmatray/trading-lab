using TradyStrat.Infrastructure.Settings.Config;
using TradyStrat.Infrastructure.Settings.UseCases;
using TheAppManager.Modules;
using TradyStrat.Application.Settings.Config;
using TradyStrat.Application.Settings.UseCases;

namespace TradyStrat.Modules;
public sealed class SettingsModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ISettingsRegistry, SettingsRegistry>();
        builder.Services.AddScoped<ISettingsService, SettingsService>();
        builder.Services.AddScoped<ISettingsReader, SettingsReader>();
        builder.Services.AddScoped<UpdateSettingUseCase>();
        builder.Services.AddHostedService<SettingsSeederHostedService>();
        builder.Services.AddScoped<UpdateGoalUseCase>();
        builder.Services.AddScoped<ProbeInstrumentUseCase>();
        builder.Services.AddScoped<AddInstrumentUseCase>();
        builder.Services.AddScoped<ListInstrumentsUseCase>();
    }
}
