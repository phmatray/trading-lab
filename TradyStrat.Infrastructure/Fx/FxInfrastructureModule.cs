using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;
using TradyStrat.Application.Fx.Providers;
using TradyStrat.Infrastructure.Fx.Providers;

namespace TradyStrat.Infrastructure.Fx;

public sealed class FxInfrastructureModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddHttpClient<IFxRateProvider, YahooFxProvider>(c =>
        {
            c.BaseAddress = new Uri(config["Yahoo:BaseUrl"] ?? "https://query1.finance.yahoo.com");
            c.Timeout = TimeSpan.FromSeconds(15);
            c.DefaultRequestHeaders.UserAgent.ParseAdd("TradyStrat/1.0");
        }).AddStandardResilienceHandler();

        services.AddScoped<DailyFxCache>();
    }
}
