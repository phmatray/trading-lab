using TheAppManager.Modules;
using TradyStrat.Application.UseCases.Trades;

namespace TradyStrat.Modules;

public sealed class TradesModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<LogTradeUseCase>();
        builder.Services.AddScoped<EditTradeUseCase>();
        builder.Services.AddScoped<DeleteTradeUseCase>();
        builder.Services.AddScoped<ImportTradesCsvUseCase>();
    }
}
