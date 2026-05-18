using TradyStrat.Features.PriceFeed.Providers;
using TradyStrat.Infrastructure.PriceFeed;
using TradyStrat.Features.PriceFeed;
using TheAppManager.Modules;
using TradyStrat.Application.PriceFeed.Providers;
using TradyStrat.Application.PriceFeed;

namespace TradyStrat.Modules;

public sealed class PriceFeedModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient<IPriceFeed, YahooPriceFeed>(c =>
        {
            c.BaseAddress = new Uri(builder.Configuration["Yahoo:BaseUrl"]
                ?? "https://query1.finance.yahoo.com");
            c.Timeout = TimeSpan.FromSeconds(15);
            c.DefaultRequestHeaders.UserAgent.ParseAdd("TradyStrat/1.0");
        }).AddStandardResilienceHandler();

        builder.Services.AddScoped<DailyPriceCache>();
        builder.Services.AddHostedService<PriceFeedHostedService>();
    }
}
