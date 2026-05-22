using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;
using TradyStrat.Application.Trades.UseCases;

namespace TradyStrat.Application.Trades;

public sealed class TradesApplicationModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<LogTradeUseCase>();
        services.AddScoped<DeleteTradeUseCase>();
        services.AddScoped<ImportTradesCsvUseCase>();
    }
}
