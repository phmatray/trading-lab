using TheAppManager.Modules;
using TradyStrat.Features.PriceFeed.UseCases;

namespace TradyStrat.Modules;

public sealed class PricesUseCasesModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<RefreshAllPricesUseCase>();
    }
}
