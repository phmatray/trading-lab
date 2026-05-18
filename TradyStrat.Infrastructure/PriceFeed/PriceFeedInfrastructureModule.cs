using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using TheAppManager.Modules;
using TradyStrat.Application.PriceFeed.Providers;
using TradyStrat.Infrastructure.PriceFeed.Providers;
using TradyStrat.Infrastructure.PriceFeed.UseCases;

namespace TradyStrat.Infrastructure.PriceFeed;

public sealed class PriceFeedInfrastructureModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddHttpClient<IPriceFeed, YahooPriceFeed>(c =>
        {
            c.BaseAddress = new Uri(config["Yahoo:BaseUrl"] ?? "https://query1.finance.yahoo.com");
            c.Timeout = TimeSpan.FromSeconds(15);
            c.DefaultRequestHeaders.UserAgent.ParseAdd("TradyStrat/1.0");
        }).AddStandardResilienceHandler();

        services.AddScoped<DailyPriceCache>();
        services.AddScoped<RefreshAllPricesUseCase>();
    }
}
