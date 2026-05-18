using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;

namespace TradyStrat.Application.Portfolio;

public sealed class PortfolioApplicationModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<PortfolioService>();
        services.AddScoped<GrowthSeriesBuilder>();
    }
}
