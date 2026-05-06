using TheAppManager.Modules;
using TradyStrat.Application.UseCases.Prices;

namespace TradyStrat.Modules;

public sealed class PricesUseCasesModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<RefreshAllPricesUseCase>();
    }
}
