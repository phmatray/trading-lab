using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;

namespace TradyStrat.Infrastructure.Trades;

public sealed class TradesInfrastructureModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        // EditTradeUseCase is .bak'd; pending disposition (likely rewrite or delete) in Task 31+.
    }
}
