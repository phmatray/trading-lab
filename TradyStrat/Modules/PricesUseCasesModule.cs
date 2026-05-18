using TheAppManager.Modules;
using TradyStrat.Infrastructure.PriceFeed.UseCases;

namespace TradyStrat.Modules;

public sealed class PricesUseCasesModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<RefreshAllPricesUseCase>();
    }
}
