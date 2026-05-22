using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;

namespace TradyStrat.Application.Trades;

public sealed class TradesApplicationModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        // TODO(Phase2): LogTradeUseCase, DeleteTradeUseCase, ImportTradesCsvUseCase are offline (.bak).
        // Registrations will be restored in Tasks 27-29 when the use cases are rewritten against the new domain.
    }
}
