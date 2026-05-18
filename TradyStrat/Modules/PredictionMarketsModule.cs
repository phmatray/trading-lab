using TheAppManager.Modules;
using TradyStrat.Application.PredictionMarkets;
using TradyStrat.Features.PredictionMarkets.Providers;

namespace TradyStrat.Modules;

public sealed class PredictionMarketsModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services
            .AddHttpClient<IPredictionMarketProvider, PolymarketGammaProvider>(c =>
            {
                c.BaseAddress = new Uri(builder.Configuration["Polymarket:BaseUrl"]
                    ?? "https://gamma-api.polymarket.com");
                c.Timeout     = TimeSpan.FromSeconds(10);
                c.DefaultRequestHeaders.UserAgent.ParseAdd("TradyStrat/1.0");
            })
            .AddStandardResilienceHandler();
    }
}
