using Microsoft.Extensions.Http.Resilience;
using TheAppManager.Modules;
using TradyStrat.Features.Fx;

namespace TradyStrat.Modules;

public sealed class FxModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient<IFxRateProvider, YahooFxProvider>(c =>
        {
            c.BaseAddress = new Uri(builder.Configuration["Yahoo:BaseUrl"]
                ?? "https://query1.finance.yahoo.com");
            c.Timeout = TimeSpan.FromSeconds(15);
            c.DefaultRequestHeaders.UserAgent.ParseAdd("TradyStrat/1.0");
        }).AddStandardResilienceHandler();

        builder.Services.AddScoped<DailyFxCache>();
        builder.Services.AddScoped<FxConverter>();
    }
}
