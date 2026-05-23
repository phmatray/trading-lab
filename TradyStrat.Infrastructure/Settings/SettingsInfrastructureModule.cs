using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;
using TradyStrat.Application.Goals;
using TradyStrat.Application.Settings;
using TradyStrat.Infrastructure.Goals;
using TradyStrat.Infrastructure.Settings.Config;
using TradyStrat.Infrastructure.Settings.UseCases;

namespace TradyStrat.Infrastructure.Settings;

public sealed class SettingsInfrastructureModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddHostedService<SettingsSeederHostedService>();
        services.AddScoped<UpdateGoalUseCase>();
        services.AddScoped<AddInstrumentUseCase>();
        services.AddScoped<IInstrumentRepository, EfInstrumentRepository>();
        services.AddScoped<IGoalRepository, EfGoalRepository>();
        services.AddScoped<IAnthropicSettingsRepository, EfAnthropicSettingsRepository>();
        services.AddScoped<IPolymarketSettingsRepository, EfPolymarketSettingsRepository>();
        services.AddScoped<IFocusTickerRepository, EfFocusTickerRepository>();
    }
}
