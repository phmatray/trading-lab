using TheAppManager.Modules;
using TradyStrat.Features.PredictionMarkets;
using TradyStrat.Features.PredictionMarkets.Providers;

namespace TradyStrat.Modules;

public sealed class PredictionMarketsModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        var options = PolymarketOptionsBinder.Read(builder.Configuration);
        builder.Services.AddSingleton(options);

        builder.Services
            .AddHttpClient<IPredictionMarketProvider, PolymarketGammaProvider>(c =>
            {
                c.BaseAddress = new Uri(options.BaseUrl);
                c.Timeout     = TimeSpan.FromSeconds(10);
                c.DefaultRequestHeaders.UserAgent.ParseAdd("TradyStrat/1.0");
            })
            .AddStandardResilienceHandler();
    }
}
