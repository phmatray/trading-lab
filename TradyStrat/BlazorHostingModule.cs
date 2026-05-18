using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;
using TradyStrat.Features.Shell;

namespace TradyStrat;

public sealed class BlazorHostingModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddRazorComponents().AddInteractiveServerComponents();
    }

    public void ConfigureMiddleware(WebApplication app)
    {
        app.UseStaticFiles();
        app.UseAntiforgery();
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapRazorComponents<App>().AddInteractiveServerRenderMode();
    }
}
