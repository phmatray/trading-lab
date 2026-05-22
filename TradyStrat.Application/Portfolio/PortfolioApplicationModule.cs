using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;

namespace TradyStrat.Application.Portfolio;

public sealed class PortfolioApplicationModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        // TODO(Phase2): PortfolioService and GrowthSeriesBuilder are offline (.bak).
        // Registrations will be restored when the Portfolio AR use cases are wired (Task 30).
    }
}
