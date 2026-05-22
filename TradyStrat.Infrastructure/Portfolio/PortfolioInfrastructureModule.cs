using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;
using TradyStrat.Application.Portfolio;

namespace TradyStrat.Infrastructure.Portfolio;

public sealed class PortfolioInfrastructureModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IPortfolioRepository, EfPortfolioRepository>();
    }
}
