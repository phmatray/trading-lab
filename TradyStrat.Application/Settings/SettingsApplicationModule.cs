using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;
using TradyStrat.Application.Settings.Config;
using TradyStrat.Application.Settings.UseCases;

namespace TradyStrat.Application.Settings;

public sealed class SettingsApplicationModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<ISettingsRegistry, SettingsRegistry>();
        services.AddScoped<UpdateSettingUseCase>();
        services.AddScoped<ProbeInstrumentUseCase>();
        services.AddScoped<ListInstrumentsUseCase>();
    }
}
