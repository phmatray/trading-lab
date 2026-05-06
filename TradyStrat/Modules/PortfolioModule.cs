using TheAppManager.Modules;
using TradyStrat.Features.Portfolio;

namespace TradyStrat.Modules;

public sealed class PortfolioModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<PortfolioService>();
        builder.Services.AddScoped<GrowthSeriesBuilder>();
    }
}
