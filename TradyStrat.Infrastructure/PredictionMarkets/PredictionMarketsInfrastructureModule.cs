using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using TheAppManager.Modules;
using TradyStrat.Application.PredictionMarkets;
using TradyStrat.Infrastructure.PredictionMarkets.Providers;

namespace TradyStrat.Infrastructure.PredictionMarkets;

public sealed class PredictionMarketsInfrastructureModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddHttpClient<IPredictionMarketProvider, PolymarketGammaProvider>(c =>
        {
            c.BaseAddress = new Uri(config["Polymarket:BaseUrl"] ?? "https://gamma-api.polymarket.com");
            c.Timeout     = TimeSpan.FromSeconds(10);
            c.DefaultRequestHeaders.UserAgent.ParseAdd("TradyStrat/1.0");
        }).AddStandardResilienceHandler();
    }
}
