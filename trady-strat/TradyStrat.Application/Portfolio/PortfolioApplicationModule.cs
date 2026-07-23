using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;

namespace TradyStrat.Application.Portfolio;

public sealed class PortfolioApplicationModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        // IPortfolioRepository implementation is registered by
        // TradyStrat.Infrastructure.Portfolio.PortfolioInfrastructureModule.
        // The Application module has no own registrations — the AR is consumed
        // directly by use cases via the IPortfolioRepository port.
    }
}
