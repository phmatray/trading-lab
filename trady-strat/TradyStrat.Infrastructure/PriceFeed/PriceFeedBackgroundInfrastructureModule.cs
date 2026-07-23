using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;

namespace TradyStrat.Infrastructure.PriceFeed;

/// <summary>
/// Registered separately from <see cref="PriceFeedInfrastructureModule"/> so non-Blazor
/// hosts (CLI) can exclude this module via the predicate-based assembly scan and skip
/// starting the background polling loop.
/// </summary>
public sealed class PriceFeedBackgroundInfrastructureModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddHostedService<PriceFeedHostedService>();
    }
}
