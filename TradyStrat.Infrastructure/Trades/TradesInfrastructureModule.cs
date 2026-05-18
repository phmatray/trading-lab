using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;
using TradyStrat.Infrastructure.Trades.UseCases;

namespace TradyStrat.Infrastructure.Trades;

public sealed class TradesInfrastructureModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<EditTradeUseCase>();
    }
}
