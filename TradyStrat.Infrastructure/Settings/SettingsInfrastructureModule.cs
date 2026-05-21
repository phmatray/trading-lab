using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;
using TradyStrat.Application.Settings.Config;
using TradyStrat.Infrastructure.Settings.Config;
using TradyStrat.Infrastructure.Settings.UseCases;

namespace TradyStrat.Infrastructure.Settings;

public sealed class SettingsInfrastructureModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<ISettingsReader, SettingsReader>();
        services.AddHostedService<SettingsSeederHostedService>();
        services.AddScoped<UpdateGoalUseCase>();
        services.AddScoped<AddInstrumentUseCase>();
    }
}
