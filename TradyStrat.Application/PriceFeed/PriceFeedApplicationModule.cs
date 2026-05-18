using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;

namespace TradyStrat.Application.PriceFeed;

public sealed class PriceFeedApplicationModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        // Currently empty — RefreshAllPricesUseCase lives in Infrastructure
        // because it depends on DailyPriceCache (which uses AppDbContext).
        // Once that use case is reworked to talk via repository ports, it moves here.
    }
}
